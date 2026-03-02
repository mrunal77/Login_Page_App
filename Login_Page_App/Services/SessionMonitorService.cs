using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Login_Page_App.Services
{
    public class SessionMonitorService : BackgroundService
    {
        private readonly ISessionTracker _sessionTracker;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SessionMonitorService> _logger;

        public SessionMonitorService(
            ISessionTracker sessionTracker,
            INotificationService notificationService,
            ILogger<SessionMonitorService> logger)
        {
            _sessionTracker = sessionTracker;
            _notificationService = notificationService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTimeOffset.UtcNow;
                    foreach (var kvp in ((SessionTracker)_sessionTracker).GetAllSessions())
                    {
                        var session = kvp.Value;
                        var timeRemaining = (session.ExpiresAt - now).TotalSeconds;

                        if (timeRemaining <= 10 && timeRemaining > 0 && !session.WarningSent)
                        {
                            await _notificationService.SendLogoutWarningAsync(session.UserId, (int)Math.Ceiling(timeRemaining));
                            session.WarningSent = true;
                            _logger.LogInformation("Sent logout warning to user {UserId}. Seconds remaining: {Seconds}", session.UserId, (int)Math.Ceiling(timeRemaining));
                        }
                        else if (timeRemaining <= 0)
                        {
                            await _notificationService.SendSessionExpiredAsync(session.UserId);
                            _sessionTracker.RemoveSession(session.UserId);
                            _logger.LogInformation("Sent session expired to user {UserId}", session.UserId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in session monitor service");
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
