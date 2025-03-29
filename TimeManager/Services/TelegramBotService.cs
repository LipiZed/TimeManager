using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeManager.Data;
using TimeManager.Models;

namespace TimeManager.Services
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TelegramBotClient _botClient;

        public TelegramBotService(
            ILogger<TelegramBotService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _botClient = new TelegramBotClient(configuration["Telegram:BotToken"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Настраиваем получение обновлений
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // получать все типы обновлений
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken
            );

            var me = await _botClient.GetMe(stoppingToken);
            _logger.LogInformation("Бот {BotName} начал работу", me.Username);

            // Держим сервис запущенным
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                // Обрабатываем только сообщения
                if (update.Message is not { } message)
                    return;

                // Обрабатываем только текстовые сообщения
                if (message.Text is not { } messageText)
                    return;

                var chatId = message.Chat.Id;
                _logger.LogInformation("Получено сообщение '{Message}' от {UserId}", messageText, message.From?.Id);

                // Проверяем команду регистрации
                if (messageText.StartsWith("/register"))
                {
                    // Извлекаем имя пользователя из команды
                    var appUsername = messageText.Replace("/register", "").Trim();
                    var telegramId = message.From.Id.ToString();

                    // Регистрируем пользователя
                    var result = await RegisterUserTelegramId(appUsername, telegramId);

                    if (result)
                    {
                        await _botClient.SendMessage(
                            chatId,
                            $"Аккаунт {appUsername} успешно привязан к Telegram! Теперь вы будете получать уведомления.",
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId,
                            $"Пользователь с именем {appUsername} не найден в системе. Пожалуйста, проверьте правильность ввода.",
                            cancellationToken: cancellationToken);
                    }
                }
                else if (messageText == "/start")
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Привет! Я бот для отправки уведомлений о задачах. Чтобы привязать свой аккаунт, используйте команду /register ваше_имя_пользователя",
                        cancellationToken: cancellationToken);
                }
                else if (messageText == "/help")
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Доступные команды:\n" +
                        "/start - начать работу с ботом\n" +
                        "/register имя_пользователя - привязать аккаунт к Telegram\n" +
                        "/help - показать список команд",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Я не понимаю эту команду. Используйте /help для просмотра списка команд.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке сообщения");
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Ошибка API Telegram: {apiRequestException.ErrorCode} - {apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(errorMessage);
            return Task.CompletedTask;
        }

        private async Task<bool> RegisterUserTelegramId(string username, string telegramId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TimeManagerDbContext>();
                
                // Ищем пользователя по имени пользователя
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
                
                if (user != null)
                {
                    // Обновляем TelegramId
                    user.TelegramId = telegramId;
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Пользователь {Username} привязал Telegram ID: {TelegramId}", username, telegramId);
                    return true;
                }
                
                _logger.LogWarning("Пользователь {Username} не найден при попытке привязки Telegram", username);
                return false;
            }
        }
    }
}