using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Modules;

public class Goats : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private IUser iselyn;
    private ISocketMessageChannel hbiGeneral;
    private static bool isRegistered = false;
    private static bool isReadyRegistered = false;
    
    public Goats(DiscordSocketClient discordSocketClient, IConfiguration configuration)
    {
        if (isRegistered) return;
        isRegistered = true;
        _client = discordSocketClient;
        _config = configuration;
        _client.MessageReceived += OnMessageAsync;
        _client.Ready += OnReadyAsync;
    }

    public async Task OnReadyAsync()
    {
        if (isReadyRegistered) return;
        isReadyRegistered = true;
        hbiGeneral = (ISocketMessageChannel) _client.GetChannel(595687469241073677);
    }
        
    public async Task OnMessageAsync(SocketMessage socketMessage)
    {
        if (socketMessage.Author.Id == _client.CurrentUser.Id) return;
        if (socketMessage.Content.ToLower().Contains("goat"))
        {
            await socketMessage.Channel.SendMessageAsync("Goat! :D");
            await socketMessage.AddReactionAsync(new Emoji("\u2764"));
            await socketMessage.AddReactionAsync(Emote.Parse("<:Goatcutie:529749906953338900>"));
        }
        
        if (socketMessage.Content.ToLower().Contains("bleat"))
        {
            await socketMessage.Channel.SendMessageAsync("Bleat!");
            await socketMessage.AddReactionAsync(new Emoji("\u2764"));
            await socketMessage.AddReactionAsync(Emote.Parse("<:Goatcutie:529749906953338900>"));
        }

        if (socketMessage.Content.StartsWith("gb!secho"))
        {
            if (_config.GetSection("Admins").Get<ulong[]>().Contains(socketMessage.Author.Id))
            {
                await socketMessage.DeleteAsync();
                await socketMessage.Channel.SendMessageAsync(socketMessage.Content.Substring("gb!secho".Length));
            }
        }

        if ((socketMessage.Content.ToLower().StartsWith("hug") || socketMessage.Content.ToLower().StartsWith("*hug")) && socketMessage.MentionedUsers.Select(x => x.Id).Contains(_client.CurrentUser.Id))
        {
            await socketMessage.AddReactionAsync(Emote.Parse("<:Goatcutie:529749906953338900>"));
            await socketMessage.Channel.SendMessageAsync($"hugs {socketMessage.Author.Mention}");
        }
    }
}