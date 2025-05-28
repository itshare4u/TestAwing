using Microsoft.EntityFrameworkCore;
using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests;

public class TreasureHuntServiceTests : IDisposable
{
    private readonly TreasureHuntContext _context;
    private readonly TreasureHuntService _service;

    public TreasureHuntServiceTests()
    {
        var options = new DbContextOptionsBuilder<TreasureHuntContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TreasureHuntContext(options);
        _service = new TreasureHuntService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SolveTreasureHunt_ValidInput_ReturnsCorrectResult()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new int[] { 3, 2, 2 },
                new int[] { 2, 2, 2 },
                new int[] { 2, 2, 1 }
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        Assert.True(result.MinFuel > 0);
        Assert.True(result.Id > 0);
        Assert.NotEmpty(result.Path);
        Assert.Equal(4, result.Path.Count); // Starting position + 3 chests
        
        // Verify path starts at (0,0)
        Assert.Equal(0, result.Path[0].ChestNumber);
        Assert.Equal(0, result.Path[0].Row);
        Assert.Equal(0, result.Path[0].Col);
        Assert.Equal(0, result.Path[0].FuelUsed);
        
        // Verify path visits chests 1, 2, 3 in order
        Assert.Equal(1, result.Path[1].ChestNumber);
        Assert.Equal(2, result.Path[2].ChestNumber);
        Assert.Equal(3, result.Path[3].ChestNumber);
    }

