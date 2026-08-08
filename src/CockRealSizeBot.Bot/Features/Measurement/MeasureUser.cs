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
        /// Равномерное распределение по всей шкале: у каждого значения от 1 до 35
        /// одинаковый шанс, края выпадают не реже середины.
        /// </summary>
        private static int ToCentimeters(DeterministicEntropy entropy)
        {
            const int span = MeasurementTiers.MaxCentimeters - MeasurementTiers.MinCentimeters + 1;

            // ToUnitInterval строго меньше 1, поэтому offset не выйдет за span - 1.
            var offset = (int)(DeterministicEntropy.ToUnitInterval(entropy.First) * span);

            return MeasurementTiers.MinCentimeters + Math.Min(offset, span - 1);
        }

        private static string PickNickname(MeasurementTier tier, ulong entropy) =>
            tier.Nicknames[(int)(entropy % (ulong)tier.Nicknames.Count)];
    }
}
