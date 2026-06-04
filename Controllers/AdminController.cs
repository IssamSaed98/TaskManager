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
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notifications;

        public AdminController(AppDbContext context, NotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        private int? GetOrgId()
        {
            var val = User.FindFirstValue("OrganizationId");
            return string.IsNullOrEmpty(val) ? null : int.Parse(val);
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var orgId = GetOrgId();

            var users = await _context.Users
                .Where(u => u.Role == "Employee" && u.OrganizationId == orgId)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    TotalTasks = _context.Tasks.Count(t => t.UserId == u.Id),
                    CompletedTasks = _context.Tasks.Count(t => t.UserId == u.Id && t.IsCompleted),
                    ActiveTasks = _context.Tasks.Count(t => t.UserId == u.Id && !t.IsCompleted),
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/admin/users/{id}/tasks
        [HttpGet("users/{id}/tasks")]
        public async Task<IActionResult> GetUserTasks(int id)
        {
            var orgId = GetOrgId();

            var tasks = await _context.Tasks
                .Where(t => t.UserId == id && t.OrganizationId == orgId)
                .ToListAsync();

            return Ok(tasks);
        }

        // POST: api/admin/tasks
        [HttpPost("tasks")]
        public async Task<IActionResult> CreateTaskForUser([FromBody] AdminCreateTaskRequest request)
        {
            var orgId = GetOrgId();

            var task = new TaskItem
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                DueDate = request.DueDate,
                IsCompleted = false,
                UserId = request.UserId,
                OrganizationId = orgId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);

            // 1. حفظ المهمة أولاً في قاعدة البيانات
            await _context.SaveChangesAsync();

            // 2. إرسال الإشعار للمستخدم بعد نجاح الحفظ
            await _notifications.SendToUser(
                request.UserId,
                "📋 Neue Aufgabe",
                $"Sie haben eine neue Aufgabe: {task.Title}"
            );

            return Ok(task);
        }
        // GET: api/admin/organization
        [HttpGet("organization")]
        public async Task<IActionResult> GetOrganization()
        {
            var orgId = GetOrgId();

            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == orgId);

            if (org == null)
                return NotFound();

            return Ok(org);
        }
    }

    public class AdminCreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public DateTime? DueDate { get; set; }
        public int UserId { get; set; }
    }
}