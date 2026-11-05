using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace GeoTag.Bot;

/// <summary>
/// Handles the <c>/start</c> command by sending a reply keyboard with a location request button.
/// </summary>
/// <remarks>
/// <para>
/// The reply keyboard contains a single <see cref="KeyboardButton"/> with
/// <see cref="KeyboardButton.RequestLocation"/> set to <see langword="true"/>.
/// When the user taps the button, Telegram sends the device's current location
/// back to the bot as a regular location message, which is then handled by
/// <see cref="LocationHandler"/>.
/// </para>
/// <para>
/// The keyboard is configured as persistent (<see cref="ReplyKeyboardMarkup.IsPersistent"/>
/// is <see langword="true"/>), so it remains visible after the button is pressed and
/// after subsequent messages. This allows the user to send coordinates repeatedly
/// without re-issuing <c>/start</c>.
/// </para>
/// </remarks>
/// <param name="botClient">The Telegram bot client used to send replies.</param>
/// <param name="logger">The logger instance.</param>
public sealed class StartHandler(ITelegramBotClient botClient, ILogger<StartHandler> logger)
{
    /// <summary>
    /// The Telegram bot client used to send the location request keyboard.
    /// </summary>
    private readonly ITelegramBotClient _botClient = botClient;

    /// <summary>
    /// The logger used to record keyboard dispatch events.
    /// </summary>
    private readonly ILogger<StartHandler> _logger = logger;

    /// <summary>
    /// Sends a message with a persistent keyboard button that requests the user's current location.
    /// </summary>
    /// <param name="message">The incoming message that triggered the command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// The reply is sent to <see cref="Message.Chat"/> of the original command message.
    /// A short confirmation text is required because the Telegram Bot API does not allow
    /// a reply keyboard to be attached to a message without text.
    /// </remarks>
    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending location request keyboard to chat {ChatId}.", message.Chat.Id);

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton("Locate") { RequestLocation = true }
        })
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };

        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Coordinates ready. Hit the button.",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}