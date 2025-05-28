using Microsoft.EntityFrameworkCore;
using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests;

public class OptimizedTreasureHuntServiceTests : IDisposable
{
    private readonly TreasureHuntContext _context;
    private readonly OptimizedTreasureHuntService _service;

    public OptimizedTreasureHuntServiceTests()
    {
        var options = new DbContextOptionsBuilder<TreasureHuntContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TreasureHuntContext(options);
        _service = new OptimizedTreasureHuntService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task OptimizedService_LinearPath_CorrectDistances()
    {
        // Arrange - Same test case as the original LinearPath test
        var request = new TreasureHuntRequest
        {
            N = 1,
            M = 4,
            P = 3,
            Matrix = new int[][]
            {
                new int[] { 3, 1, 2, 4 } // Starting at (0,0), chest 1 at (0,1), chest 2 at (0,2), chest 3 at (0,0)
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        // Path: (0,0) -> (0,1) -> (0,2) -> (0,0)
        // Distances: 1 + 1 + 2 = 4
        Assert.Equal(4.0, result.MinFuel);
        
        Assert.Equal(4, result.Path.Count);
        
        // Start at (0,0)
        Assert.Equal(0, result.Path[0].ChestNumber);
        Assert.Equal(0, result.Path[0].Row);
        Assert.Equal(0, result.Path[0].Col);
        Assert.Equal(0.0, result.Path[0].FuelUsed);
        
        // Chest 1 at (0,1) - fuel used = 1 (distance from start)
        Assert.Equal(1, result.Path[1].ChestNumber);
        Assert.Equal(0, result.Path[1].Row);
        Assert.Equal(1, result.Path[1].Col);
        Assert.Equal(1.0, result.Path[1].FuelUsed);
        
        // Chest 2 at (0,2) - fuel used = 1 (distance from chest 1)
        Assert.Equal(2, result.Path[2].ChestNumber);
        Assert.Equal(0, result.Path[2].Row);
        Assert.Equal(2, result.Path[2].Col);
        Assert.Equal(1.0, result.Path[2].FuelUsed);
        
        // Chest 3 at (0,0) - fuel used = 2 (distance from chest 2)
        // This should show fuel=2, NOT fuel=0 even though it's at start position
        Assert.Equal(3, result.Path[3].ChestNumber);
        Assert.Equal(0, result.Path[3].Row);
        Assert.Equal(0, result.Path[3].Col);
        Assert.Equal(2.0, result.Path[3].FuelUsed);
    }
}
