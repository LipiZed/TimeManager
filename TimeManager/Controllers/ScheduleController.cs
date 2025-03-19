using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
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

    // GET: Отображение главной страницы с неделей
    public IActionResult Index()
    {
        return View();
    }

    // GET: Отображение конкретного дня недели
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

    // POST: Добавление новой задачи
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddObjective(DayOfWeek day, 
        string Title, 
        string Description, 
        string StartTime, 
        string EndTime,
        bool AddReminder,
        string? ReminderTime)
    {
        var userId = _userManager.GetUserId(User);

        var objective = new Objective
        {
            UserId = userId,
            WeekDay = day,
            Title = Title,
            Description = Description,
            StartTime = DateTime.Parse(StartTime), // Без округления
            EndTime = string.IsNullOrEmpty(EndTime) ? null : DateTime.Parse(EndTime), // Без округления
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };

        if (AddReminder && !string.IsNullOrEmpty(ReminderTime))
        {
            objective.Reminders.Add(new Reminder
            {
                ReminderTime = DateTime.Parse(ReminderTime),
                IsSent = false
            });
        }

        _context.Objectives.Add(objective);
        await _context.SaveChangesAsync();

        return RedirectToAction("DayView", new { day });
    }

    // GET: Получение данных задачи для редактирования
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
            startTime = objective.StartTime.ToString("yyyy-MM-ddTHH:mm"),
            endTime = objective.EndTime?.ToString("yyyy-MM-ddTHH:mm"),
            isCompleted = objective.IsCompleted,
            reminderTime = objective.Reminders.FirstOrDefault()?.ReminderTime.ToString("yyyy-MM-ddTHH:mm")
        });
    }

    // POST: Редактирование задачи
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
    
        var objective = await _context.Objectives
            .Include(o => o.Reminders)
            .FirstOrDefaultAsync(o => o.ObjectiveId == ObjectiveId && o.UserId == userId);

        if (objective == null)
        {
            return NotFound();
        }

        objective.Title = Title;
        objective.Description = Description;
        objective.StartTime = DateTime.Parse(StartTime); // Без округления
        objective.EndTime = string.IsNullOrEmpty(EndTime) ? null : DateTime.Parse(EndTime); // Без округления
        objective.IsCompleted = IsCompleted;

        // Обработка напоминания
        var existingReminder = objective.Reminders.FirstOrDefault();
        if (!string.IsNullOrEmpty(ReminderTime))
        {
            if (existingReminder == null)
            {
                objective.Reminders.Add(new Reminder
                {
                    ReminderTime = DateTime.Parse(ReminderTime),
                    IsSent = false
                });
            }
            else
            {
                existingReminder.ReminderTime = DateTime.Parse(ReminderTime);
                existingReminder.IsSent = false;
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

    // POST: Удаление задачи
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

    // POST: Отметить задачу как выполненную
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

// Модель представления для страницы дня
public class DayViewModel
{
    public DayOfWeek Day { get; set; }
    public List<Objective> Objectives { get; set; }
}