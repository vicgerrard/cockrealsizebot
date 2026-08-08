using CockRealSizeBot.Bot.Features.Measurement;

namespace CockRealSizeBot.Tests.Features.Measurement;

public sealed class MeasureUserTests
{
    private const long UserId = 777_000_111;

    /// <summary>Запрос по умолчанию — для тестов, которым важен пользователь, а не запрос.</summary>
    private const string RequestId = "query-1";

    [Fact]
    public void Same_user_on_same_day_always_gets_the_same_centimeters()
    {
        var clock = TestSubjects.Clock();
        var handler = TestSubjects.Handler(clock);

        var morning = handler.Measure(new MeasureUser.Query(UserId, "morning-query"));

        // Двигаемся внутри тех же суток по МСК: 09:00 UTC → 20:00 UTC = 12:00 → 23:00 МСК.
        clock.Advance(TimeSpan.FromHours(11));
        var evening = handler.Measure(new MeasureUser.Query(UserId, "evening-query"));

        // Прозвище от запроса меняется, а сантиметры (и с ними разряд) — суточные.
        Assert.Equal(morning.Centimeters, evening.Centimeters);
        Assert.Equal(morning.Rank, evening.Rank);
    }

    [Fact]
    public void Nickname_changes_between_requests_within_the_same_day()
    {
        var handler = TestSubjects.Handler();

        var results = Enumerable.Range(1, 500)
            .Select(request => handler.Measure(new MeasureUser.Query(UserId, $"query-{request}")))
            .ToList();

        // Сантиметры от запроса не зависят вовсе.
        Assert.Single(results.Select(result => result.Centimeters).Distinct());

        // А прозвища за 500 запросов должны выпасть все, что есть в разряде.
        var tier = MeasurementTiers.For(results[0].Centimeters);
        Assert.Equal(tier.Nicknames.Count, results.Select(result => result.Nickname).Distinct().Count());
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

        // Самый населённый разряд — на нём выборка достаточна для оценки перекоса.
        var tier = MeasurementTiers.For(18);

        var usage = Enumerable.Range(1, 40_000)
            .Select(id => handler.Measure(new MeasureUser.Query(id, RequestId)))
            .Where(result => result.Rank == tier.Rank)
            .GroupBy(result => result.Nickname)
            .Select(group => group.Count())
            .ToList();

        // Прозвище выбирается независимо от размера, поэтому внутри разряда
        // все варианты должны встречаться примерно одинаково часто.
        Assert.Equal(tier.Nicknames.Count, usage.Count);
        Assert.True(usage.Max() < usage.Min() * 1.5, $"Перекос прозвищ: от {usage.Min()} до {usage.Max()}");
    }

    private static List<int> MeasureEveryone(MeasureUser.Handler handler) =>
        [.. Enumerable.Range(1, 100).Select(id => handler.Measure(new MeasureUser.Query(id, RequestId)).Centimeters)];

    [Fact]
    public void Different_users_get_independent_results()
    {
        var handler = TestSubjects.Handler();

        var results = Enumerable.Range(1, 200)
            .Select(id => handler.Measure(new MeasureUser.Query(id, RequestId)).Centimeters)
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
            .Count(id => original.Measure(new MeasureUser.Query(id, RequestId)).Centimeters
                      != reshuffled.Measure(new MeasureUser.Query(id, RequestId)).Centimeters);

        Assert.True(changed > 80, $"Смена соли изменила лишь {changed} результатов из 100");
    }

    [Fact]
    public void Result_always_stays_within_the_scale()
    {
        var handler = TestSubjects.Handler();

        foreach (var id in Enumerable.Range(1, 5_000))
        {
            var result = handler.Measure(new MeasureUser.Query(id, RequestId));

            Assert.InRange(result.Centimeters, MeasurementTiers.MinCentimeters, MeasurementTiers.MaxCentimeters);
        }
    }

    [Fact]
    public void Distribution_is_uniform_across_the_whole_scale()
    {
        var handler = TestSubjects.Handler();

        var values = Enumerable.Range(1, 35_000)
            .Select(id => handler.Measure(new MeasureUser.Query(id, RequestId)).Centimeters)
            .ToList();

        var byValue = values.GroupBy(cm => cm).ToDictionary(group => group.Key, group => group.Count());

        // Каждое значение шкалы должно встречаться, и примерно одинаково часто:
        // при колоколе края недобирали бы на порядок.
        const int span = MeasurementTiers.MaxCentimeters - MeasurementTiers.MinCentimeters + 1;
        Assert.Equal(span, byValue.Count);

        var expected = values.Count / (double)span;
        var least = byValue.Values.Min();
        var most = byValue.Values.Max();

        Assert.True(least > expected * 0.8, $"Значение выпадает слишком редко: {least} при ожидаемых {expected:F0}");
        Assert.True(most < expected * 1.2, $"Значение выпадает слишком часто: {most} при ожидаемых {expected:F0}");
    }

    [Fact]
    public void Nickname_and_rank_match_the_tier_of_the_result()
    {
        var handler = TestSubjects.Handler();

        foreach (var id in Enumerable.Range(1, 1_000))
        {
            var result = handler.Measure(new MeasureUser.Query(id, RequestId));
            var tier = MeasurementTiers.For(result.Centimeters);

            Assert.Equal(tier.Rank, result.Rank);
            Assert.Equal(tier.Emoji, result.Emoji);
            Assert.Contains(result.Nickname, tier.Nicknames);
        }
    }
}
