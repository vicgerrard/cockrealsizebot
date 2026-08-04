using CockRealSizeBot.Bot.Configuration;
using CockRealSizeBot.Bot.Features.Measurement;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CockRealSizeBot.Tests;

/// <summary>
/// Сборка тестируемых объектов вручную: граф зависимостей крошечный,
/// поднимать ради него хост дороже, чем собрать три объекта.
/// </summary>
internal static class TestSubjects
{
    public const string Salt = "test-salt-достаточно-длинная";

    /// <summary>Полдень 4 августа 2026 по Москве — заведомо не у границы суток.</summary>
    public static readonly DateTimeOffset Noon = new(2026, 8, 4, 9, 0, 0, TimeSpan.Zero);

    public static FakeTimeProvider Clock(DateTimeOffset? utcNow = null) => new(utcNow ?? Noon);

    public static DailyCycle Cycle(TimeProvider clock, string timeZone = "Europe/Moscow") =>
        new(Options(Salt, timeZone), clock);

    public static MeasureUser.Handler Handler(
        TimeProvider? clock = null,
        string salt = Salt,
        string timeZone = "Europe/Moscow")
    {
        clock ??= Clock();

        return new MeasureUser.Handler(Options(salt, timeZone), Cycle(clock, timeZone));
    }

    private static IOptions<BotOptions> Options(string salt, string timeZone) =>
        Microsoft.Extensions.Options.Options.Create(new BotOptions
        {
            Token = "123:test",
            Salt = salt,
            TimeZone = timeZone,
        });
}
