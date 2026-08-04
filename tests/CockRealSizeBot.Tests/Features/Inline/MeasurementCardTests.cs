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
    public void Card_is_a_single_line()
    {
        // Карточка намеренно короткая: ни линейки, ни строки со званием,
        // ни обещания «завтра» — она уходит в чат и живёт там вечно.
        var card = MeasurementCard.Render(new MeasureUser.Result(23, "Лоллипап", "Тяжёлая артиллерия", "😯"));

        Assert.Single(card.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void Dropdown_entry_does_not_leak_the_result()
    {
        // Вся соль шутки — в реакции чата, поэтому в выпадашке числа быть не должно.
        Assert.DoesNotContain("см", MeasurementCard.ResultTitle, StringComparison.Ordinal);
        Assert.DoesNotContain("см", MeasurementCard.ResultDescription, StringComparison.Ordinal);
    }
}
