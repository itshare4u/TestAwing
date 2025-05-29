using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TestAwing.Models;
using TestAwing.Services;
using System.Text.Json;

namespace TestAwing.Tests.Services;

/// <summary>
/// Comprehensive test coverage for TreasureHuntSolverService edge cases and error scenarios
/// </summary>
public class TreasureHuntSolverServiceEdgeCasesTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TreasureHuntContext _context;
    private readonly TreasureHuntSolverService _solverService;
    private readonly Mock<ILogger<TreasureHuntSolverService>> _mockLogger;

    public TreasureHuntSolverServiceEdgeCasesTests()
    {
        // Setup in-memory database
        var services = new ServiceCollection();
        services.AddDbContext<TreasureHuntContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<TreasureHuntContext>();
        
        // Setup mock logger
        _mockLogger = new Mock<ILogger<TreasureHuntSolverService>>();
        
        // Create scope factory mock
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        
        mockServiceProvider.Setup(x => x.GetService(typeof(TreasureHuntContext)))
            .Returns(_context);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
        
        _solverService = new TreasureHuntSolverService(mockScopeFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task StartSolveAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _solverService.StartSolveAsync(null!));
    }

    [Theory]
    [InlineData(0, 3, 3)] // Invalid N
    [InlineData(3, 0, 3)] // Invalid M
    [InlineData(3, 3, 0)] // Invalid P
    [InlineData(-1, 3, 3)] // Negative N
    [InlineData(3, -1, 3)] // Negative M
    [InlineData(3, 3, -1)] // Negative P
    public async Task StartSolveAsync_InvalidDimensions_ThrowsArgumentException(int n, int m, int p)
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = n,
            M = m,
            P = p,
            Matrix = new int[][] { new[] { 1 } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _solverService.StartSolveAsync(request));
    }

    [Fact]
    public async Task StartSolveAsync_NullMatrix_ThrowsArgumentException()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = null!
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _solverService.StartSolveAsync(request));
    }

    [Fact]
    public async Task StartSolveAsync_InconsistentMatrixDimensions_ThrowsArgumentException()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new[] { 1, 2, 3 },
                new[] { 2, 3 }, // Wrong column count
                new[] { 3, 1, 2 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _solverService.StartSolveAsync(request));
    }

    [Fact]
    public async Task StartSolveAsync_MissingChestNumbers_ThrowsArgumentException()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new[] { 1, 2, 2 },
                new[] { 2, 2, 2 },
                new[] { 2, 2, 1 }
                // Missing chest number 3
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _solverService.StartSolveAsync(request));
    }

    [Fact]
    public async Task StartSolveAsync_OutOfRangeChestNumbers_ThrowsArgumentException()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new[] { 1, 2, 3 },
                new[] { 2, 3, 1 },
                new[] { 3, 1, 5 } // 5 is greater than P=3
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _solverService.StartSolveAsync(request));
    }

    [Fact]
    public async Task StartSolveAsync_MinimumValidSize_ExecutesSuccessfully()
    {
        // Arrange - smallest possible valid matrix
        var request = new TreasureHuntRequest
        {
            N = 1,
            M = 1,
            P = 1,
            Matrix = new int[][]
            {
                new[] { 1 }
            }
        };

        // Act
        var result = await _solverService.StartSolveAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SolveId > 0);
        Assert.Equal("Solve operation started successfully", result.Message);
    }

    [Fact]
    public async Task StartSolveAsync_LargeMatrix_HandlesEfficiently()
    {
        // Arrange - larger matrix to test performance characteristics
        var n = 10;
        var m = 10;
        var p = 20;
        var matrix = new int[n][];
        
        // Generate a valid matrix
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < n; i++)
        {
            matrix[i] = new int[m];
            for (int j = 0; j < m; j++)
            {
                matrix[i][j] = random.Next(1, p + 1);
            }
        }

        // Ensure all chest numbers 1 to p are present
        var positions = new List<(int row, int col)>();
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                positions.Add((i, j));
            }
        }
        
        for (int chest = 1; chest <= p; chest++)
        {
            var pos = positions[chest - 1];
            matrix[pos.row][pos.col] = chest;
        }

        var request = new TreasureHuntRequest
        {
            N = n,
            M = m,
            P = p,
            Matrix = matrix
        };

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _solverService.StartSolveAsync(request);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SolveId > 0);
        Assert.True(duration.TotalSeconds < 5); // Should start quickly even for large matrices
    }

    [Fact]
    public async Task GetSolveStatusAsync_CompletedSolve_ReturnsFullResults()
    {
        // Arrange
        var pathSteps = new List<PathStep>
        {
            new() { ChestNumber = 0, Row = 1, Col = 1 },
            new() { ChestNumber = 1, Row = 2, Col = 2 },
            new() { ChestNumber = 2, Row = 3, Col = 3 }
        };

        var dbResult = new TreasureHuntResult
        {
            N = 3,
            M = 3,
            P = 2,
            MatrixJson = JsonSerializer.Serialize(new int[][] 
            { 
                new[] { 1, 2, 2 }, 
                new[] { 2, 1, 2 }, 
                new[] { 2, 2, 2 } 
            }),
            PathJson = JsonSerializer.Serialize(pathSteps),
            MinFuel = 2.828,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            StartedAt = DateTime.UtcNow.AddMinutes(-4),
            CompletedAt = DateTime.UtcNow.AddMinutes(-3),
            Status = SolveStatus.Completed
        };
        
        _context.TreasureHuntResults.Add(dbResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _solverService.GetSolveStatusAsync(dbResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dbResult.Id, result.SolveId);
        Assert.Equal(SolveStatus.Completed, result.Status);
        Assert.NotNull(result.Result);
        Assert.Equal(2.828, result.Result.MinFuel, precision: 3);
        Assert.Equal(dbResult.Id, result.Result.Id);
        Assert.NotNull(result.Result.Path);
        Assert.Equal(3, result.Result.Path.Count);
        Assert.Equal(0, result.Result.Path[0].ChestNumber);
        Assert.Equal(2, result.Result.Path[2].ChestNumber);
    }

    [Fact]
    public async Task GetSolveStatusAsync_FailedSolve_ReturnsErrorDetails()
    {
        // Arrange
        var dbResult = new TreasureHuntResult
        {
            N = 3,
            M = 3,
            P = 3,
            MatrixJson = "[[1,2,3],[2,3,1],[3,1,2]]",
            PathJson = "[]",
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            StartedAt = DateTime.UtcNow.AddMinutes(-4),
            CompletedAt = DateTime.UtcNow.AddMinutes(-3),
            Status = SolveStatus.Failed,
            ErrorMessage = "Test error message"
        };
        
        _context.TreasureHuntResults.Add(dbResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _solverService.GetSolveStatusAsync(dbResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dbResult.Id, result.SolveId);
        Assert.Equal(SolveStatus.Failed, result.Status);
        Assert.Equal("Test error message", result.ErrorMessage);
        Assert.Null(result.Result); // No result for failed solve
    }

    [Fact]
    public async Task CancelSolveAsync_NonExistentSolveId_ReturnsFalse()
    {
        // Arrange
        int nonExistentId = 99999;

        // Act
        var result = await _solverService.CancelSolveAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CancelSolveAsync_AlreadyCompletedSolve_ReturnsFalse()
    {
        // Arrange
        var dbResult = new TreasureHuntResult
        {
            N = 2,
            M = 2,
            P = 2,
            MatrixJson = "[[1,2],[2,1]]",
            PathJson = "[]",
            MinFuel = 1.0,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            StartedAt = DateTime.UtcNow.AddMinutes(-4),
            CompletedAt = DateTime.UtcNow.AddMinutes(-3),
            Status = SolveStatus.Completed
        };
        
        _context.TreasureHuntResults.Add(dbResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _solverService.CancelSolveAsync(dbResult.Id);

        // Assert
        Assert.False(result); // Cannot cancel already completed solve
    }

    [Fact]
    public async Task CancelSolveAsync_AlreadyCancelledSolve_ReturnsFalse()
    {
        // Arrange
        var dbResult = new TreasureHuntResult
        {
            N = 2,
            M = 2,
            P = 2,
            MatrixJson = "[[1,2],[2,1]]",
            PathJson = "[]",
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            StartedAt = DateTime.UtcNow.AddMinutes(-4),
            CompletedAt = DateTime.UtcNow.AddMinutes(-3),
            Status = SolveStatus.Cancelled
        };
        
        _context.TreasureHuntResults.Add(dbResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _solverService.CancelSolveAsync(dbResult.Id);

        // Assert
        Assert.False(result); // Cannot cancel already cancelled solve
    }

    [Fact]
    public async Task StartSolveAsync_ConcurrentRequests_HandlesMultipleSolves()
    {
        // Arrange
        var request1 = new TreasureHuntRequest
        {
            N = 2, M = 2, P = 2,
            Matrix = new int[][] { new[] { 1, 2 }, new[] { 2, 1 } }
        };
        
        var request2 = new TreasureHuntRequest
        {
            N = 3, M = 3, P = 3,
            Matrix = new int[][] 
            { 
                new[] { 1, 2, 3 }, 
                new[] { 2, 3, 1 }, 
                new[] { 3, 1, 2 } 
            }
        };

        // Act - start multiple solves concurrently
        var task1 = _solverService.StartSolveAsync(request1);
        var task2 = _solverService.StartSolveAsync(request2);
        
        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.NotNull(results[0]);
        Assert.NotNull(results[1]);
        Assert.NotEqual(results[0].SolveId, results[1].SolveId);
        Assert.True(results[0].SolveId > 0);
        Assert.True(results[1].SolveId > 0);
    }

    [Theory]
    [InlineData(SolveStatus.Pending)]
    [InlineData(SolveStatus.InProgress)]
    [InlineData(SolveStatus.Completed)]
    [InlineData(SolveStatus.Failed)]
    [InlineData(SolveStatus.Cancelled)]
    public async Task GetSolveStatusAsync_AllStatusTypes_ReturnsCorrectStatus(SolveStatus status)
    {
        // Arrange
        var dbResult = new TreasureHuntResult
        {
            N = 2,
            M = 2,
            P = 2,
            MatrixJson = "[[1,2],[2,1]]",
            PathJson = "[]",
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow,
            Status = status,
            ErrorMessage = status == SolveStatus.Failed ? "Test error" : null
        };
        
        _context.TreasureHuntResults.Add(dbResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _solverService.GetSolveStatusAsync(dbResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(status, result.Status);
        
        if (status == SolveStatus.Failed)
        {
            Assert.Equal("Test error", result.ErrorMessage);
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
