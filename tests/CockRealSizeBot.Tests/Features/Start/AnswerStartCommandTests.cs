using CockRealSizeBot.Bot.Features.Start;
using CockRealSizeBot.Bot.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CockRealSizeBot.Tests.Features.Start;

public sealed class AnswerStartCommandTests
{
    private const long ChatId = 555_000_222;

    [Fact]
    public async Task Replies_into_the_chat_the_message_came_from()
    {
        var (bot, handler) = Arrange();

        await handler.HandleAsync(PrivateMessage("/start"), TestContext.Current.CancellationToken);

        Assert.Equal(ChatId, bot.SingleRequest<SendMessageRequest>().ChatId);
    }

    [Fact]
    public async Task Greeting_mentions_the_bot_so_it_can_be_typed_by_hand()
    {
        var (bot, handler) = Arrange();

        await handler.HandleAsync(PrivateMessage("/start"), TestContext.Current.CancellationToken);

        Assert.Contains("@test_size_bot", bot.SingleRequest<SendMessageRequest>().Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Button_switches_the_user_into_inline_mode()
    {
        var (bot, handler) = Arrange();

        await handler.HandleAsync(PrivateMessage("/start"), TestContext.Current.CancellationToken);

        var markup = Assert.IsType<InlineKeyboardMarkup>(bot.SingleRequest<SendMessageRequest>().ReplyMarkup);
        var button = Assert.Single(Assert.Single(markup.InlineKeyboard));

        Assert.Equal(StartMessages.ButtonText, button.Text);
        // Именно switch_inline_query: он открывает выбор чата, а не пишет в текущий.
        Assert.Equal(StartMessages.ButtonQuery, button.SwitchInlineQuery);
        Assert.Null(button.SwitchInlineQueryCurrentChat);
    }

    [Fact]
    public async Task Bot_username_is_fetched_once_even_across_several_greetings()
    {
        var (bot, handler) = Arrange();

        await handler.HandleAsync(PrivateMessage("/start"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(PrivateMessage("привет"), TestContext.Current.CancellationToken);
        await handler.HandleAsync(PrivateMessage("/start"), TestContext.Current.CancellationToken);

        Assert.Single(bot.RequestsOf<GetMeRequest>());
        Assert.Equal(3, bot.RequestsOf<SendMessageRequest>().Count);
    }

    private static (FakeTelegramBotClient Bot, AnswerStartCommand Handler) Arrange()
    {
        var bot = new FakeTelegramBotClient();

        return (bot, new AnswerStartCommand(bot, new BotIdentity(bot), NullLogger<AnswerStartCommand>.Instance));
    }

    private static Message PrivateMessage(string text) => new()
    {
        Id = 1,
        Text = text,
        Chat = new Chat { Id = ChatId, Type = ChatType.Private },
        From = new User { Id = 777, FirstName = "Тестовый" },
    };
}
