using CockRealSizeBot.Bot.Features.Measurement;

namespace CockRealSizeBot.Tests.Features.Measurement;

public sealed class DailyCycleTests
{
    [Fact]
    public void Current_day_follows_the_configured_time_zone_not_utc()
    {
        // 22:30 UTC 4 августа = 01:30 МСК уже 5 августа.
        var clock = TestSubjects.Clock(new DateTimeOffset(2026, 8, 4, 22, 30, 0, TimeSpan.Zero));
        var cycle = TestSubjects.Cycle(clock);

        Assert.Equal(new DateOnly(2026, 8, 5), cycle.CurrentDay());
    }

    [Fact]
    public void Time_until_next_day_counts_down_to_local_midnight()
    {
        // 21:00 МСК — до полуночи ровно три часа.
        var clock = TestSubjects.Clock(new DateTimeOffset(2026, 8, 4, 18, 0, 0, TimeSpan.Zero));
        var cycle = TestSubjects.Cycle(clock);

        Assert.Equal(TimeSpan.FromHours(3), cycle.UntilNextDay());
    }

    [Fact]
    public void Time_until_next_day_is_never_zero_or_longer_than_a_day()
    {
        var clock = TestSubjects.Clock(new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero));
        var cycle = TestSubjects.Cycle(clock);

        for (var minute = 0; minute < 24 * 60; minute++)
        {
            var remaining = cycle.UntilNextDay();

            Assert.InRange(remaining, TimeSpan.Zero, TimeSpan.FromDays(1));
            Assert.NotEqual(TimeSpan.Zero, remaining);

            clock.Advance(TimeSpan.FromMinutes(1));
        }
    }

    [Fact]
    public void Unknown_time_zone_fails_loudly_at_construction()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => TestSubjects.Cycle(TestSubjects.Clock(), timeZone: "Mars/Olympus_Mons"));

        Assert.Contains("Bot:TimeZone", ex.Message, StringComparison.Ordinal);
    }
}
