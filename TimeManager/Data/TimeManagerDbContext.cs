using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeManager.Models;

namespace TimeManager.Data
{
    // Изменяем наследование на IdentityDbContext и указываем вашу модель пользователя
    public class TimeManagerDbContext : IdentityDbContext<User>
    {
        public TimeManagerDbContext(DbContextOptions<TimeManagerDbContext> options)
            : base(options)
        {
            
        }
        
        
        public DbSet<Objective> Objectives { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Теперь у нас только одно отношение, которое мы можем настроить как Cascade
            modelBuilder.Entity<User>()
                .HasMany(u => u.Objectives)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настраиваем отношение для Reminder
            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.Objective)
                .WithMany(o => o.Reminders)
                .HasForeignKey(r => r.ObjectiveId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}