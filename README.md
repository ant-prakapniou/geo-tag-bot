# SomewhereHere

A stateless Telegram bot that returns your current coordinates on demand. Built for field use — no database, no stored locations, just the chat history.

## How it works

1. Send `/start` — the bot replies with a persistent Locate button.
2. Tap **Locate** — Telegram sends your current location to the bot.
3. The bot replies with a plain-text coordinate string (`lat, lon`), ready to copy.

No coordinates are stored. The bot is stateless by design.

## Requirements

- .NET 11 SDK

- A Telegram bot token from [@BotFather](https://t.me/BotFather)

## Project structure

- `Program.cs` — entry point; delegates to `Application.RunAsync`.
- `Application.cs` — host configuration, service registration, token validation.
- `BotPollingService.cs` — long-polling background service; dispatches updates.
- `StartHandler.cs` — handles `/start`; sends the persistent **Locate** keyboard.
- `LocationHandler.cs` — handles location messages; replies with formatted coordinates.

## Local development

Set the token environment variable and run the project:

```bash
export TELEGRAM_BOT_TOKEN="your-token-here"
dotnet run --project src/GeoTag.Bot
```

On Windows PowerShell:

```powershell
$env:TELEGRAM_BOT_TOKEN="your-token-here"
dotnet run --project src/GeoTag.Bot
```

## Deployment (Railway)

1. Connect the repository to a Railway service.
2. Add `TELEGRAM_BOT_TOKEN` as a service variable.
3. Disable **Serverless** in Settings → Deploy (the bot must run continuously).
4. Enable **Wait for CI** in Settings (Railway waits for the GitHub Actions workflow before deploying).

Pushing to `main` triggers CI; if the build succeeds, Railway deploys.

## Configuration

| Variable | Required | Description |
|---|---|---|
| `TELEGRAM_BOT_TOKEN` | Yes | Token from [@BotFather](https://t.me/BotFather) |

## License

See [LICENSE](LICENSE).