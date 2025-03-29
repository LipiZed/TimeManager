using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using TimeManager.Data;
using TimeManager.Models;

namespace TimeManager.Services
{
    public class ReminderService : BackgroundService
    {
        private readonly ILogger<ReminderService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly TelegramBotClient _botClient;

        public ReminderService(
            ILogger<ReminderService> logger,
            IServiceProvider serviceProvider, 
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _botClient = new TelegramBotClient(configuration["Telegram:BotToken"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<TimeManagerDbContext>();
                        var now = DateTime.Now;
                        var currentMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

                        var reminders = await dbContext.Reminders
                            .Include(r => r.Objective)
                            .ThenInclude(o => o.User)
                            .Where(r => !r.IsSent && r.ReminderTime <= currentMinute)
                            .ToListAsync(stoppingToken);

                        _logger.LogInformation("Найдено {Count} непосланных напоминаний на текущее время", reminders.Count);

                        foreach (var reminder in reminders)
                        {
                            var objective = reminder.Objective;
                            var user = objective.User;

                            if (string.IsNullOrEmpty(user.TelegramId))
                            {
                                _logger.LogWarning("Пользователь {UserId} не имеет привязанного Telegram ID", user.Id);
                                continue;
                            }

                            try
                            {
                                // Пробуем преобразовать TelegramId в long
                                if (long.TryParse(user.TelegramId, out long chatId))
                                {
                                    string message = $"Напоминание по задаче '{objective.Title}'\n" +
                                                    $"Описание: {objective.Description ?? "Нет описания"}\n" +
                                                    $"Время начала: {objective.StartTime:g}";
                                    
                                    // Используем SendMessage, если у вас есть такой метод
                                    await _botClient.SendMessage(chatId, message, cancellationToken: stoppingToken);
                                    
                                    reminder.IsSent = true;
                                    _logger.LogInformation("Отправлено напоминание пользователю {UserId} о задаче {ObjectiveId}", user.Id, objective.ObjectiveId);
                                }
                                else
                                {
                                    _logger.LogError("Неверный формат TelegramId для пользователя {UserId}: {TelegramId}", user.Id, user.TelegramId);
                                }
                            }
                            catch (ApiRequestException ex) when (ex.Message.Contains("chat not found"))
                            {
                                _logger.LogError("Чат не найден для пользователя {UserId} (TelegramId: {TelegramId}). Возможно, пользователь не начал диалог с ботом", user.Id, user.TelegramId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Ошибка при отправке сообщения пользователю {UserId}", user.Id);
                            }
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в ReminderService");
                }

                await Task.Delay(50000, stoppingToken);
            }
        }
    }
}