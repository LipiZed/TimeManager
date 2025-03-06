using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TimeManager.Models;

public enum DayOfWeek
{
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6,
    Sunday = 7
}

public class Schedule
{
    [Key]
    public int ScheduleId { get; set; }

    public string UserId { get; set; }

    [Required]
    public DayOfWeek WeekDay { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    // Коллекция задач на этот день недели
    public virtual ICollection<Objective> Objectives { get; set; } = new List<Objective>();
}