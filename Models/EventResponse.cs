namespace TaskManager.API.Models
{
    public class EventResponse
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Status { get; set; } = "Pending";
        public bool IsApproved { get; set; } = false;
        public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    }
}