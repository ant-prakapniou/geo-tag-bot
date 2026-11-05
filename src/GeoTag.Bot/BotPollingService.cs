using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GeoTag.Bot;

/// <summary>
/// A hosted background service that polls the Telegram Bot API for updates
/// using long polling and dispatches each update to the appropriate handler.
/// </summary>
/// <remarks>
/// <para>
/// The service runs for the entire lifetime of the host. On startup it begins
/// an infinite polling loop that repeatedly calls
/// <see cref="ITelegramBotClient.GetUpdates"/> with a long-polling timeout.
/// Received updates are dispatched to <see cref="StartHandler"/> (for the
/// <c>/start</c> command) or <see cref="LocationHandler"/> (for location messages).
/// </para>
/// <para>
/// When the host requests shutdown (for example, on <c>SIGTERM</c>), the
/// <paramref name="stoppingToken"/> passed to <see cref="ExecuteAsync"/> is
/// cancelled, the loop exits gracefully, and a final log entry is written.
/// </para>
/// </remarks>
public sealed class BotPollingService : BackgroundService
{
    /// <summary>
    /// The Telegram bot client used to fetch updates from the Bot API.
    /// </summary>
    private readonly ITelegramBotClient _botClient;

    /// <summary>
    /// The handler that processes the <c>/start</c> command.
    /// </summary>
    private readonly StartHandler _startHandler;

    /// <summary>
    /// The handler that processes incoming location messages.
    /// </summary>
    private readonly LocationHandler _locationHandler;

    /// <summary>
    /// The logger used to record polling lifecycle events and errors.
    /// </summary>
    private readonly ILogger<BotPollingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotPollingService"/> class.
    /// </summary>
    /// <param name="botClient">The Telegram bot client used to receive updates.</param>
    /// <param name="startHandler">The handler that processes the <c>/start</c> command.</param>
    /// <param name="locationHandler">The handler that processes location messages.</param>
    /// <param name="logger">The logger instance.</param>
    public BotPollingService(
        ITelegramBotClient botClient,
        StartHandler startHandler,
        LocationHandler locationHandler,
        ILogger<BotPollingService> logger)
    {
        _botClient = botClient;
        _startHandler = startHandler;
        _locationHandler = locationHandler;
        _logger = logger;
    }

    /// <summary>
    /// Starts the long polling loop and processes incoming updates until cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">
    /// A token that signals when the service should stop. It is cancelled by the host
    /// when the application is shutting down.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the lifetime of the polling loop.</returns>
    /// <remarks>
    /// <para>
    /// The loop maintains an <c>offset</c> value that is advanced after each processed update,
    /// ensuring that no update is handled twice. A long-polling timeout of 30 seconds is used
    /// to reduce the number of empty requests while keeping the bot responsive.
    /// </para>
    /// <para>
    /// Transient errors are logged and followed by a five-second delay before the loop retries.
    /// Cancellation triggered by <paramref name="stoppingToken"/> is treated as a normal
    /// shutdown and does not produce an error log entry.
    /// </para>
    /// </remarks>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bot polling service started.");

        var offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _botClient.GetUpdates(
                    offset: offset,
                    timeout: 30,
                    cancellationToken: stoppingToken);

                foreach (var update in updates)
                {
                    offset = update.Id + 1;
                    await DispatchAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while polling Telegram updates.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Bot polling service stopped.");
    }

    /// <summary>
    /// Dispatches a single update to the appropriate handler.
    /// </summary>
    /// <param name="update">The update received from Telegram.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Only text commands (currently <c>/start</c>) and location messages are handled.
    /// Updates without a message, or messages that match neither condition, are ignored.
    /// </remarks>
    private async Task DispatchAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
        {
            return;
        }

        if (message.Text == "/start")
        {
            await _startHandler.HandleAsync(message, cancellationToken);
            return;
        }

        if (message.Location is { } location)
        {
            await _locationHandler.HandleAsync(message, location, cancellationToken);
        }
    }
}