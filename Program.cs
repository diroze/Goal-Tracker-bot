using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    class Goal
    {
        public string Description { get; set; }
        public bool IsDone { get; set; }
    }

    static Dictionary<long, List<Goal>> userGoals = new();

    static async Task Main()
    {
        var token = Environment.GetEnvironmentVariable("BOT_TOKEN");
        var botClient = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cts.Token);

        var me = await botClient.GetMe();
        Console.WriteLine($"Запущено @{me.Username}");

        Console.ReadLine();

        cts.Cancel();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null) return;

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text;

        if (!userGoals.ContainsKey(chatId))
            userGoals[chatId] = new List<Goal>();

        if (text.StartsWith("/start"))
        {
            await botClient.SendMessage(chatId,
                "Привіт! Це Goal Tracker бот.\nКоманди:\n/add <текст> - додати ціль\n/list - показати цілі\n/done <номер> - позначити ціль виконаною",
                cancellationToken: cancellationToken);
        }
        else if (text.StartsWith("/add "))
        {
            var goalText = text[5..].Trim();

            if (string.IsNullOrWhiteSpace(goalText))
            {
                await botClient.SendMessage(chatId, "Текст цілі не може бути порожнім.", cancellationToken: cancellationToken);
                return;
            }

            userGoals[chatId].Add(new Goal { Description = goalText, IsDone = false });
            await botClient.SendMessage(chatId, $"Додано ціль: {goalText}", cancellationToken: cancellationToken);
        }
        else if (text == "/list")
        {
            var goals = userGoals[chatId];
            if (goals.Count == 0)
            {
                await botClient.SendMessage(chatId, "У вас немає доданих цілей.", cancellationToken: cancellationToken);
                return;
            }

            string response = "Ваші цілі:\n";
            for (int i = 0; i < goals.Count; i++)
            {
                var g = goals[i];
                response += $"{i + 1}. {(g.IsDone ? "✅" : "❌")} {g.Description}\n";
            }

            await botClient.SendMessage(chatId, response, cancellationToken: cancellationToken);
        }
        else if (text.StartsWith("/done "))
        {
            var numberStr = text[6..].Trim();
            if (!int.TryParse(numberStr, out int number))
            {
                await botClient.SendMessage(chatId, "Введіть правильний номер цілі.", cancellationToken: cancellationToken);
                return;
            }

            var goals = userGoals[chatId];
            if (number < 1 || number > goals.Count)
            {
                await botClient.SendMessage(chatId, "Номер цілі поза діапазоном.", cancellationToken: cancellationToken);
                return;
            }

            goals[number - 1].IsDone = true;
            await botClient.SendMessage(chatId, $"Ціль №{number} позначена як виконана ✅", cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendMessage(chatId, "Невідома команда. Напишіть /start для допомоги.", cancellationToken: cancellationToken);
        }
    }

    static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Помилка: {exception.Message}");
        return Task.CompletedTask;
    }
}
