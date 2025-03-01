using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeManager.Models
{
    // Наследуемся от IdentityUser, который содержит все необходимые Identity поля
    public class User : IdentityUser
    {
        // Примечание: поля Id, UserName, Email, PasswordHash уже есть в IdentityUser
        // поэтому мы их не добавляем повторно

        // Telegram ID для отправки уведомлений
        [StringLength(50)]
        public string TelegramId { get; set; }

        // Имя пользователя
        [StringLength(50)]
        public string FirstName { get; set; }

        // Фамилия пользователя
        [StringLength(50)]
        public string LastName { get; set; }

        // Дата регистрации пользователя
        [Required]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        // Навигационное свойство, связь "один-ко-многим" с задачами
        public virtual ICollection<Objective> Objectives { get; set; } = new List<Objective>();
    }
}