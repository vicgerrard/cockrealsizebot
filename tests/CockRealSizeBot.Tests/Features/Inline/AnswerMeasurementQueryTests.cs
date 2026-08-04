using CockRealSizeBot.Bot.Features.Inline;
using CockRealSizeBot.Bot.Features.Measurement;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using TelegramInlineQuery = Telegram.Bot.Types.InlineQuery;

namespace CockRealSizeBot.Tests.Features.Inline;

public sealed class AnswerMeasurementQueryTests
{
    private const long UserId = 777_000_111;

    [Fact]
    public async Task Answers_with_a_single_article_carrying_the_card()
    {
        var (bot, handler, expected) = Arrange();

        await handler.HandleAsync(InlineQueryFrom(UserId), TestContext.Current.CancellationToken);

        var request = bot.SingleRequest<AnswerInlineQueryRequest>();
        var article = Assert.IsType<InlineQueryResultArticle>(Assert.Single(request.Results));
        var content = Assert.IsType<InputTextMessageContent>(article.InputMessageContent);

        Assert.Equal(MeasurementCard.Render(expected), content.MessageText);
    }

    [Fact]
    public async Task Answer_is_personal_so_telegram_does_not_share_it_between_users()
    {
        var (bot, handler, _) = Arrange();

        await handler.HandleAsync(InlineQueryFrom(UserId), TestContext.Current.CancellationToken);

        Assert.True(bot.SingleRequest<AnswerInlineQueryRequest>().IsPersonal);
    }

    [Fact]
    public async Task Answer_is_cached_exactly_until_local_midnight()
    {
        // 21:00 МСК — до полуночи три часа.
        var clock = TestSubjects.Clock(new DateTimeOffset(2026, 8, 4, 18, 0, 0, TimeSpan.Zero));
        var (bot, handler, _) = Arrange(clock);

        await handler.HandleAsync(InlineQueryFrom(UserId), TestContext.Current.CancellationToken);

        Assert.Equal((int)TimeSpan.FromHours(3).TotalSeconds, bot.SingleRequest<AnswerInlineQueryRequest>().CacheTime);
    }

    [Fact]
    public async Task Query_text_is_ignored_so_nobody_can_measure_someone_else()
    {
        var (bot, handler, _) = Arrange();

        await handler.HandleAsync(InlineQueryFrom(UserId, query: "999999"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(InlineQueryFrom(UserId, query: string.Empty), TestContext.Current.CancellationToken);

        var cards = bot.SentRequests
            .Cast<AnswerInlineQueryRequest>()
            .Select(request => ((InputTextMessageContent)((InlineQueryResultArticle)request.Results.Single()).InputMessageContent!).MessageText)
            .Distinct()
            .ToList();

        Assert.Single(cards);
    }

    [Fact]
    public async Task Answer_echoes_the_id_of_the_incoming_query()
    {
        var (bot, handler, _) = Arrange();

        await handler.HandleAsync(InlineQueryFrom(UserId), TestContext.Current.CancellationToken);

        Assert.Equal("query-1", bot.SingleRequest<AnswerInlineQueryRequest>().InlineQueryId);
    }

    private static (FakeTelegramBotClient Bot, AnswerMeasurementQuery Handler, MeasureUser.Result Expected) Arrange(
        TimeProvider? clock = null)
    {
        clock ??= TestSubjects.Clock();

        var bot = new FakeTelegramBotClient();
        var measure = TestSubjects.Handler(clock);
        var handler = new AnswerMeasurementQuery(
            bot,
            measure,
            TestSubjects.Cycle(clock),
            NullLogger<AnswerMeasurementQuery>.Instance);

        return (bot, handler, measure.Measure(new MeasureUser.Query(UserId)));
    }

    private static TelegramInlineQuery InlineQueryFrom(long userId, string query = "") => new()
    {
        Id = "query-1",
        From = new User { Id = userId, FirstName = "Тестовый" },
        Query = query,
    };
}
