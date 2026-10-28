// Registers the slash commands with Discord and routes incoming slash commands to PriceSlashModule.

using Discord.Interactions;
using Discord.WebSocket;
using ICCD.UltimatePriceBot.App.Configuration;
using ICCD.UltimatePriceBot.App.Modules;
using Microsoft.Extensions.Options;

namespace ICCD.UltimatePriceBot.App.Services;

/// <summary>
/// Handles slash command registration and execution.
/// </summary>
public class InteractionHandlingService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly IOptions<AppSettings> _appSettings;
    private bool _registered;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractionHandlingService"/> class.
    /// </summary>
    /// <param name="client">Discord client.</param>
    /// <param name="interactions">Interaction service.</param>
    /// <param name="services">Service provider.</param>
    /// <param name="appSettings">App settings.</param>
    public InteractionHandlingService(DiscordSocketClient client, InteractionService interactions, IServiceProvider services, IOptions<AppSettings> appSettings)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Loads the slash command module and hooks up the event handlers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await _interactions.AddModuleAsync<PriceSlashModule>(_services);
        _client.Ready += RegisterCommandsAsync;
        _client.InteractionCreated += HandleInteractionAsync;
    }

    private async Task RegisterCommandsAsync()
    {
        // Ready fires again on every reconnect; only register once per run.
        if (_registered)
        {
            return;
        }

        _registered = true;

        // With GuildId set, commands show up in that server instantly. Without it, they're registered globally.
        var guildId = _appSettings.Value.GuildId;
        if (guildId.HasValue)
        {
            await _interactions.RegisterCommandsToGuildAsync(guildId.Value, true);
        }
        else
        {
            await _interactions.RegisterCommandsGloballyAsync(true);
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);
        await _interactions.ExecuteCommandAsync(context, _services);
    }
}
