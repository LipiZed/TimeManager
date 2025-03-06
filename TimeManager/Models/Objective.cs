using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeManager.Models
{
    public class Objective
    {
        [Key]
        public int ObjectiveId { get; set; }

        public string UserId { get; set; }

        // Добавляем ссылку на расписание (день недели)
        public int? ScheduleId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool IsCompleted { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Связь с пользователем
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // Связь с расписанием дня недели
        [ForeignKey("ScheduleId")]
        public virtual Schedule Schedule { get; set; }

        // Связь с напоминаниями
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    }
}