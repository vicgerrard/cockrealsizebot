using CockRealSizeBot.Bot.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace CockRealSizeBot.Bot.Infrastructure.Polling;

internal static class TelegramRegistration
{
    private const string HttpClientName = "telegram";

    public static IServiceCollection AddTelegramBot(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName)
            // Дефолтные логгеры HttpClient пишут URL целиком, а в URL Telegram
            // лежит токен бота. Убираем их, чтобы токен не утёк в логи.
            .RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>((httpClient, serviceProvider) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<BotOptions>>().Value;

                return new TelegramBotClient(options.Token, httpClient);
            });

        services.AddSingleton<BotIdentity>();
        services.AddSingleton<BotUpdateHandler>();
        services.AddHostedService<BotPollingService>();

        return services;
    }
}
