using Microsoft.EntityFrameworkCore;

namespace TestAwing.Models;

public class TreasureHuntContext(DbContextOptions<TreasureHuntContext> options) : DbContext(options)
{
    public DbSet<TreasureHuntResult> TreasureHuntResults { get; set; }
}