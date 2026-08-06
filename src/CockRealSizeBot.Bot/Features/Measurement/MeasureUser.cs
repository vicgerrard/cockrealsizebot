using CockRealSizeBot.Bot.Configuration;
using Microsoft.Extensions.Options;

namespace CockRealSizeBot.Bot.Features.Measurement;

/// <summary>
/// Слайс «замерить пользователя». Чистая функция без I/O: одинаковый вход даёт
/// одинаковый выход, поэтому суточный кулдаун получается без всякого хранилища.
/// Сантиметры зависят только от пользователя и даты, прозвище — ещё и от
/// <see cref="Query.RequestId"/>: каждый запрос даёт новое прозвище из того же разряда.
/// </summary>
public static class MeasureUser
{
    /// <param name="RequestId">
    /// Идентификатор конкретного запроса (id inline-запроса Telegram). Участвует
    /// только в выборе прозвища — сантиметры от него не зависят.
    /// </param>
    public sealed record Query(long UserId, string RequestId);

    public sealed record Result(int Centimeters, string Nickname, string Rank, string Emoji);

    internal sealed class Handler(IOptions<BotOptions> options, DailyCycle cycle)
    {
        public Result Measure(Query query)
        {
            var dailySeed = $"{query.UserId}:{cycle.CurrentDay():yyyy-MM-dd}";
            var daily = DeterministicEntropy.Derive(options.Value.Salt, dailySeed);
            var perRequest = DeterministicEntropy.Derive(options.Value.Salt, $"{dailySeed}:{query.RequestId}");

            var centimeters = ToCentimeters(daily);
            var tier = MeasurementTiers.For(centimeters);
            var nickname = PickNickname(tier, perRequest.Fourth);

            return new Result(centimeters, nickname, tier.Rank, tier.Emoji);
        }

        /// <summary>
        /// Среднее трёх независимых равномерных величин — приближение нормального
        /// распределения (центральная предельная теорема на минималках). Середина
        /// шкалы выпадает часто, края — редко и оттого смешно.
        /// </summary>
        private static int ToCentimeters(DeterministicEntropy entropy)
        {
            var bell =
                (DeterministicEntropy.ToUnitInterval(entropy.First)
                 + DeterministicEntropy.ToUnitInterval(entropy.Second)
                 + DeterministicEntropy.ToUnitInterval(entropy.Third)) / 3.0;

            const int span = MeasurementTiers.MaxCentimeters - MeasurementTiers.MinCentimeters + 1;

            // bell строго меньше 1, поэтому offset не выйдет за span - 1.
            var offset = (int)(bell * span);

            return MeasurementTiers.MinCentimeters + Math.Min(offset, span - 1);
        }

        private static string PickNickname(MeasurementTier tier, ulong entropy) =>
            tier.Nicknames[(int)(entropy % (ulong)tier.Nicknames.Count)];
    }
}
