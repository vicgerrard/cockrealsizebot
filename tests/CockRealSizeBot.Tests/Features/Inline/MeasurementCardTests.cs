using CockRealSizeBot.Bot.Features.Inline;
using CockRealSizeBot.Bot.Features.Measurement;

namespace CockRealSizeBot.Tests.Features.Inline;

public sealed class MeasurementCardTests
{
    [Fact]
    public void Card_opens_with_the_nickname_and_the_number()
    {
        var card = MeasurementCard.Render(new MeasureUser.Result(23, "Лоллипап", "Тяжёлая артиллерия", "😯"));

        var firstLine = card.Split(Environment.NewLine)[0];

        Assert.Equal("Лоллипап у меня 23 см 😯", firstLine);
    }

    [Fact]
    public void Card_ends_on_the_rank()
    {
        var card = MeasurementCard.Render(new MeasureUser.Result(23, "Лоллипап", "Тяжёлая артиллерия", "😯"));

        // Про кулдаун в карточке не пишем — она уходит в чат и живёт там вечно,
        // а «завтра» через сутки станет враньём.
        Assert.EndsWith("Звание: Тяжёлая артиллерия", card, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(18)]
    [InlineData(35)]
    public void Ruler_length_matches_the_result(int centimeters)
    {
        var card = MeasurementCard.Render(new MeasureUser.Result(centimeters, "Кто-то", "Звание", "🙂"));

        var ruler = card.Split(Environment.NewLine).Single(line => line.Contains('▶', StringComparison.Ordinal));

        Assert.Equal(centimeters, ruler.Count(symbol => symbol == '▬'));
        Assert.EndsWith("▶", ruler, StringComparison.Ordinal);
    }

    [Fact]
    public void Dropdown_entry_does_not_leak_the_result()
    {
        // Вся соль шутки — в реакции чата, поэтому в выпадашке числа быть не должно.
        Assert.DoesNotContain("см", MeasurementCard.ResultTitle, StringComparison.Ordinal);
        Assert.DoesNotContain("см", MeasurementCard.ResultDescription, StringComparison.Ordinal);
    }
}
