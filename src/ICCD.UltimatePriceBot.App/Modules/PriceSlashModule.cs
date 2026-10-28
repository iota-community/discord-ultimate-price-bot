// Slash-command version of the price commands (/p, /pi, /price, /rice): compact IOTA card, public replies.
// Same data and same embeds as the old text commands; replies are public.
// Replaces the text commands so the bot no longer needs the Message Content intent.

using Discord;
using Discord.Interactions;
using ICCD.UltimatePriceBot.App.Services.PriceData;

namespace ICCD.UltimatePriceBot.App.Modules;

/// <summary>
/// Price slash commands.
/// </summary>
public class PriceSlashModule : InteractionModuleBase<SocketInteractionContext>
{
    // Same per-command cooldown as the live text commands (2s on the box; upstream had 30s).
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(2);
    private static readonly Dictionary<string, DateTime> LastRequests = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly object LastRequestsLock = new();

    private readonly PriceDataService _priceDataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PriceSlashModule"/> class.
    /// </summary>
    /// <param name="priceDataService">Price data service.</param>
    public PriceSlashModule(PriceDataService priceDataService)
    {
        _priceDataService = priceDataService;
    }

    // Matches the live config: "p", "price", "rice" all show the compact IOTA card
    // (no relations). "pi" used to be IOTA + Shimmer; Shimmer is offline, so it's the same card now.
    // All four share one cooldown, like the live text commands (cooldown is keyed by token).

    /// <summary>/p - compact IOTA price card.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [EnabledInDm(false)]
    [SlashCommand("p", "Show the current IOTA price")]
    public Task PAsync() => CompactIotaAsync();

    /// <summary>/pi - same card (Shimmer half dropped).</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [EnabledInDm(false)]
    [SlashCommand("pi", "Show the current IOTA price")]
    public Task PiAsync() => CompactIotaAsync();

    /// <summary>/price - same card.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [EnabledInDm(false)]
    [SlashCommand("price", "Show the current IOTA price")]
    public Task PriceAsync() => CompactIotaAsync();

    /// <summary>/rice - same card.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [EnabledInDm(false)]
    [SlashCommand("rice", "Show the current IOTA price")]
    public Task RiceAsync() => CompactIotaAsync();

    private Task CompactIotaAsync()
        => RunAsync("iota", async () => (await _priceDataService.GetPriceDataAsync("iota")).ToConciseEmbed());

    private async Task RunAsync(string cooldownKey, Func<Task<Embed>> buildEmbed)
    {
        // Cooldown: instead of the old 😡 reaction, tell only the requester (ephemeral).
        lock (LastRequestsLock)
        {
            LastRequests.TryGetValue(cooldownKey, out var last);
            if (DateTime.Now - last < Cooldown)
            {
                _ = RespondAsync("The price was just posted - scroll up 👆", ephemeral: true);
                return;
            }

            LastRequests[cooldownKey] = DateTime.Now;
        }

        // Public "thinking..." placeholder, so the price fetch can take longer than Discord's 3s limit.
        await DeferAsync();

        try
        {
            var embed = await buildEmbed();
            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Price request failed: {ex}");
            await FollowupAsync("Couldn't fetch the price right now, please try again in a moment.", ephemeral: true);
        }
    }
}
