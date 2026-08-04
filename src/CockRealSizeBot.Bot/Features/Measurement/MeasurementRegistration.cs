namespace CockRealSizeBot.Bot.Features.Measurement;

internal static class MeasurementRegistration
{
    public static IServiceCollection AddMeasurement(this IServiceCollection services)
    {
        services.AddSingleton<DailyCycle>();
        services.AddSingleton<MeasureUser.Handler>();

        return services;
    }
}
