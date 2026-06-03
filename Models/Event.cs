namespace TaskManager.API.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int RequiredStaff { get; set; }
        public int OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<EventResponse> Responses { get; set; } = new List<EventResponse>();
    }
}