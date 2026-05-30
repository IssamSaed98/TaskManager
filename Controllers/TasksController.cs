using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.API.Data;
using TaskManager.API.Models;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private int? GetOrgId()
        {
            var val = User.FindFirstValue("OrganizationId");
            return string.IsNullOrEmpty(val) ? null : int.Parse(val);
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            var userId = GetUserId();
            var orgId = GetOrgId();
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (role == "Admin" && orgId.HasValue)
                return await _context.Tasks
                    .Where(t => t.OrganizationId == orgId)
                    .ToListAsync();

            return await _context.Tasks
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        // GET: api/tasks/1
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var userId = GetUserId();
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound();

            return task;
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(CreateTaskRequest request)
        {
            var task = new TaskItem
            {
                Title = request.Title,
                Description = request.Description,
                IsCompleted = request.IsCompleted,
                Priority = request.Priority,
                DueDate = request.DueDate,
                UserId = GetUserId(),
                OrganizationId = GetOrgId(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        // PUT: api/tasks/1
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem task)
        {
            var userId = GetUserId();

            if (id != task.Id)
                return BadRequest();

            var existing = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (existing == null)
                return NotFound();

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.IsCompleted = task.IsCompleted;
            existing.Priority = task.Priority;
            existing.DueDate = task.DueDate;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/tasks/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetUserId();
            var orgId = GetOrgId();
            var role = User.FindFirstValue(ClaimTypes.Role);

            TaskItem? task;

            if (role == "Admin")
                task = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
            else
                task = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; } = false;
        public string Priority { get; set; } = "Medium";
        public DateTime? DueDate { get; set; }
    }
}