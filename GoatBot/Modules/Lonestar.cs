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

[RequireGuild(595687467827462144)]
public class Lonestar : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GoatbotDbContext _dbCtx;
    private readonly LonestarAPIClient _lonestarClient;
    private readonly IConfiguration _config;
    private readonly DiscordSocketClient _client;
    
    public Lonestar(GoatbotDbContext dbCtx, LonestarAPIClient lonestarClient, IConfiguration config, DiscordSocketClient client)
    {
        _dbCtx = dbCtx;
        _lonestarClient = lonestarClient;
        _config = config;
        _client = client;
    }

    [SlashCommand("park",
        "Inform Lonestar Towing that you will be staying at the grandon household overnight in a guest spot")]
    public async Task Park(IUser? user = null, [MinValue(1)][MaxValue(2)] ushort days = 1)
    {
        var resolvedUser = user ?? Context.User;
        
        await DeferAsync(true);

        var driver = await _dbCtx.Drivers.SingleOrDefaultAsync(x => x.DiscordUserId == resolvedUser.Id);

        if (driver == null)
        {
            if (resolvedUser == null) await  FollowupAsync("You are not registered, contact gring", ephemeral: true);
            else await FollowupAsync("The specified user is not registered, contact gring", ephemeral: true);
            return;
        }
        
        try
        {
            await _lonestarClient.IssuePermit(new PermitRequest
            {
                PermitDays = days,
                PlateNumber = driver.PlateNumber,
                Name = driver.Name,
                Email = driver.Email,
                VehicleColorCode = driver.VehicleColorCode,
                VehicleMake = driver.VehicleMake,
                VehicleModel = driver.VehicleModel,
                PlateStateCode = driver.PlateStateCode,
            });

            var message = $"Permit issued to {driver.Name} with plate {driver.PlateNumber} for {days} day(s). Lonestar will email {driver.Email}";

            await FollowupAsync(message);
            await Context.User.SendMessageAsync(message);
            if (Context.User.Id != resolvedUser.Id) resolvedUser.SendMessageAsync(message);
            foreach (var notificationUserID in _config.GetSection("LonestarAPI:PermitNotifications").Get<ulong[]>())
            {
                if (notificationUserID == Context.User.Id) continue;
                if (notificationUserID == resolvedUser.Id) continue;
                var notificationUser = await _client.GetUserAsync(notificationUserID);
                notificationUser?.SendMessageAsync(message);
            }
        }
        catch (Exception ex)
        {            
            await FollowupAsync("Something went wrong! Please inform the grandon immediately\n" + ex.Message);
        }
        
    }
}