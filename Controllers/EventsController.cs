using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.API.Data;
using TaskManager.API.Models;
using TaskManager.API.Services;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notifications;

        public EventsController(AppDbContext context, NotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private int? GetOrgId()
        {
            var val = User.FindFirstValue("OrganizationId");
            return string.IsNullOrEmpty(val) ? null : int.Parse(val);
        }

        private string GetRole() =>
            User.FindFirstValue(ClaimTypes.Role) ?? "Employee";

        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var orgId = GetOrgId();
            var userId = GetUserId();
            var role = GetRole();

            var events = await _context.Events
                .Where(e => e.OrganizationId == orgId)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.RequiredStaff,
                    e.CreatedAt,
                    TotalResponses = e.Responses.Count,
                    AvailableResponses = e.Responses.Count(r => r.Status == "Available"),
                    ApprovedResponses = e.Responses.Count(r => r.IsApproved),
                    MyResponse = e.Responses
                        .Where(r => r.UserId == userId)
                        .Select(r => new { r.Status, r.IsApproved })
                        .FirstOrDefault(),
                    Responses = role == "Admin" ? e.Responses.Select(r => new
                    {
                        r.Id,
                        r.UserId,
                        r.Status,
                        r.IsApproved,
                        r.RespondedAt,
                        Username = r.User!.Username,
                    }).ToList() : null
                })
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            return Ok(events);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            var orgId = GetOrgId();
            var userId = GetUserId();

            if (orgId == null) return BadRequest("No organization");

            var ev = new Event
            {
                Title = request.Title,
                Description = request.Description,
                EventDate = request.EventDate,
                Location = request.Location,
                RequiredStaff = request.RequiredStaff,
                OrganizationId = orgId.Value,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            // إشعار لكل موظفي المنظمة
            await _notifications.SendToOrganization(
                orgId.Value,
                userId,
                "📅 " + ev.Title,
                $"Neue Veranstaltung am {ev.EventDate:dd.MM.yyyy HH:mm}"
            );

            return Ok(ev);
        }

        [HttpPost("{id}/respond")]
        public async Task<IActionResult> Respond(int id, [FromBody] RespondRequest request)
        {
            var userId = GetUserId();

            var existing = await _context.EventResponses
                .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);

            if (existing != null)
            {
                existing.Status = request.Status;
                existing.RespondedAt = DateTime.UtcNow;
            }
            else
            {
                _context.EventResponses.Add(new EventResponse
                {
                    EventId = id,
                    UserId = userId,
                    Status = request.Status,
                    RespondedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // إشعار للمدير لما يرد موظف
            if (request.Status == "Available")
            {
                var ev = await _context.Events
                    .Include(e => e.Organization)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (ev != null)
                {
                    var respondingUser = await _context.Users.FindAsync(userId);
                    var adminIds = _context.Users
                        .Where(u => u.OrganizationId == ev.OrganizationId && u.Role == "Admin")
                        .Select(u => u.Id)
                        .ToList();

                    foreach (var adminId in adminIds)
                    {
                        await _notifications.SendToUser(
                            adminId,
                            "✅ Neue Antwort",
                            $"{respondingUser?.Username} ist verfügbar für: {ev.Title}"
                        );
                    }
                }
            }

            return Ok(new { message = "Response saved" });
        }

        [HttpPost("{id}/approve/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, int userId)
        {
            var response = await _context.EventResponses
                .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);

            if (response == null) return NotFound();

            response.IsApproved = true;
            await _context.SaveChangesAsync();

            // إشعار للموظف المقبول
            var eventInfo = await _context.Events.FindAsync(id);
            await _notifications.SendToUser(
                userId,
                "✅ Teilnahme bestätigt!",
                $"Ihre Teilnahme wurde bestätigt für: {eventInfo?.Title}"
            );

            return Ok(new { message = "Approved" });
        }

        [HttpDelete("{id}/remove/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remove(int id, int userId)
        {
            var response = await _context.EventResponses
                .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);

            if (response != null)
            {
                _context.EventResponses.Remove(response);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Removed" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted" });
        }
    }

    public class CreateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int RequiredStaff { get; set; }
    }

    public class RespondRequest
    {
        public string Status { get; set; } = "Available";
    }
}