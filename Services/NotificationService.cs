using WebPush;
using TaskManager.API.Data;
using System.Text.Json;

namespace TaskManager.API.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public NotificationService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task SendToUser(int userId, string title, string body, string? url = null)
        {
            var subscriptions = _context.PushSubscriptions
                .Where(s => s.UserId == userId)
                .ToList();

            if (!subscriptions.Any()) return;

            var subject = _config["VapidKeys:Subject"]!;
            var publicKey = _config["VapidKeys:PublicKey"]!;
            var privateKey = _config["VapidKeys:PrivateKey"]!;

            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);

            var payload = JsonSerializer.Serialize(new
            {
                title,
                body,
                url = url ?? "/",
                icon = "/pwa-192x192.png"
            });

            var webPushClient = new WebPushClient();

            foreach (var sub in subscriptions)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(
                        sub.Endpoint,
                        sub.P256dh,
                        sub.Auth
                    );
                    await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                }
                catch
                {
                    _context.PushSubscriptions.Remove(sub);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task SendToOrganization(int orgId, int excludeUserId, string title, string body, string? url = null)
        {
            var userIds = _context.Users
                .Where(u => u.OrganizationId == orgId && u.Id != excludeUserId)
                .Select(u => u.Id)
                .ToList();

            foreach (var userId in userIds)
            {
                await SendToUser(userId, title, body, url);
            }
        }
    }
}