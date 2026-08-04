using System.Text;
using CockRealSizeBot.Bot.Features.Measurement;

namespace CockRealSizeBot.Bot.Features.Inline;

/// <summary>
/// Все тексты, которые видит пользователь, собраны здесь — правка формулировок
/// не должна превращаться в поиск по проекту.
/// </summary>
internal static class MeasurementCard
{
    /// <summary>Заголовок пункта в выпадашке inline-режима.</summary>
    public const string ResultTitle = "📏 Измерить достоинство";

    /// <summary>Подпись под заголовком. Результат намеренно не раскрываем — вся соль в чате.</summary>
    public const string ResultDescription = "Результат появится в чате";

    /// <summary>Текст, который улетит в чат от имени пользователя.</summary>
    public static string Render(MeasureUser.Result result)
    {
        var card = new StringBuilder();

        card.Append(result.Nickname)
            .Append(" у меня ")
            .Append(result.Centimeters)
            .Append(" см ")
            .AppendLine(result.Emoji);

        return card.ToString();
    }
}
