using Microsoft.EntityFrameworkCore;
using TaskManager.API.Models;

namespace TaskManager.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // علاقة المستخدم بالمهام
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.User).WithMany(u => u.Tasks).HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}