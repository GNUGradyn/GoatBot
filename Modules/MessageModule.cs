using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GoatBot.Services;

public class MessageModule : ApplicationCommandModule<ApplicationCommandContext>
{
    [GatewayEvent(nameof(GatewayClient.MessageCreate))]
    public class MessageCreateHandler(ILogger<MessageCreateHandler> logger, GatewayClient client, IConfiguration config) : IGatewayEventHandler<Message>
    {
        public async ValueTask HandleAsync(Message message)
        {
            if (message.Author.Id == client.Id) return;
            var channel = message.Channel;
            if (message.Content.ToLower().Contains("goat"))
            {
                await message.Channel.SendMessageAsync("Goat! :D");
                await message.AddReactionAsync(new ReactionEmojiProperties("\u2764"));
                await message.AddReactionAsync(new ReactionEmojiProperties("Goatcutie",529749906953338900));
            }
        
            if (message.Content.ToLower().Contains("bleat"))
            { 
                await message.Channel.SendMessageAsync("Bleat!");
                await message.AddReactionAsync(new ReactionEmojiProperties("\u2764"));
                await message.AddReactionAsync(new ReactionEmojiProperties("Goatcutie",529749906953338900));
            }

            if (message.Content.StartsWith("gb!secho"))
            {
                if (config.GetSection("Admins").Get<ulong[]>().Contains(message.Author.Id))
                {
                    await message.DeleteAsync();
                    await message.Channel.SendMessageAsync(message.Content.Substring("gb!secho".Length));
                }
            }

            if ((message.Content.ToLower().StartsWith("hug") || message.Content.ToLower().StartsWith("*hug")) && message.MentionedUsers.Select(x => x.Id).Contains(client.Id))
            {
                await message.AddReactionAsync(new ReactionEmojiProperties("Goatcutie",529749906953338900));
                await message.Channel.SendMessageAsync($"hugs {message.Author.Id}");
            }
        }
    }
}