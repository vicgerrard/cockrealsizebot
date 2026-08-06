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
    ILogger<AnswerMeasurementQuery> logger)
{
    /// <summary>
    /// Идентификатор результата. Постоянный — у нас всегда ровно один вариант ответа.
    /// </summary>
    private const string ResultId = "measurement";

    public async Task HandleAsync(TelegramInlineQuery query, CancellationToken cancellationToken)
    {
        var result = measure.Measure(new MeasureUser.Query(query.From.Id, query.Id));

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
            // Ноль передаётся явно: если cacheTime не отправить, Telegram применит
            // свой дефолт в 300 секунд.
            cacheTime: 0,
            // Без этого флага кэш у Telegram общий: результат, отданный одному
            // пользователю, показывается всем, кто набрал тот же запрос.
            isPersonal: true,
            cancellationToken: cancellationToken);

        LogMeasured(logger, query.From.Id, result.Centimeters, result.Nickname);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Замер для пользователя {UserId}: {Centimeters} см ({Nickname})")]
    private static partial void LogMeasured(ILogger logger, long userId, int centimeters, string nickname);
}
