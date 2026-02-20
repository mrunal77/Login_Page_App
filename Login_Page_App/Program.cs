using Login_Page_App.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;
using Login_Page_App.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog bootstrap logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

// Configure DB sink and register a dedicated DatabaseLogger in DI. The global Serilog logger will write to console/file only.
var dbConn = builder.Configuration.GetConnectionString("DefaultConnection");
var columnOptions = new ColumnOptions();
columnOptions.AdditionalColumns = new Collection<Serilog.Sinks.MSSqlServer.SqlColumn>
{
    new Serilog.Sinks.MSSqlServer.SqlColumn { ColumnName = "UserName", PropertyName = "User", DataType = SqlDbType.NVarChar, DataLength = 200, AllowNull = true }
};

// Register DatabaseLogger so middleware can write directly to DB sink
builder.Services.AddSingleton(new Login_Page_App.Logging.DatabaseLogger(dbConn, columnOptions));

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = default(WebApplication);

try
{
    Log.Information("Starting web host");

    app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        // Use custom global exception middleware instead of the built-in handler
        app.UseMiddleware<GlobalExceptionMiddleware>();
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Ensure authentication runs so HttpContext.User is populated for middleware
    app.UseAuthentication();

    // Serilog request logging middleware (custom) - runs after authentication and before authorization
    app.UseMiddleware<SerilogMiddleware>();

    app.UseRouting();

    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapRazorPages()
       .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
