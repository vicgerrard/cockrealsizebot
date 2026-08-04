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
    public async Task Answer_disables_the_telegram_cache_explicitly()
    {
        var (bot, handler, _) = Arrange();

        await handler.HandleAsync(InlineQueryFrom(UserId), TestContext.Current.CancellationToken);

        // Не «null», а именно ноль: неотправленный cacheTime Telegram трактует как 300 секунд,
        // и ответ одного пользователя пять минут показывается всем остальным.
        Assert.Equal(0, bot.SingleRequest<AnswerInlineQueryRequest>().CacheTime);
    }

    [Fact]
    public async Task Every_user_is_answered_with_his_own_card()
    {
        var (bot, handler, _) = Arrange();

        foreach (var id in Enumerable.Range(1, 20))
        {
            await handler.HandleAsync(InlineQueryFrom(id), TestContext.Current.CancellationToken);
        }

        var cards = bot.RequestsOf<AnswerInlineQueryRequest>().Select(CardOf).Distinct().ToList();

        // Совпадения возможны — шкала конечная, — но одна карточка на всех означала бы,
        // что замер перестал зависеть от автора запроса.
        Assert.True(cards.Count > 5, $"На 20 пользователей всего {cards.Count} различных карточек");
    }

    [Fact]
    public async Task Query_text_is_ignored_so_nobody_can_measure_someone_else()
    {
        var (bot, handler, _) = Arrange();

        await handler.HandleAsync(InlineQueryFrom(UserId, query: "999999"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(InlineQueryFrom(UserId, query: string.Empty), TestContext.Current.CancellationToken);

        var cards = bot.RequestsOf<AnswerInlineQueryRequest>().Select(CardOf).Distinct().ToList();

        Assert.Single(cards);
    }

    [Fact]
    public async Task Answer_echoes_the_id_of_the_incoming_query()
    {
        var (bot, handler, _) = Arrange();

        await handler.HandleAsync(InlineQueryFrom(UserId), TestContext.Current.CancellationToken);

        Assert.Equal("query-1", bot.SingleRequest<AnswerInlineQueryRequest>().InlineQueryId);
    }

    private static string CardOf(AnswerInlineQueryRequest request) =>
        ((InputTextMessageContent)((InlineQueryResultArticle)request.Results.Single()).InputMessageContent!).MessageText;

    private static (FakeTelegramBotClient Bot, AnswerMeasurementQuery Handler, MeasureUser.Result Expected) Arrange()
    {
        var bot = new FakeTelegramBotClient();
        var measure = TestSubjects.Handler(TestSubjects.Clock());
        var handler = new AnswerMeasurementQuery(bot, measure, NullLogger<AnswerMeasurementQuery>.Instance);

        return (bot, handler, measure.Measure(new MeasureUser.Query(UserId)));
    }

    private static TelegramInlineQuery InlineQueryFrom(long userId, string query = "") => new()
    {
        Id = "query-1",
        From = new User { Id = userId, FirstName = "Тестовый" },
        Query = query,
    };
}
