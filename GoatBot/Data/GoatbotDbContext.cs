using Goatbot.Data.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Goatbot.Data;

public class GoatbotDbContext : DbContext
{
    private IConfiguration _config;

    public DbSet<Driver> Drivers { get; set; }

    
    public GoatbotDbContext(IConfiguration config)
    {
        _config = config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql($"Host={_config.GetValue<string>("Database:Host")};Database={_config.GetValue<string>("Database:Database")};Username={_config.GetValue<string>("Database:Username")};Password={_config.GetValue<string>("Database:Password")}; Include Error Detail=true");
}