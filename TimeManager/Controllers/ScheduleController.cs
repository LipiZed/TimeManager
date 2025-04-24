using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using TimeManager.Data;
using TimeManager.Models;

[Authorize]
public class ScheduleController : Controller
{
    private readonly TimeManagerDbContext _context;
    private readonly UserManager<User> _userManager;

    public ScheduleController(TimeManagerDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> DayView(DayOfWeek day)
    {
        var userId = _userManager.GetUserId(User);
        
        var objectives = await _context.Objectives
            .Include(o => o.Reminders)
            .Where(o => o.UserId == userId && o.WeekDay == day)
            .OrderBy(o => o.StartTime)
            .ToListAsync();

        var viewModel = new DayViewModel
        {
            Day = day,
            Objectives = objectives
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddObjective(DayOfWeek day, 
        string Title, 
        string Description, 
        string StartTime, 
        string EndTime,
        string? ReminderTime)
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId);
        if (string.IsNullOrEmpty(user.TelegramId))
        {
            ModelState.AddModelError("", "Укажите Telegram ID в профиле, чтобы использовать напоминания.");
            return RedirectToAction("DayView", new { day });
        }

        // Parse time value (hours and minutes only)
        var startTimeValue = TimeSpan.Parse(StartTime);
        DateTime startDateTime = DateTime.Today.Add(startTimeValue);
        
        // Set the day of week to the currently selected day
        int daysToAdd = ((int)day - (int)startDateTime.DayOfWeek + 7) % 7;
        startDateTime = startDateTime.AddDays(daysToAdd);

        // Parse end time if provided
        DateTime? endDateTime = null;
        if (!string.IsNullOrEmpty(EndTime))
        {
            var endTimeValue = TimeSpan.Parse(EndTime);
            endDateTime = DateTime.Today.Add(endTimeValue);
            
            // Set the day of week to the currently selected day
            endDateTime = endDateTime.Value.AddDays(daysToAdd);
            
            // If end time is earlier than start time, assume it's the next day
            if (endDateTime < startDateTime)
            {
                endDateTime = endDateTime.Value.AddDays(1);
            }
        }

        var objective = new Objective
        {
            UserId = userId,
            WeekDay = day,
            Title = Title,
            Description = Description ?? " ",
            StartTime = startDateTime,
            EndTime = endDateTime,
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };

        // Add task to context and save to get ObjectiveId
        _context.Objectives.Add(objective);
        await _context.SaveChangesAsync();

        // Add reminder if specified
        if (!string.IsNullOrEmpty(ReminderTime))
        {
            try
            {
                var reminderTimeValue = TimeSpan.Parse(ReminderTime);
                DateTime reminderDateTime = DateTime.Today.Add(reminderTimeValue);
                
                // Set the day of week to match the objective's day
                reminderDateTime = reminderDateTime.AddDays(daysToAdd);
                
                // If reminder time is after start time, assume it's for the previous day
                if (reminderDateTime > startDateTime)
                {
                    reminderDateTime = reminderDateTime.AddDays(-1);
                }
                
                var reminder = new Reminder
                {
                    ObjectiveId = objective.ObjectiveId,
                    ReminderTime = reminderDateTime,
                    IsSent = false
                };
                _context.Reminders.Add(reminder);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении напоминания: {ex.Message}");
            }
        }

        return RedirectToAction("DayView", new { day });
    }

    [HttpGet]
    public async Task<IActionResult> GetObjective(int objectiveId)
    {
        var userId = _userManager.GetUserId(User);
        
        var objective = await _context.Objectives
            .Include(o => o.Reminders)
            .FirstOrDefaultAsync(o => o.ObjectiveId == objectiveId && o.UserId == userId);

        if (objective == null)
        {
            return NotFound();
        }

        return Json(new
        {
            objectiveId = objective.ObjectiveId,
            title = objective.Title,
            description = objective.Description,
            startTime = objective.StartTime.ToString("HH:mm"),
            endTime = objective.EndTime?.ToString("HH:mm"),
            isCompleted = objective.IsCompleted,
            reminderTime = objective.Reminders.FirstOrDefault()?.ReminderTime.ToString("HH:mm")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditObjective(int ObjectiveId, 
        DayOfWeek day,
        string Title,
        string Description,
        string StartTime,
        string? EndTime,
        bool IsCompleted,
        string? ReminderTime)
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId);

        var objective = await _context.Objectives
            .Include(o => o.Reminders)
            .FirstOrDefaultAsync(o => o.ObjectiveId == ObjectiveId && o.UserId == userId);
        
        if (objective == null)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(user.TelegramId))
        {
            ModelState.AddModelError("", "Укажите Telegram ID в профиле, чтобы использовать напоминания.");
            return RedirectToAction("DayView", new { day });
        }

        // Parse time values (hours and minutes only)
        var startTimeValue = TimeSpan.Parse(StartTime);
        DateTime startDateTime = DateTime.Today.Add(startTimeValue);
        
        // Set the day of week to the currently selected day
        int daysToAdd = ((int)day - (int)startDateTime.DayOfWeek + 7) % 7;
        startDateTime = startDateTime.AddDays(daysToAdd);

        // Parse end time if provided
        DateTime? endDateTime = null;
        if (!string.IsNullOrEmpty(EndTime))
        {
            var endTimeValue = TimeSpan.Parse(EndTime);
            endDateTime = DateTime.Today.Add(endTimeValue);
            
            // Set the day of week to the currently selected day
            endDateTime = endDateTime.Value.AddDays(daysToAdd);
            
            // If end time is earlier than start time, assume it's the next day
            if (endDateTime < startDateTime)
            {
                endDateTime = endDateTime.Value.AddDays(1);
            }
        }

        objective.Title = Title;
        objective.Description = Description ?? " ";
        objective.StartTime = startDateTime;
        objective.EndTime = endDateTime;
        objective.IsCompleted = IsCompleted;

        // Handle reminder
        var existingReminder = objective.Reminders.FirstOrDefault();
        if (!string.IsNullOrEmpty(ReminderTime))
        {
            try
            {
                var reminderTimeValue = TimeSpan.Parse(ReminderTime);
                DateTime reminderDateTime = DateTime.Today.Add(reminderTimeValue);
                
                // Set the day of week to match the objective's day
                reminderDateTime = reminderDateTime.AddDays(daysToAdd);
                
                // If reminder time is after start time, assume it's for the previous day
                if (reminderDateTime > startDateTime)
                {
                    reminderDateTime = reminderDateTime.AddDays(-1);
                }
                
                if (existingReminder == null)
                {
                    var newReminder = new Reminder
                    {
                        ObjectiveId = objective.ObjectiveId,
                        ReminderTime = reminderDateTime,
                        IsSent = false
                    };
                    _context.Reminders.Add(newReminder);
                }
                else
                {
                    existingReminder.ReminderTime = reminderDateTime;
                    existingReminder.IsSent = false;
                    _context.Reminders.Update(existingReminder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении напоминания: {ex.Message}");
                throw;
            }
        }
        else if (existingReminder != null)
        {
            _context.Reminders.Remove(existingReminder);
        }

        _context.Objectives.Update(objective);
        await _context.SaveChangesAsync();

        return RedirectToAction("DayView", new { day });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteObjective(int objectiveId, DayOfWeek day)
    {
        var userId = _userManager.GetUserId(User);
        
        var objective = await _context.Objectives
            .FirstOrDefaultAsync(o => o.ObjectiveId == objectiveId && o.UserId == userId);

        if (objective == null)
        {
            return NotFound();
        }

        _context.Objectives.Remove(objective);
        await _context.SaveChangesAsync();

        return RedirectToAction("DayView", new { day });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteObjective(int objectiveId, DayOfWeek day)
    {
        var userId = _userManager.GetUserId(User);
        
        var objective = await _context.Objectives
            .FirstOrDefaultAsync(o => o.ObjectiveId == objectiveId && o.UserId == userId);

        if (objective == null)
        {
            return NotFound();
        }

        objective.IsCompleted = true;
        _context.Objectives.Update(objective);
        await _context.SaveChangesAsync();

        return RedirectToAction("DayView", new { day });
    }
}

public class DayViewModel
{
    public DayOfWeek Day { get; set; }
    public List<Objective> Objectives { get; set; }
}