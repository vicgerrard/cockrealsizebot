using Telegram.Bot;

namespace CockRealSizeBot.Bot.Infrastructure;

/// <summary>
/// Собственный @username бота. Telegram отдаёт его только через getMe,
/// а в текстах он нужен, поэтому забираем один раз и держим в памяти.
/// </summary>
internal sealed class BotIdentity(ITelegramBotClient bot)
{
    private string? username;

    public async ValueTask<string> UsernameAsync(CancellationToken cancellationToken)
    {
        // Гонка здесь безобидна: в худшем случае уйдут два одинаковых getMe.
        return username ??= await FetchUsernameAsync(cancellationToken);
    }

    private async Task<string> FetchUsernameAsync(CancellationToken cancellationToken)
    {
        var me = await bot.GetMe(cancellationToken);

        return me.Username
            ?? throw new InvalidOperationException("Telegram вернул бота без username — такого быть не должно.");
    }
}
