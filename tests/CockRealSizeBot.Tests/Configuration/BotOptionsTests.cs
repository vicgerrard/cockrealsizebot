using CockRealSizeBot.Bot.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CockRealSizeBot.Tests.Configuration;

public sealed class BotOptionsTests
{
    [Fact]
    public void Validation_fails_when_token_is_missing()
    {
        var options = Build(new Dictionary<string, string?>
        {
            ["Bot:Salt"] = "long-enough-salt-value",
        });

        var ex = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains(nameof(BotOptions.Token), string.Join(' ', ex.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_fails_when_salt_is_too_short()
    {
        var options = Build(new Dictionary<string, string?>
        {
            ["Bot:Token"] = "123:ABC",
            ["Bot:Salt"] = "short",
        });

        var ex = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains(nameof(BotOptions.Salt), string.Join(' ', ex.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void Complete_configuration_passes_validation()
    {
        var options = Build(new Dictionary<string, string?>
        {
            ["Bot:Token"] = "123:ABC",
            ["Bot:Salt"] = "long-enough-salt-value",
            ["Bot:TimeZone"] = "Europe/Moscow",
        });

        Assert.Equal("Europe/Moscow", options.Value.TimeZone);
    }

    private static IOptions<BotOptions> Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<BotOptions>()
            .Bind(configuration.GetSection(BotOptions.SectionName))
            .ValidateDataAnnotations();

        return services.BuildServiceProvider().GetRequiredService<IOptions<BotOptions>>();
    }
}
