using Microsoft.AspNetCore.SignalR;
using Login_Page_App.Hubs;
using System.Threading.Tasks;

namespace Login_Page_App.Services
{
    public interface INotificationService
    {
        Task SendLogoutWarningAsync(string userId, int secondsRemaining);
        Task SendSessionExpiredAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendLogoutWarningAsync(string userId, int secondsRemaining)
        {
            await _hubContext.Clients.Group(userId).SendAsync("ReceiveLogoutWarning", secondsRemaining);
        }

        public async Task SendSessionExpiredAsync(string userId)
        {
            await _hubContext.Clients.Group(userId).SendAsync("ReceiveSessionExpired");
        }
    }
}
