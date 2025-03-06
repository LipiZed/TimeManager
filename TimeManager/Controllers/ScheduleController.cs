using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TimeManager.Data;
using TimeManager.Models;

[Authorize] // Доступ только авторизованным пользователям
public class ScheduleController : Controller
{
    private readonly TimeManagerDbContext _context;
    private readonly UserManager<User> _userManager;

    public ScheduleController(TimeManagerDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Метод для отображения главной страницы с неделей
    public IActionResult Index()
    {
        return View();
    }

    // Метод для отображения конкретного дня недели
    public async Task<IActionResult> DayView(DayOfWeek day)
    {
        var userId = _userManager.GetUserId(User);

        // Теперь получаем задачи напрямую по дню недели
        var objectives = await _context.Objectives
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

    // Метод для добавления новой задачи
    [HttpPost]
    public async Task<IActionResult> AddObjective(DayOfWeek day, Objective objective)
    {
        var userId = _userManager.GetUserId(User);

        objective.UserId = userId;
        objective.WeekDay = day; // Устанавливаем день недели напрямую

        _context.Objectives.Add(objective);
        await _context.SaveChangesAsync();

        return RedirectToAction("DayView", new { day });
    }

    // Другие методы для редактирования, удаления задач и т.д.
}

// Модель представления для страницы дня
public class DayViewModel
{
    public DayOfWeek Day { get; set; }
    public List<Objective> Objectives { get; set; }
}