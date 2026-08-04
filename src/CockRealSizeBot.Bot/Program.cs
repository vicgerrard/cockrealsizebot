using CockRealSizeBot.Bot.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<BotOptions>()
    .Bind(builder.Configuration.GetSection(BotOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);

// Фичи регистрируются здесь: клиент Telegram.Bot, long polling hosted service,
// обработчики inline-запросов. См. CLAUDE.md → «Как добавлять фичи».

var host = builder.Build();
host.Run();
