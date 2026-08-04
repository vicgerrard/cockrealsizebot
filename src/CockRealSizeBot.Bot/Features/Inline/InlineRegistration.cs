namespace CockRealSizeBot.Bot.Features.Inline;

internal static class InlineRegistration
{
    public static IServiceCollection AddInlineMeasurement(this IServiceCollection services)
    {
        services.AddSingleton<AnswerMeasurementQuery>();

        return services;
    }
}
