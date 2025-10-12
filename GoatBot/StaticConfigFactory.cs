using Microsoft.Extensions.Configuration;

namespace Goatbot;

// We need a static method to create the IConfiguration to be injected so it can be used by both Program.cs and the DesignTimeDbContextFactory 
public static class StaticConfigFactory
{
    public static IConfiguration LoadConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }

}