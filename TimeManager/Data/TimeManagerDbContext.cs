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

        // Удаляем DbSet<User>, так как теперь пользователи будут в таблице AspNetUsers
        public DbSet<Models.Objective> Objectives { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Обязательно вызвать метод базового класса!

            // Настраиваем связи с другими таблицами
            modelBuilder.Entity<User>()
                .HasMany(u => u.Objectives)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}