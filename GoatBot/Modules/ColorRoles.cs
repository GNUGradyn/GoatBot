using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class ColorRoles : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private IMessage _reactionRoleMessage;
    private IMessageChannel _welcomeChannel;
    private IGuild _HBI;
    private List<string> _validChars;
    

    public ColorRoles(DiscordSocketClient client, IConfiguration config)
    {
        _client = client;
        _config = config;
        client.Ready += OnReady;
        client.RoleCreated += HandleRoleCreateDeleteOrModify;
        client.RoleDeleted += HandleRoleCreateDeleteOrModify;
        client.RoleUpdated += HandleRoleCreateDeleteOrModify;
        client.ReactionAdded += OnReaction;
    }

    private async Task OnReady()
    {
        _welcomeChannel = (IMessageChannel)await _client.GetChannelAsync(_config.GetValue<ulong>("WelcomeChannel"));
        _reactionRoleMessage = await _welcomeChannel.GetMessageAsync(_config.GetValue<ulong>("ReactionRoles"));
        _HBI = _client.GetGuild(_config.GetValue<ulong>("HBI"));

        await InitializeReactionRoles();
    }

    private async Task OnReaction(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
    {
        if (reaction.UserId == _client.CurrentUser.Id) return;
        await _reactionRoleMessage.RemoveReactionAsync(reaction.Emote, reaction.UserId);
        if (cachedMessage.Id == _reactionRoleMessage.Id)
        {
            IRole? role = null;
            var emoteAsUnicode = reaction.Emote.Name.ToCharArray();
            if (reaction.Emote.Name == char.ConvertFromUtf32(0x01F1EA)) // E has a special role name because it breaks @everyone to just make it E
            {
                role = _HBI.GetRole(_config.GetValue<ulong>("ERole"));
            }
            else if (emoteAsUnicode.Length == 3 && emoteAsUnicode[1] == 0xFE0F && emoteAsUnicode[2] == 0x20E3) // Numeric
            {
                role = _HBI.Roles.Single(roleToCheck => roleToCheck.Name == emoteAsUnicode[0].ToString());
            }
            else if (emoteAsUnicode.Length == 2 && (int)char.ConvertToUtf32(reaction.Emote.Name, 0) >= 0x1F1E6 && (int)char.ConvertToUtf32(reaction.Emote.Name, 0) <= 0x1F1FF) // Regional indicator (surrogate pair since its from discord)
            {
                role = _HBI.Roles.Single(roleToCheck => roleToCheck.Name == ((char)('A' + (char.ConvertToUtf32(reaction.Emote.Name, 0) - 0x1F1E6))).ToString());
            }
            else if (emoteAsUnicode.Length == 2 && emoteAsUnicode[0] == 0xD83D && emoteAsUnicode[1] == 0xDEAB) // Remove all
            {
                role = null;
            }
            else // Something else
            {
                return;
            }
            
            IGuildUser user;
            if (reaction.User.IsSpecified)
            {
                user = (reaction.User.Value as IGuildUser)!; // Cast cannot result in null since we only care about reactions on the reaction role message
            }
            else
            {
                user = await _HBI.GetUserAsync(reaction.UserId);
            }
            
            if (role != null && user.RoleIds.Contains(role.Id)) return; // User already has that color
            var colorRoleIds = _HBI.Roles.Where(roleToCheck => _validChars.Contains(roleToCheck.Name)).Select(foundRole => foundRole.Id);
            var rolesToRemove = user.RoleIds.Where(roleIdFromUser => colorRoleIds.Contains(roleIdFromUser));
            await user.RemoveRolesAsync(rolesToRemove);
            if (role != null) await user.AddRoleAsync(role);
        }
    }
    
    private async Task HandleRoleCreateDeleteOrModify(SocketRole socketRole)
    {
        await InitializeReactionRoles();

    }
    
    private async Task HandleRoleCreateDeleteOrModify(SocketRole arg1, SocketRole arg2)
    {
        await InitializeReactionRoles();
    }
    
    private async Task InitializeReactionRoles()
    {
        _validChars = _HBI.Roles.Select(role => role.Name).Where(roleName => roleName.Length == 1).ToList();
        
        foreach (var charAsString in _validChars.OrderBy(x => x)) // This order by only works because only single digits are handled. e.g. it would put "12a" after "2a"
        {
            var emoji = char.IsDigit(charAsString.Single()) ? new Emoji($"{charAsString}\uFE0F\u20E3") : new Emoji(char.ConvertFromUtf32(Convert.ToChar(charAsString) + 0x1F1A5));
            if (_reactionRoleMessage.Reactions.ContainsKey(emoji))
            {
                var reactionUsers = (await _reactionRoleMessage.GetReactionUsersAsync(emoji, 500).FlattenAsync()).ToArray();
                if (reactionUsers.Length <= 1) continue;
                foreach (var reactionUser in reactionUsers)
                {
                    await _reactionRoleMessage.RemoveReactionAsync(emoji, reactionUser);
                }
            }
            else
            {
                await _reactionRoleMessage.AddReactionAsync(emoji);
            }
        }

        await _reactionRoleMessage.AddReactionAsync(new Emoji("\uD83D\uDEAB"));
    }
}