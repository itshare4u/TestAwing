using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace TestAwing.Models;

public class TreasureHuntContextFactory : IDesignTimeDbContextFactory<TreasureHuntContext>
{
    public TreasureHuntContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TreasureHuntContext>();
        
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        // Throw exception if connection string not found
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
        }
        
        optionsBuilder.UseSqlServer(connectionString);
        
        return new TreasureHuntContext(optionsBuilder.Options);
    }
}
