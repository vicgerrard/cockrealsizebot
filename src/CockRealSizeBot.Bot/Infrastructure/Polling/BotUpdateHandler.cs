using CockRealSizeBot.Bot.Features.Inline;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CockRealSizeBot.Bot.Infrastructure.Polling;

/// <summary>
/// Маршрутизация апдейтов по слайсам. Пока апдейт ровно один — inline-запрос.
/// </summary>
internal sealed partial class BotUpdateHandler(
    AnswerMeasurementQuery inlineMeasurement,
    ILogger<BotUpdateHandler> logger) : IUpdateHandler
{
    /// <summary>Пауза после сетевой ошибки, чтобы не молотить API в цикле.</summary>
    private static readonly TimeSpan PollingErrorBackoff = TimeSpan.FromSeconds(5);

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.InlineQuery is { } inlineQuery)
        {
            await inlineMeasurement.HandleAsync(inlineQuery, cancellationToken);
            return;
        }

        LogUpdateSkipped(logger, update.Type);
    }

    public async Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        LogHandlingFailed(logger, exception, source);

        // Ошибка конкретного апдейта не повод тормозить очередь; а вот сорванный
        // опрос лучше повторить не сразу.
        if (source is HandleErrorSource.PollingError)
        {
            await Task.Delay(PollingErrorBackoff, cancellationToken);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Апдейт типа {UpdateType} пропущен — обработчика нет")]
    private static partial void LogUpdateSkipped(ILogger logger, UpdateType updateType);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Ошибка при обработке апдейта, источник {Source}")]
    private static partial void LogHandlingFailed(ILogger logger, Exception exception, HandleErrorSource source);
}
