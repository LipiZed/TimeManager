using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeManager.Models
{
    public class Objective
    {
        [Key]
        public int ObjectiveId { get; set; }

        // Здесь важное изменение. Теперь ключ пользователя — это строка, а не число
        public string UserId { get; set; }

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

        // Связь с напоминаниями
        public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    }
}