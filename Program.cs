using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Services.ApplicationCommands;

Console.WriteLine("Starting goatbot");

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", false, false);
builder.Services
    .AddDiscordGateway(options =>
    {
        options.Intents = GatewayIntents.GuildMessages
                          // | GatewayIntents.DirectMessages
                          | GatewayIntents.MessageContent
                          // | GatewayIntents.DirectMessageReactions
                          | GatewayIntents.GuildMessageReactions;
        options.Token = builder.Configuration["Token"];
    })
    .AddGatewayEventHandlers(typeof(Program).Assembly)
    .AddApplicationCommands<ApplicationCommandInteraction, ApplicationCommandContext>();

var host = builder.Build()
    .UseGatewayEventHandlers();

await host.RunAsync();