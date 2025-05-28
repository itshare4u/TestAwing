using Microsoft.EntityFrameworkCore;
using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests;

public class TreasureHuntAlgorithmTests : IDisposable
{
    private readonly TreasureHuntContext _context;
    private readonly TreasureHuntService _service;

    public TreasureHuntAlgorithmTests()
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
    public async Task AlgorithmTest_ExampleCase1_ProducesExpectedResult()
    {
        // Arrange - Test case from problem description
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
        // Expected path: (0,0) -> (2,2) for chest 1 -> (0,1) for chest 2 -> (0,0) for chest 3
        // Distance: √8 + √5 + √1 = 2√2 + √5 + 1 ≈ 4.06
        var expectedFuel = Math.Sqrt(8) + Math.Sqrt(5) + 1;
        Assert.Equal(expectedFuel, result.MinFuel, 2); // 2 decimal places precision

        // Verify path
        Assert.Equal(4, result.Path.Count); // Start + 3 chests
        Assert.Equal(0, result.Path[0].ChestNumber); // Start
        Assert.Equal(1, result.Path[1].ChestNumber); // First chest
        Assert.Equal(2, result.Path[2].ChestNumber); // Second chest  
        Assert.Equal(3, result.Path[3].ChestNumber); // Third chest
    }

    [Fact]
    public async Task AlgorithmTest_ExampleCase2_ProducesExpectedResult()
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
        Assert.Equal(expectedTotal, result.MinFuel, 1);

        // Verify path visits correct positions
        Assert.Equal(4, result.Path.Count);
        
        // Start at (0,0)
        Assert.Equal(0, result.Path[0].Row);
        Assert.Equal(0, result.Path[0].Col);
        
        // Chest 1 at (0,1) - closest to start
        Assert.Equal(1, result.Path[1].ChestNumber);
        Assert.Equal(0, result.Path[1].Row);
        Assert.Equal(1, result.Path[1].Col);
        Assert.Equal(1.0, result.Path[1].FuelUsed);
        
        // Chest 2 at (0,0) - closest to current position
        Assert.Equal(2, result.Path[2].ChestNumber);
        Assert.Equal(0, result.Path[2].Row);
        Assert.Equal(0, result.Path[2].Col);
        Assert.Equal(1.0, result.Path[2].FuelUsed);
        
