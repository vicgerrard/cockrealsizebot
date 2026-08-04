using CockRealSizeBot.Bot.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CockRealSizeBot.Bot.Features.Start;

/// <summary>
/// Отвечает на сообщение в личке. Сам замер здесь не делается — бот только
/// объясняет, что работает через inline, и даёт кнопку выбора чата.
/// </summary>
internal sealed partial class AnswerStartCommand(
    ITelegramBotClient bot,
    BotIdentity identity,
    ILogger<AnswerStartCommand> logger)
{
    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        var username = await identity.UsernameAsync(cancellationToken);

        // switch_inline_query открывает выбор чата и подставляет туда упоминание
        // бота — ровно тот сценарий, ради которого бот и существует.
        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithSwitchInlineQuery(StartMessages.ButtonText, StartMessages.ButtonQuery));

        await bot.SendMessage(
            message.Chat.Id,
            StartMessages.Greeting(username),
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        LogGreeted(logger, message.Chat.Id);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Приветствие отправлено в чат {ChatId}")]
    private static partial void LogGreeted(ILogger logger, long chatId);
}
