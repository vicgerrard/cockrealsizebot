using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace CockRealSizeBot.Tests;

/// <summary>
/// Клиент, который никуда не ходит, а складывает отправленные запросы в список.
/// Интерфейс маленький, поэтому фейк дешевле мок-библиотеки.
/// </summary>
internal sealed class FakeTelegramBotClient : ITelegramBotClient
{
    public List<object> SentRequests { get; } = [];

    public bool LocalBotServer => false;

    public long BotId => 424242;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public IExceptionParser ExceptionsParser { get; set; } = new DefaultExceptionParser();

    // Событиями интерфейса фейк не пользуется — они здесь только чтобы
    // удовлетворить контракт ITelegramBotClient.
#pragma warning disable CS0067
    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;

    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;
#pragma warning restore CS0067

    /// <summary>Ответ на getMe. Из него берётся @username для текстов.</summary>
    public User Me { get; init; } = new()
    {
        Id = 424242,
        IsBot = true,
        FirstName = "Тестовый бот",
        Username = "test_size_bot",
    };

    public Task<TResponse> SendRequest<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        SentRequests.Add(request);

        if (request is GetMeRequest)
        {
            return Task.FromResult((TResponse)(object)Me);
        }

        return Task.FromResult<TResponse>(default!);
    }

    public Task<bool> TestApi(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>Единственный отправленный запрос ожидаемого типа.</summary>
    public TRequest SingleRequest<TRequest>() => Assert.Single(SentRequests.OfType<TRequest>());

    public IReadOnlyList<TRequest> RequestsOf<TRequest>() => [.. SentRequests.OfType<TRequest>()];
}
