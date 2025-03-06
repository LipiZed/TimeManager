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

        public DbSet<Models.Objective> Objectives { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Schedule> Schedules { get; set; } // Добавляем новую таблицу

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Objectives)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Schedule>()
                .HasMany(s => s.Objectives)
                .WithOne(o => o.Schedule)
                .HasForeignKey(o => o.ScheduleId)
                .OnDelete(DeleteBehavior.SetNull); // Если расписание удалено, задачи остаются
        }
    }
}