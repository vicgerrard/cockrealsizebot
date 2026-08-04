namespace CockRealSizeBot.Bot.Features.Start;

internal static class StartRegistration
{
    public static IServiceCollection AddStartScreen(this IServiceCollection services)
    {
        services.AddSingleton<AnswerStartCommand>();

        return services;
    }
}
