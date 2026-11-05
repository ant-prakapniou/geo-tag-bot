using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

namespace GeoTag.Bot;

/// <summary>
/// Builds and runs the application host for the GeoTag bot.
/// </summary>
/// <remarks>
/// <para>
/// This class encapsulates all host configuration: reading the bot token from the
/// environment, registering services in the dependency injection container, and
/// starting the generic host. Keeping this logic in a dedicated class allows
/// <c>Program</c> to remain a minimal entry point and makes the configuration
/// independently testable and documentable.
/// </para>
/// <para>
/// The bot token is read exclusively from the <c>TELEGRAM_BOT_TOKEN</c> environment
/// variable. No configuration files are used, which keeps the token out of the
/// repository and simplifies deployment on platforms such as Railway.
/// </para>
/// </remarks>
internal static class Application
{
    /// <summary>
    /// The name of the environment variable that holds the Telegram bot token.
    /// </summary>
    private const string TokenEnvironmentVariable = "TELEGRAM_BOT_TOKEN";

    /// <summary>
    /// Builds the host, registers all required services, and runs the application
    /// until the host is stopped.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the host builder.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>TELEGRAM_BOT_TOKEN</c> environment variable is not set
    /// or contains only whitespace.
    /// </exception>
    public static async Task RunAsync(string[] args)
    {
        var token = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                $"Environment variable '{TokenEnvironmentVariable}' is not set. " +
                "Provide a valid Telegram bot token before starting the application.");
        }

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(token));
        builder.Services.AddSingleton<LocationHandler>();
        builder.Services.AddSingleton<StartHandler>();
        builder.Services.AddHostedService<BotPollingService>();

        var host = builder.Build();
        await host.RunAsync();
    }
}