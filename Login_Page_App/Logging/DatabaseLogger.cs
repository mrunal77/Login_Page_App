using Serilog;
using Serilog.Sinks.MSSqlServer;

namespace Login_Page_App.Logging
{
    public class DatabaseLogger
    {
        public Serilog.ILogger Logger { get; }

        public DatabaseLogger(string connectionString, ColumnOptions columnOptions)
        {
            Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions { TableName = "RequestLogs", AutoCreateSqlTable = true },
                    columnOptions: columnOptions,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .CreateLogger();
        }
    }
}
