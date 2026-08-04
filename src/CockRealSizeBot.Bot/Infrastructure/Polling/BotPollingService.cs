using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace CockRealSizeBot.Bot.Infrastructure.Polling;

/// <summary>
/// Long polling: бот сам ходит за апдейтами. Не требует ни публичного домена,
/// ни сертификата — ровно то, что нужно для VPS без DNS.
/// </summary>
internal sealed partial class BotPollingService(
    ITelegramBotClient bot,
    BotUpdateHandler updateHandler,
    ILogger<BotPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await bot.GetMe(stoppingToken);
        LogStarted(logger, me.Username, me.Id);

        var receiverOptions = new ReceiverOptions
        {
            // Просим у Telegram только то, что умеем обрабатывать.
            AllowedUpdates = [UpdateType.InlineQuery],

            // Апдейты, накопившиеся пока бот лежал, уже неактуальны:
            // inline-запрос живёт секунды.
            DropPendingUpdates = true,
        };

        await bot.ReceiveAsync(updateHandler, receiverOptions, stoppingToken);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Бот @{Username} (id {BotId}) запущен в режиме long polling")]
    private static partial void LogStarted(ILogger logger, string? username, long botId);
}