    [Fact]
    public async Task SolveTreasureHunt_InvalidMatrixDimensions_ThrowsException()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][]
            {
                new int[] { 1, 2, 3 }, // Wrong column count
                new int[] { 2, 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SolveTreasureHunt(request));
    }

    [Fact]
    public async Task SolveTreasureHunt_MissingChest_ThrowsException()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 3,
            Matrix = new int[][]
            {
                new int[] { 1, 2 },
                new int[] { 2, 1 }
                // Missing chest 3
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SolveTreasureHunt(request));
    }

    [Fact]
    public void GenerateRandomTestData_ValidParameters_ReturnsValidMatrix()
    {
        // Arrange
        int n = 3, m = 4, p = 5;

        // Act
        var result = _service.GenerateRandomTestData(n, m, p);

        // Assert
        Assert.Equal(n, result.N);
        Assert.Equal(m, result.M);
        Assert.Equal(p, result.P);
        Assert.Equal(n, result.Matrix.Length);
        Assert.All(result.Matrix, row => Assert.Equal(m, row.Length));
        
        // Verify each chest number from 1 to p appears at least once
        var allValues = result.Matrix.SelectMany(row => row).ToList();
        for (int chest = 1; chest <= p; chest++)
        {
            Assert.Contains(chest, allValues);
        }
        
        // Verify all values are in valid range
        Assert.All(allValues, value => Assert.InRange(value, 1, p));
    }

    [Fact]
    public void GenerateRandomTestData_InvalidParameters_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => _service.GenerateRandomTestData(0, 3, 3));
        Assert.Throws<ArgumentException>(() => _service.GenerateRandomTestData(3, 0, 3));
        Assert.Throws<ArgumentException>(() => _service.GenerateRandomTestData(3, 3, 0));
        Assert.Throws<ArgumentException>(() => _service.GenerateRandomTestData(-1, 3, 3));
    }

    [Fact]
    public async Task GetAllTreasureHunts_WithData_ReturnsOrderedResults()
    {
        // Arrange
        var request1 = new TreasureHuntRequest
        {
            N = 2, M = 2, P = 2,
            Matrix = new int[][] { new int[] { 1, 2 }, new int[] { 2, 1 } }
        };
        
        var request2 = new TreasureHuntRequest
        {
            N = 2, M = 2, P = 2,
            Matrix = new int[][] { new int[] { 2, 1 }, new int[] { 1, 2 } }
        };

        await _service.SolveTreasureHunt(request1);
        await Task.Delay(10); // Small delay to ensure different timestamps
        await _service.SolveTreasureHunt(request2);

        // Act
        var results = await _service.GetAllTreasureHunts();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.True(results[0].CreatedAt >= results[1].CreatedAt); // Ordered by CreatedAt descending
    }

    [Fact]
    public async Task GetTreasureHuntById_ExistingId_ReturnsResult()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2, M = 2, P = 2,
            Matrix = new int[][] { new int[] { 1, 2 }, new int[] { 2, 1 } }
        };
        
        var savedResult = await _service.SolveTreasureHunt(request);

        // Act
        var result = await _service.GetTreasureHuntById(savedResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(savedResult.Id, result.Id);
        Assert.Equal(request.N, result.N);
        Assert.Equal(request.M, result.M);
        Assert.Equal(request.P, result.P);
        Assert.NotNull(result.Matrix);
        Assert.NotNull(result.Path);
    }

    [Fact]
    public async Task GetTreasureHuntById_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetTreasureHuntById(99999);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(2, 2, 2)]
    [InlineData(3, 3, 3)]
    [InlineData(4, 5, 10)]
    [InlineData(1, 1, 1)]
    public void GenerateRandomTestData_VariousValidSizes_ProducesValidResults(int n, int m, int p)
    {
        // Act
        var result = _service.GenerateRandomTestData(n, m, p);

        // Assert
        Assert.Equal(n, result.N);
        Assert.Equal(m, result.M);
        Assert.Equal(p, result.P);
        
        var allValues = result.Matrix.SelectMany(row => row).ToList();
        
        // Each chest number from 1 to p should appear at least once
        for (int chest = 1; chest <= p; chest++)
        {
            Assert.Contains(chest, allValues);
        }
    }

    [Fact]
    public async Task SolveTreasureHunt_CalculatesCorrectFuelForKnownCase()
    {
        // Arrange - Test case from problem description
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 4,
            P = 3,
            Matrix = new int[][]
            {
                new int[] { 2, 1, 1, 1 },
                new int[] { 1, 1, 1, 1 },
                new int[] { 2, 1, 1, 3 }
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        // Expected path: (0,0) -> (0,1) for chest 1 -> (0,0) for chest 2 -> (2,3) for chest 3
        // Distance: 1 + 1 + sqrt(13) ≈ 5.606
        var expectedTotal = 1.0 + 1.0 + Math.Sqrt(13);
        Assert.Equal(expectedTotal, result.MinFuel, 1); // Allow small tolerance for floating point
    }

    [Fact]
    public async Task SolveTreasureHunt_SingleChest_ReturnsCorrectDistance()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 1,
            Matrix = new int[][]
            {
                new int[] { 1, 2 },
                new int[] { 2, 2 }
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        Assert.Equal(0.0, result.MinFuel); // Chest 1 is at starting position (0,0)
        Assert.Equal(2, result.Path.Count); // Start + 1 chest
    }

    [Fact]
    public async Task SolveTreasureHunt_MultipleChestsOfSameNumber_ChoosesClosest()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 2,
            Matrix = new int[][]
            {
                new int[] { 1, 2, 2 },  // Chest 1 at (0,0), Chest 2 at (0,1) - closer
                new int[] { 3, 3, 3 },
                new int[] { 2, 3, 3 }   // Chest 2 at (2,0) - farther
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        Assert.Equal(3, result.Path.Count); // Start + 2 chests
        
        // Should visit chest 1 at (0,0) first
        Assert.Equal(1, result.Path[1].ChestNumber);
        Assert.Equal(0, result.Path[1].Row);
        Assert.Equal(0, result.Path[1].Col);
        
        // Should visit chest 2 at (0,1) - the closer one
        Assert.Equal(2, result.Path[2].ChestNumber);
        Assert.Equal(0, result.Path[2].Row);
        Assert.Equal(1, result.Path[2].Col);
        
        // Total distance should be 0 + 1 = 1
        Assert.Equal(1.0, result.MinFuel);
    }

    [Fact]
    public void GenerateRandomTestData_LargePvalue_HandlesCorrectly()
    {
        // Arrange
        int n = 10, m = 10, p = 50;

        // Act
        var result = _service.GenerateRandomTestData(n, m, p);

        // Assert
        var allValues = result.Matrix.SelectMany(row => row).ToList();
        Assert.Equal(100, allValues.Count); // n * m total positions
        
        // Each chest number from 1 to p should appear at least once
        for (int chest = 1; chest <= p; chest++)
        {
            Assert.Contains(chest, allValues);
        }
        
        // All values should be in range [1, p]
        Assert.All(allValues, value => Assert.InRange(value, 1, p));
    }
}
