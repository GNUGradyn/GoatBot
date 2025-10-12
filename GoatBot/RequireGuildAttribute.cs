using Discord.Commands;

namespace Goatbot;

public class RequireGuildAttribute : PreconditionAttribute
{
    private readonly ulong[] _guildIds;

    public RequireGuildAttribute(params ulong[] guildIds)
    {
        _guildIds = guildIds;
    }

    public override Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (context.Guild == null)
            return Task.FromResult(PreconditionResult.FromError("Command must be used in a guild."));

        if (!_guildIds.Contains(context.Guild.Id))
            return Task.FromResult(PreconditionResult.FromError("This command is not available in this guild."));

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}