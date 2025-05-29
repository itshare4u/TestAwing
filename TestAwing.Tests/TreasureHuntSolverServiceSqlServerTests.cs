using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TestAwing.Models;
using TestAwing.Services;
using System.Text.Json;
using TestAwing.Tests.Base;

namespace TestAwing.Tests.Services;

/// <summary>
/// Comprehensive test coverage for TreasureHuntSolverService edge cases and error scenarios using SQL Server
/// </summary>
public class TreasureHuntSolverServiceSqlServerTests : SqlServerTestBase
{
    private readonly TreasureHuntSolverService _solverService;
    private readonly Mock<ILogger<TreasureHuntSolverService>> _mockLogger;

    public TreasureHuntSolverServiceSqlServerTests() : base()
    {
        // Setup mock logger
        _mockLogger = new Mock<ILogger<TreasureHuntSolverService>>();
        
        // Create scope factory mock
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        
        mockServiceProvider.Setup(x => x.GetService(typeof(TreasureHuntContext)))
            .Returns(Context);
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
        
        Context.TreasureHuntResults.Add(dbResult);
        await Context.SaveChangesAsync();

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
        
        Context.TreasureHuntResults.Add(dbResult);
        await Context.SaveChangesAsync();

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
        
        Context.TreasureHuntResults.Add(dbResult);
        await Context.SaveChangesAsync();

        // Act
        var result = await _solverService.CancelSolveAsync(dbResult.Id);

        // Assert
        Assert.False(result); // Cannot cancel already completed solve
    }
}
