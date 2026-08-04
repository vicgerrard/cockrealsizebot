using CockRealSizeBot.Bot.Features.Measurement;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;
using TelegramInlineQuery = Telegram.Bot.Types.InlineQuery;

namespace CockRealSizeBot.Bot.Features.Inline;

/// <summary>
/// Отвечает на inline-запрос карточкой с результатом замера.
/// Текст запроса намеренно игнорируется: мерим всегда автора, чтобы результат
/// нельзя было подделать и нельзя было «измерить» кого-то без его участия.
/// </summary>
internal sealed partial class AnswerMeasurementQuery(
    ITelegramBotClient bot,
    MeasureUser.Handler measure,
    DailyCycle cycle,
    ILogger<AnswerMeasurementQuery> logger)
{
    /// <summary>
    /// Идентификатор результата. Постоянный — у нас всегда ровно один вариант ответа.
    /// </summary>
    private const string ResultId = "measurement";

    public async Task HandleAsync(TelegramInlineQuery query, CancellationToken cancellationToken)
    {
        var result = measure.Measure(new MeasureUser.Query(query.From.Id));

        var article = new InlineQueryResultArticle(
            id: ResultId,
            title: MeasurementCard.ResultTitle,
            inputMessageContent: new InputTextMessageContent(MeasurementCard.Render(result)))
        {
            Description = MeasurementCard.ResultDescription,
        };

        await bot.AnswerInlineQuery(
            query.Id,
            [article],
            cacheTime: CacheSecondsUntilNextDay(),
            // Результат зависит от пользователя — Telegram не должен показывать
            // чужой ответ из общего кэша.
            isPersonal: true,
            cancellationToken: cancellationToken);

        LogMeasured(logger, query.From.Id, result.Centimeters, result.Rank);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Замер для пользователя {UserId}: {Centimeters} см ({Rank})")]
    private static partial void LogMeasured(ILogger logger, long userId, int centimeters, string rank);

    /// <summary>
    /// Кэшируем ответ ровно до полуночи: до неё результат всё равно не изменится,
    /// а после — обязан. Так суточный кулдаун соблюдается ещё и на стороне Telegram.
    /// </summary>
    private int CacheSecondsUntilNextDay() =>
        (int)Math.Clamp(cycle.UntilNextDay().TotalSeconds, 1, TimeSpan.FromDays(1).TotalSeconds);
}
