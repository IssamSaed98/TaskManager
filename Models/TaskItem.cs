namespace TaskManager.API.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsCompleted { get; set; } = false;

        public string Priority { get; set; } = "Medium";

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // One to Many Rel
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}