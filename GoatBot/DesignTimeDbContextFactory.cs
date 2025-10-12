using Goatbot.Data;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Goatbot;

public class DesignTimeDbContextFactory: IDesignTimeDbContextFactory<GoatbotDbContext>
{
    // Static since dependency injection is not managing this class - initialized in static constructor
    private static readonly IConfiguration Config;

    static DesignTimeDbContextFactory()
    {
        Config = StaticConfigFactory.LoadConfig();
    }
    
    public GoatbotDbContext CreateDbContext(string[] args)
    {
        return new GoatbotDbContext(Config);
    }
}