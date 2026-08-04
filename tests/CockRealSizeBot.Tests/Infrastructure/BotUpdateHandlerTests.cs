using CockRealSizeBot.Bot.Features.Inline;
using CockRealSizeBot.Bot.Features.Start;
using CockRealSizeBot.Bot.Infrastructure;
using CockRealSizeBot.Bot.Infrastructure.Polling;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramInlineQuery = Telegram.Bot.Types.InlineQuery;

namespace CockRealSizeBot.Tests.Infrastructure;

public sealed class BotUpdateHandlerTests
{
    [Fact]
    public async Task Inline_query_goes_to_the_measurement_slice()
    {
        var (bot, handler) = Arrange();

        var update = new Update
        {
            InlineQuery = new TelegramInlineQuery
            {
                Id = "query-1",
                From = new User { Id = 777, FirstName = "Тестовый" },
                Query = string.Empty,
            },
        };

        await handler.HandleUpdateAsync(bot, update, TestContext.Current.CancellationToken);

        Assert.Single(bot.RequestsOf<AnswerInlineQueryRequest>());
        Assert.Empty(bot.RequestsOf<SendMessageRequest>());
    }

    [Fact]
    public async Task Private_message_goes_to_the_start_screen()
    {
        var (bot, handler) = Arrange();

        await handler.HandleUpdateAsync(bot, MessageUpdate(ChatType.Private, "/start"), TestContext.Current.CancellationToken);

        Assert.Single(bot.RequestsOf<SendMessageRequest>());
    }

    [Fact]
    public async Task Any_private_text_gets_the_start_screen_not_only_the_command()
    {
        var (bot, handler) = Arrange();

        await handler.HandleUpdateAsync(bot, MessageUpdate(ChatType.Private, "а как этим пользоваться"), TestContext.Current.CancellationToken);

        Assert.Single(bot.RequestsOf<SendMessageRequest>());
    }

    [Theory]
    [InlineData(ChatType.Group)]
    [InlineData(ChatType.Supergroup)]
    public async Task Group_messages_are_ignored(ChatType chatType)
    {
        var (bot, handler) = Arrange();

        await handler.HandleUpdateAsync(bot, MessageUpdate(chatType, "/start"), TestContext.Current.CancellationToken);

        Assert.Empty(bot.SentRequests);
    }

    [Fact]
    public async Task Messages_without_text_are_ignored()
    {
        var (bot, handler) = Arrange();

        var update = new Update
        {
            Message = new Message
            {
                Id = 1,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                Sticker = new Sticker(),
            },
        };

        await handler.HandleUpdateAsync(bot, update, TestContext.Current.CancellationToken);

        Assert.Empty(bot.SentRequests);
    }

    private static (FakeTelegramBotClient Bot, BotUpdateHandler Handler) Arrange()
    {
        var bot = new FakeTelegramBotClient();
        var clock = TestSubjects.Clock();

        var inline = new AnswerMeasurementQuery(
            bot,
            TestSubjects.Handler(clock),
            TestSubjects.Cycle(clock),
            NullLogger<AnswerMeasurementQuery>.Instance);

        var start = new AnswerStartCommand(bot, new BotIdentity(bot), NullLogger<AnswerStartCommand>.Instance);

        return (bot, new BotUpdateHandler(inline, start, NullLogger<BotUpdateHandler>.Instance));
    }

    private static Update MessageUpdate(ChatType chatType, string text) => new()
    {
        Message = new Message
        {
            Id = 1,
            Text = text,
            Chat = new Chat { Id = 555, Type = chatType },
            From = new User { Id = 777, FirstName = "Тестовый" },
        },
    };
}
