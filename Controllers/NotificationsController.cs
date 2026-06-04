using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.API.Data;
using TaskManager.API.Models;
using TaskManager.API.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public NotificationsController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/notifications/vapid-public-key
        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            return Ok(new { publicKey = _config["VapidKeys:PublicKey"] });
        }

        // POST: api/notifications/subscribe
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            var userId = GetUserId();

            var existing = _context.PushSubscriptions
                .FirstOrDefault(s => s.UserId == userId && s.Endpoint == request.Endpoint);

            if (existing != null) return Ok(new { message = "Already subscribed" });

            _context.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Subscribed" });
        }

        // DELETE: api/notifications/unsubscribe
        [HttpDelete("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
        {
            var userId = GetUserId();
            var sub = _context.PushSubscriptions
                .FirstOrDefault(s => s.UserId == userId && s.Endpoint == request.Endpoint);

            if (sub != null)
            {
                _context.PushSubscriptions.Remove(sub);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Unsubscribed" });
        }
    }

    public class SubscribeRequest
    {
        public string Endpoint { get; set; } = string.Empty;
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class UnsubscribeRequest
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}