using Microsoft.EntityFrameworkCore;

namespace TreasureHunt.Models;

public class TreasureHuntContext : DbContext
{
    public TreasureHuntContext(DbContextOptions<TreasureHuntContext> options)
        : base(options)
    {
    }

    public DbSet<TreasureHuntResult> TreasureHuntResults { get; set; }
}