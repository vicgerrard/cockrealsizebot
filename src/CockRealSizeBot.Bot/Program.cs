using CockRealSizeBot.Bot.Configuration;
using CockRealSizeBot.Bot.Features.Inline;
using CockRealSizeBot.Bot.Features.Measurement;
using CockRealSizeBot.Bot.Features.Start;
using CockRealSizeBot.Bot.Infrastructure.Polling;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<BotOptions>()
    .Bind(builder.Configuration.GetSection(BotOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddMeasurement()
    .AddInlineMeasurement()
    .AddStartScreen()
    .AddTelegramBot();

var host = builder.Build();
host.Run();
