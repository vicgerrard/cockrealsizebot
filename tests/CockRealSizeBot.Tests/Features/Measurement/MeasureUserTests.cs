using CockRealSizeBot.Bot.Features.Measurement;

namespace CockRealSizeBot.Tests.Features.Measurement;

public sealed class MeasureUserTests
{
    private const long UserId = 777_000_111;

    [Fact]
    public void Same_user_on_same_day_always_gets_the_same_result()
    {
        var clock = TestSubjects.Clock();
        var handler = TestSubjects.Handler(clock);

        var morning = handler.Measure(new MeasureUser.Query(UserId));

        // Двигаемся внутри тех же суток по МСК: 09:00 UTC → 20:00 UTC = 12:00 → 23:00 МСК.
        clock.Advance(TimeSpan.FromHours(11));
        var evening = handler.Measure(new MeasureUser.Query(UserId));

        Assert.Equal(morning, evening);
    }

    [Fact]
    public void Result_changes_after_midnight_in_the_configured_time_zone()
    {
        // 20:30 МСК 4 августа.
        var clock = TestSubjects.Clock(new DateTimeOffset(2026, 8, 4, 17, 30, 0, TimeSpan.Zero));
        var handler = TestSubjects.Handler(clock);

        var beforeMidnight = MeasureEveryone(handler);

        // 00:30 МСК 5 августа — по UTC сутки ещё те же, по Москве уже новые.
        clock.Advance(TimeSpan.FromHours(4));
        var afterMidnight = MeasureEveryone(handler);

        // Отдельному пользователю может выпасть то же число — шкала конечная.
        // Поэтому смотрим на выборку: совпасть должны единицы, а не все.
        var unchanged = beforeMidnight.Zip(afterMidnight).Count(pair => pair.First == pair.Second);

        Assert.True(unchanged < 20, $"После полуночи не изменилось {unchanged} результатов из 100");
    }

    [Fact]
    public void Nicknames_within_a_tier_are_used_evenly()
    {
        var handler = TestSubjects.Handler();

        var usage = Enumerable.Range(1, 20_000)
            .Select(id => handler.Measure(new MeasureUser.Query(id)))
            .Where(result => result.Rank == "Уверенный середняк")
            .GroupBy(result => result.Nickname)
            .Select(group => group.Count())
            .ToList();

        // Прозвище выбирается независимо от размера, поэтому внутри разряда
        // все варианты должны встречаться примерно одинаково часто.
        Assert.Equal(8, usage.Count);
        Assert.True(usage.Max() < usage.Min() * 1.5, $"Перекос прозвищ: от {usage.Min()} до {usage.Max()}");
    }

    private static List<int> MeasureEveryone(MeasureUser.Handler handler) =>
        [.. Enumerable.Range(1, 100).Select(id => handler.Measure(new MeasureUser.Query(id)).Centimeters)];

    [Fact]
    public void Different_users_get_independent_results()
    {
        var handler = TestSubjects.Handler();

        var results = Enumerable.Range(1, 200)
            .Select(id => handler.Measure(new MeasureUser.Query(id)).Centimeters)
            .Distinct()
            .ToList();

        // Не требуем уникальности — шкала всего 35 значений. Требуем разнообразия.
        Assert.True(results.Count > 10, $"Слишком мало различных значений: {results.Count}");
    }

    [Fact]
    public void Changing_the_salt_reshuffles_everyone()
    {
        var clock = TestSubjects.Clock();

        var original = TestSubjects.Handler(clock);
        var reshuffled = TestSubjects.Handler(clock, salt: "совершенно-другая-соль");

        var changed = Enumerable.Range(1, 100)
            .Count(id => original.Measure(new MeasureUser.Query(id)).Centimeters
                      != reshuffled.Measure(new MeasureUser.Query(id)).Centimeters);

        Assert.True(changed > 80, $"Смена соли изменила лишь {changed} результатов из 100");
    }

    [Fact]
    public void Result_always_stays_within_the_scale()
    {
        var handler = TestSubjects.Handler();

        foreach (var id in Enumerable.Range(1, 5_000))
        {
            var result = handler.Measure(new MeasureUser.Query(id));

            Assert.InRange(result.Centimeters, MeasurementTiers.MinCentimeters, MeasurementTiers.MaxCentimeters);
        }
    }

    [Fact]
    public void Distribution_is_bell_shaped_not_uniform()
    {
        var handler = TestSubjects.Handler();

        var values = Enumerable.Range(1, 10_000)
            .Select(id => handler.Measure(new MeasureUser.Query(id)).Centimeters)
            .ToList();

        var middle = values.Count(cm => cm is >= 13 and <= 23);
        var edges = values.Count(cm => cm is <= 5 or >= 31);

        // При равномерном распределении середина дала бы ~31%, края ~14%.
        Assert.True(middle > values.Count * 0.55, $"Середина шкалы недобрала: {middle} из {values.Count}");
        Assert.True(edges < values.Count * 0.05, $"Края шкалы выпадают слишком часто: {edges} из {values.Count}");
    }

    [Fact]
    public void Nickname_and_rank_match_the_tier_of_the_result()
    {
        var handler = TestSubjects.Handler();

        foreach (var id in Enumerable.Range(1, 1_000))
        {
            var result = handler.Measure(new MeasureUser.Query(id));
            var tier = MeasurementTiers.For(result.Centimeters);

            Assert.Equal(tier.Rank, result.Rank);
            Assert.Equal(tier.Emoji, result.Emoji);
            Assert.Contains(result.Nickname, tier.Nicknames);
        }
    }
}
