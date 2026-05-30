namespace TaskManager.API.Models
{
    public class Organization
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}