        // Chest 3 at (2,3) - only position
        Assert.Equal(3, result.Path[3].ChestNumber);
        Assert.Equal(2, result.Path[3].Row);
        Assert.Equal(3, result.Path[3].Col);
        Assert.Equal(Math.Sqrt(13), result.Path[3].FuelUsed, 1);
    }

    [Fact]
    public async Task AlgorithmTest_ExampleCase3_ProducesExpectedResult()
    {
        // Arrange - Test case from problem description  
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 4,
            P = 12,
            Matrix = new int[][]
            {
                new int[] { 1, 2, 3, 4 },
                new int[] { 8, 7, 6, 5 },
                new int[] { 9, 10, 11, 12 }
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        // Expected result: 11 (from problem description)
        Assert.Equal(11.0, result.MinFuel, 1);

        // Verify path has correct number of steps
        Assert.Equal(13, result.Path.Count); // Start + 12 chests
        
        // Verify sequential chest numbers
        for (int i = 1; i <= 12; i++)
        {
            Assert.Equal(i, result.Path[i].ChestNumber);
        }
    }

    [Fact]
    public async Task AlgorithmTest_SingleChestAtStart_ZeroFuel()
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
        Assert.Equal(0.0, result.MinFuel);
        Assert.Equal(2, result.Path.Count);
        
        // Both start and chest 1 at (0,0)
        Assert.Equal(0, result.Path[0].Row);
        Assert.Equal(0, result.Path[0].Col);
        Assert.Equal(0, result.Path[1].Row);
        Assert.Equal(0, result.Path[1].Col);
        Assert.Equal(0.0, result.Path[1].FuelUsed);
    }

    [Fact]
    public async Task AlgorithmTest_LinearPath_CorrectDistances()
    {
        // Arrange - Chests arranged in a line
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
        
        // Chest 1 at (0,1)
        Assert.Equal(0, result.Path[1].Row);
        Assert.Equal(1, result.Path[1].Col);
        Assert.Equal(1.0, result.Path[1].FuelUsed);
        
        // Chest 2 at (0,2)
        Assert.Equal(0, result.Path[2].Row);
        Assert.Equal(2, result.Path[2].Col);
        Assert.Equal(1.0, result.Path[2].FuelUsed);
        
        // Chest 3 at (0,0)
        Assert.Equal(0, result.Path[3].Row);
        Assert.Equal(0, result.Path[3].Col);
        Assert.Equal(2.0, result.Path[3].FuelUsed);
    }

    [Fact]
    public async Task AlgorithmTest_DiagonalMovement_CorrectDistance()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 1,
            Matrix = new int[][]
            {
                new int[] { 2, 2, 2 },
                new int[] { 2, 2, 2 },
                new int[] { 2, 2, 1 }
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        // Distance from (0,0) to (2,2) = √((2-0)² + (2-0)²) = √8 = 2√2
        var expectedDistance = Math.Sqrt(8);
        Assert.Equal(expectedDistance, result.MinFuel, 10);
        
        Assert.Equal(2, result.Path.Count);
        Assert.Equal(2, result.Path[1].Row);
        Assert.Equal(2, result.Path[1].Col);
        Assert.Equal(expectedDistance, result.Path[1].FuelUsed, 10);
    }

    [Fact]
    public async Task AlgorithmTest_MultipleInstancesOfSameChest_ChoosesClosest()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 2,
            Matrix = new int[][]
            {
                new int[] { 1, 2, 3 },  // Chest 1 at (0,0), Chest 2 at (0,1) - distance 1
                new int[] { 3, 3, 3 },
                new int[] { 2, 3, 3 }   // Chest 2 at (2,0) - distance √8
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        Assert.Equal(3, result.Path.Count);
        
        // Should choose chest 1 at (0,0) - distance 0
        Assert.Equal(0, result.Path[1].Row);
        Assert.Equal(0, result.Path[1].Col);
        Assert.Equal(0.0, result.Path[1].FuelUsed);
        
        // Should choose chest 2 at (0,1) - distance 1, not (2,0) - distance √8
        Assert.Equal(0, result.Path[2].Row);
        Assert.Equal(1, result.Path[2].Col);
        Assert.Equal(1.0, result.Path[2].FuelUsed);
        
        // Total fuel should be 1, not √8
        Assert.Equal(1.0, result.MinFuel);
    }

    [Fact]
    public async Task AlgorithmTest_LargeNumbers_HandlesCorrectly()
    {
        // Arrange - Create a matrix that actually contains all required chests
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 3,
            P = 5,  // P=5 matches the available chest numbers in matrix
            Matrix = new int[][]
            {
                new int[] { 5, 1, 2 },
                new int[] { 3, 4, 5 }
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        Assert.True(result.MinFuel > 0);
        Assert.Equal(6, result.Path.Count); // Start + 5 chests
        
        // Should visit chests in order 1, 2, 3, 4, 5
        for (int i = 1; i <= 5; i++)
        {
            Assert.Equal(i, result.Path[i].ChestNumber);
        }
    }

    [Fact]
    public async Task AlgorithmTest_FloatingPointPrecision_HandlesCorrectly()
    {
        // Arrange - Create a case that results in irrational distances
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 2,
            Matrix = new int[][]
            {
                new int[] { 3, 3, 3 },
                new int[] { 3, 1, 3 },  // Chest 1 at (1,1) - distance √2 from start
                new int[] { 3, 3, 2 }   // Chest 2 at (2,2) - distance √2 from chest 1
            }
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        var expectedDistance = Math.Sqrt(2) + Math.Sqrt(2); // 2√2
        Assert.Equal(expectedDistance, result.MinFuel, 10); // High precision check
        
        // Verify cumulative fuel calculations
        Assert.Equal(Math.Sqrt(2), result.Path[1].CumulativeFuel, 10);
        Assert.Equal(expectedDistance, result.Path[2].CumulativeFuel, 10);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 2, 2)]
    [InlineData(3, 3, 3)]
    [InlineData(5, 5, 5)]
    public async Task AlgorithmTest_SquareMatrices_ProducesValidResults(int size, int p, int expectedPathLength)
    {
        // Arrange - Create a square matrix
        var matrix = new int[size][];
        for (int i = 0; i < size; i++)
        {
            matrix[i] = new int[size];
            for (int j = 0; j < size; j++)
            {
                matrix[i][j] = ((i * size + j) % p) + 1;
            }
        }

        var request = new TreasureHuntRequest
        {
            N = size,
            M = size,
            P = p,
            Matrix = matrix
        };

        // Act
        var result = await _service.SolveTreasureHunt(request);

        // Assert
        Assert.True(result.MinFuel >= 0);
        Assert.Equal(expectedPathLength + 1, result.Path.Count); // +1 for start position
        Assert.True(result.Id > 0); // Should be saved to database
    }
}
