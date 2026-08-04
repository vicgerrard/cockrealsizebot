using System.ComponentModel.DataAnnotations;

namespace CockRealSizeBot.Bot.Configuration;

/// <summary>
/// Настройки бота. Токен и соль — секреты, держим их в user-secrets (dev)
/// и в переменных окружения (prod), а не в appsettings.json.
/// </summary>
public sealed class BotOptions
{
    public const string SectionName = "Bot";

    /// <summary>Токен, выданный @BotFather.</summary>
    [Required(AllowEmptyStrings = false)]
    public required string Token { get; init; }

    /// <summary>
    /// Соль для детерминированного хэша. Меняется — меняются все результаты,
    /// поэтому в проде фиксируется один раз и не трогается.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(16)]
    public required string Salt { get; init; }

    /// <summary>
    /// Часовой пояс, по границе суток которого сбрасывается ежедневный замер.
    /// </summary>
    public string TimeZone { get; init; } = "Europe/Moscow";
}
