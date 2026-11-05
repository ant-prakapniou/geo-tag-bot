using System.Globalization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GeoTag.Bot;

/// <summary>
/// Handles incoming Telegram messages that contain a geographic location
/// and replies with a plain-text coordinate string.
/// </summary>
/// <remarks>
/// <para>
/// The handler is stateless: it does not store coordinates, track users, or keep any
/// state between calls. Each location message is formatted and echoed back to the same chat.
/// The chat history itself acts as the only persistent record of coordinates.
/// </para>
/// <para>
/// The primary constructor captures <see cref="ITelegramBotClient"/> and
/// <see cref="ILogger{TCategoryName}"/> dependencies, which are resolved by the
/// dependency injection container configured in <c>Program</c>.
/// </para>
/// </remarks>
/// <param name="botClient">The Telegram bot client used to send replies.</param>
/// <param name="logger">The logger instance.</param>
public sealed class LocationHandler(ITelegramBotClient botClient, ILogger<LocationHandler> logger)
{
    /// <summary>
    /// The Telegram bot client used to send coordinate replies.
    /// </summary>
    private readonly ITelegramBotClient _botClient = botClient;

    /// <summary>
    /// The logger used to record outgoing coordinate messages.
    /// </summary>
    private readonly ILogger<LocationHandler> _logger = logger;

    /// <summary>
    /// Processes a location message and sends the formatted coordinates back to the same chat.
    /// </summary>
    /// <param name="message">The original message that contains the location.</param>
    /// <param name="location">The geographic location received from Telegram.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// The reply is sent to <see cref="Message.Chat"/> of the original message, so the
    /// coordinates always appear in the same conversation where the location was shared.
    /// </remarks>
    public async Task HandleAsync(Message message, Location location, CancellationToken cancellationToken)
    {
        var text = FormatCoordinates(location);

        _logger.LogInformation(
            "Sending coordinates to chat {ChatId}: {Coordinates}",
            message.Chat.Id,
            text);

        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: text,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Formats a geographic location as a plain-text string with latitude and longitude.
    /// </summary>
    /// <param name="location">The geographic location to format.</param>
    /// <returns>
    /// A string in the form <c>"latitude, longitude"</c> using invariant culture
    /// and six decimal places.
    /// </returns>
    /// <remarks>
    /// Invariant culture is used to guarantee that the decimal separator is always a dot
    /// (<c>.</c>), regardless of the server's regional settings. Six decimal places provide
    /// roughly 0.1-meter resolution, which is well within the accuracy reported by Telegram.
    /// </remarks>
    private static string FormatCoordinates(Location location)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:F6}, {1:F6}",
            location.Latitude,
            location.Longitude);
    }
}