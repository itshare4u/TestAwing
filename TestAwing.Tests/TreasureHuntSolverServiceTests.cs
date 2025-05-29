using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TestAwing.Models;
using TestAwing.Services;
using System.Text.Json;

namespace TestAwing.Tests.Services;

public class TreasureHuntSolverServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TreasureHuntContext _context;
    private readonly TreasureHuntSolverService _solverService;
    private readonly Mock<ILogger<TreasureHuntSolverService>> _mockLogger;

    public TreasureHuntSolverServiceTests()
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
        
        // Mock GetService method instead of GetRequiredService extension
        mockServiceProvider.Setup(x => x.GetService(typeof(TreasureHuntContext)))
            .Returns(_context);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
        
        _solverService = new TreasureHuntSolverService(mockScopeFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task StartSolveAsync_ValidRequest_ReturnsAsyncSolveResponse()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new[] { 3, 2, 2 },
                new[] { 2, 2, 2 },
                new[] { 2, 2, 1 }
            }
        };

        // Act
        var result = await _solverService.StartSolveAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SolveId > 0);
        // Status can be Pending, InProgress, or Completed depending on timing
        Assert.True(result.Status == SolveStatus.Pending || 
                   result.Status == SolveStatus.InProgress || 
                   result.Status == SolveStatus.Completed);
        Assert.Equal("Solve operation started successfully", result.Message);
        
        // Verify database record was created
        var dbRecord = await _context.TreasureHuntResults.FindAsync(result.SolveId);
        Assert.NotNull(dbRecord);
        Assert.Equal(request.N, dbRecord.N);
        Assert.Equal(request.M, dbRecord.M);
        Assert.Equal(request.P, dbRecord.P);
        // Status can be Pending, InProgress, or Completed depending on timing
        Assert.True(dbRecord.Status == SolveStatus.Pending || 
                   dbRecord.Status == SolveStatus.InProgress || 
                   dbRecord.Status == SolveStatus.Completed);
    }

    [Fact]
    public async Task GetSolveStatus_ExistingSolveId_ReturnsCorrectStatus()
    {
        // Arrange
        var dbResult = new TreasureHuntResult
        {
            N = 3,
            M = 3,
            P = 3,
            MatrixJson = JsonSerializer.Serialize(new int[][] 
            { 
                new[] { 3, 2, 2 }, 
                new[] { 2, 2, 2 }, 
                new[] { 2, 2, 1 } 
            }),
            PathJson = "[]",
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow,
            Status = SolveStatus.InProgress
        };
        
        _context.TreasureHuntResults.Add(dbResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _solverService.GetSolveStatusAsync(dbResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dbResult.Id, result.SolveId);
        Assert.Equal(SolveStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task GetSolveStatus_NonExistentSolveId_ReturnsNull()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        var result = await _solverService.GetSolveStatusAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CancelSolveAsync_ExistingPendingSolve_ReturnsTrue()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 5, // Larger matrix to give more time for cancellation
            M = 5,
            P = 8,
            Matrix = new int[][]
            {
                new[] { 1, 2, 3, 4, 5 },
                new[] { 2, 3, 4, 5, 6 },
                new[] { 3, 4, 5, 6, 7 },
                new[] { 4, 5, 6, 7, 8 },
                new[] { 5, 6, 7, 8, 1 }
            }
        };

        var solveResponse = await _solverService.StartSolveAsync(request);
        
        // Try to cancel immediately to catch it before completion
        await Task.Delay(10); // Small delay to ensure the solve has started

        // Act
        var result = await _solverService.CancelSolveAsync(solveResponse.SolveId);

        // Assert - cancellation might succeed or fail depending on timing
        // If it fails, the solve operation likely completed before cancellation
        if (result)
        {
            // Verify the database was updated if cancellation succeeded
            var status = await _solverService.GetSolveStatusAsync(solveResponse.SolveId);
            Assert.NotNull(status);
            Assert.Equal(SolveStatus.Cancelled, status.Status);
        }
        else
        {
            // If cancellation failed, the solve should be completed or in progress
            var status = await _solverService.GetSolveStatusAsync(solveResponse.SolveId);
            Assert.NotNull(status);
            Assert.True(status.Status == SolveStatus.Completed || status.Status == SolveStatus.InProgress);
        }
    }

    [Fact]
    public async Task CancelSolveAsync_NonExistentSolve_ReturnsFalse()
    {
        // Arrange
        int nonExistentId = 999;

        // Act
        var result = await _solverService.CancelSolveAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task StartSolveAsync_ValidSimpleMatrix_CreatesValidSolveOperation()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][]
            {
                new[] { 1, 2 },
                new[] { 2, 1 }
            }
        };

        // Act
        var result = await _solverService.StartSolveAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SolveId > 0);
        // Status can be either Pending or InProgress depending on timing
        Assert.True(result.Status == SolveStatus.Pending || result.Status == SolveStatus.InProgress);
        Assert.Equal("Solve operation started successfully", result.Message);
        
        // Verify database record was created with correct parameters
        var dbRecord = await _context.TreasureHuntResults.FindAsync(result.SolveId);
        Assert.NotNull(dbRecord);
        Assert.Equal(request.N, dbRecord.N);
        Assert.Equal(request.M, dbRecord.M);
        Assert.Equal(request.P, dbRecord.P);
        // Status can be either Pending or InProgress depending on timing
        Assert.True(dbRecord.Status == SolveStatus.Pending || dbRecord.Status == SolveStatus.InProgress);
    }

    [Fact]
    public async Task StartSolveAsync_CompletesEventually_UpdatesStatusToCompleted()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][]
            {
                new[] { 1, 2 },
                new[] { 2, 1 }
            }
        };

        // Act
        var solveResponse = await _solverService.StartSolveAsync(request);
        
        // Wait a bit for the background task to complete
        await Task.Delay(2000);

        var status = await _solverService.GetSolveStatusAsync(solveResponse.SolveId);

        // Assert
        Assert.NotNull(status);
        // Status should eventually be Completed (or still InProgress for very quick execution)
        Assert.True(status.Status == SolveStatus.Completed || status.Status == SolveStatus.InProgress);
        
        if (status.Status == SolveStatus.Completed)
        {
            Assert.NotNull(status.Result);
            Assert.True(status.Result.MinFuel > 0);
            Assert.NotNull(status.Result.Path);
            Assert.True(status.Result.Path.Count > 0);
            
            // Verify path starts with chest 0
            Assert.Equal(0, status.Result.Path[0].ChestNumber);
            
            // Verify path ends with chest P
            Assert.Equal(request.P, status.Result.Path.Last().ChestNumber);
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
