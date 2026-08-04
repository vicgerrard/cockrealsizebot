using CockRealSizeBot.Bot.Configuration;
using Microsoft.Extensions.Options;

namespace CockRealSizeBot.Bot.Features.Measurement;

/// <summary>
/// Границы суток в настроенном часовом поясе. Отсюда берётся и «день замера»
/// (он же ключ детерминированности), и время до следующего замера.
/// </summary>
internal sealed class DailyCycle
{
    private readonly TimeProvider clock;
    private readonly TimeZoneInfo zone;

    public DailyCycle(IOptions<BotOptions> options, TimeProvider clock)
    {
        this.clock = clock;

        var id = options.Value.TimeZone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"Часовой пояс '{id}' из Bot:TimeZone не найден. Ожидается IANA-идентификатор, например Europe/Moscow.",
                ex);
        }
    }

    /// <summary>Текущая календарная дата в часовом поясе бота.</summary>
    public DateOnly CurrentDay() => DateOnly.FromDateTime(LocalNow());

    /// <summary>Сколько осталось до полуночи. Никогда не ноль и не больше суток.</summary>
    public TimeSpan UntilNextDay()
    {
        var now = LocalNow();
        return now.Date.AddDays(1) - now;
    }

    private DateTime LocalNow() => TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime;
}
