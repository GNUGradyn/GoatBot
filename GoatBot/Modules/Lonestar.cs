using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Goatbot.Data;
using Goatbot.Models;
using Goatbot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Modules;

public class Lonestar : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GoatbotDbContext _dbCtx;
    private readonly LonestarAPIClient _lonestarClient;

    public Lonestar(GoatbotDbContext dbCtx, LonestarAPIClient lonestarClient)
    {
        _dbCtx = dbCtx;
        _lonestarClient = lonestarClient;
    }

    [SlashCommand("park",
        "Inform Lonestar Towing that you will be staying at the grandon household overnight in a guest spot")]
    public async Task Park(IUser? user = null, [MinValue(1)][MaxValue(2)] ushort days = 1)
    {
        var userId = user?.Id ?? Context.User.Id;
        
        await DeferAsync();

        var driver = await _dbCtx.Drivers.SingleOrDefaultAsync(x => x.DiscordUserId == userId);

        if (driver == null)
        {
            if (user == null) await RespondAsync("The specified user is not registered, contact gring", ephemeral: true);
            else await RespondAsync("You are not registered, contact gring", ephemeral: true);
            return;
        }

        await _lonestarClient.IssuePermit(new PermitRequest
        {
            PermitDays = days,
            PlateNumber = driver.PlateNumber,
            Name = driver.Name,
            Email = driver.Email,
            VehicleColorCode = driver.VehicleColorCode,
            VehicleMake = driver.VehicleMake,
            VehicleModel = driver.VehicleModel
        });

        var message = $"Permit issued to {driver.Name} with plate {driver.PlateNumber}. Expect an email from Lonestar at {driver.Email}";
        
        await RespondAsync(message, ephemeral: true);
        await Context.Channel.SendMessageAsync(message);
    }
}