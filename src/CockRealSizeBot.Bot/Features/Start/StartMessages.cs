namespace CockRealSizeBot.Bot.Features.Start;

/// <summary>
/// Тексты приветствия. Задача экрана одна — объяснить, что бот работает не здесь,
/// а в любом чате через inline-режим.
/// </summary>
internal static class StartMessages
{
    public const string ButtonText = "Измерить в чате";

    /// <summary>
    /// Пустой запрос: кнопка откроет выбор чата и подставит туда только упоминание
    /// бота, чтобы результат выпал сразу.
    /// </summary>
    public const string ButtonQuery = "";

    public static string Greeting(string botUsername) =>
        $"""
        Здесь мерить нечего — я работаю в других чатах.

        Наберите в любом чате @{botUsername} и выберите результат. Или жмите кнопку ниже.

        Замер один на сутки: до полуночи цифра не изменится, как бы вы ни старались.
        """;
}
