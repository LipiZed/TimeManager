using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeManager.Models
{
    public class Reminder
    {
        [Key]
        public int ReminderId { get; set; }

        [Required]
        public int ObjectiveId { get; set; }

        [Required]
        public DateTime ReminderTime { get; set; }

        public bool IsSent { get; set; } = false;

        [ForeignKey("ObjectiveId")]
        public virtual Objective Objective { get; set; }
    }
}