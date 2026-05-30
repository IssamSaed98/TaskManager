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
        public DbSet<Organization> Organizations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // علاقة المستخدم بالمهام
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // علاقة المنظمة بالمستخدمين
            modelBuilder.Entity<User>()
                .HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            // علاقة المنظمة بالمهام
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Organization)
                .WithMany()
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}