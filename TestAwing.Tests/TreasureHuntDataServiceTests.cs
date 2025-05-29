using Microsoft.EntityFrameworkCore;
using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests.Services;

public class TreasureHuntDataServiceTests : IDisposable
{
    private readonly TreasureHuntContext _context;
    private readonly TreasureHuntDataService _dataService;

    public TreasureHuntDataServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<TreasureHuntContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TreasureHuntContext(options);
        _dataService = new TreasureHuntDataService(_context);
    }

    [Fact]
    public async Task GetPaginatedTreasureHunts_EmptyDatabase_ReturnsEmptyResult()
    {
        // Act
        var result = await _dataService.GetPaginatedTreasureHunts(1, 8);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(8, result.PageSize);
    }

    [Fact]
    public async Task GetPaginatedTreasureHunts_WithData_ReturnsCorrectPagination()
    {
        // Arrange
        var testData = CreateTestResults(15); // Create 15 test results
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetPaginatedTreasureHunts(1, 8);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.Data.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(8, result.PageSize);
        
        // Should be ordered by CreatedAt descending (newest first)
        var dates = result.Data.Select(x => x.CreatedAt).ToList();
        Assert.True(dates.SequenceEqual(dates.OrderByDescending(d => d)));
    }

    [Fact]
    public async Task GetPaginatedTreasureHunts_SecondPage_ReturnsCorrectData()
    {
        // Arrange
        var testData = CreateTestResults(15);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetPaginatedTreasureHunts(2, 8);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.Data.Count); // 15 total - 8 on first page = 7 on second page
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(8, result.PageSize);
    }

    [Theory]
    [InlineData(0, 8, 1, 8)] // Invalid page, should default to 1
    [InlineData(-1, 8, 1, 8)] // Invalid page, should default to 1
    [InlineData(1, 0, 1, 1)] // Invalid pageSize, should default to 1
    [InlineData(1, -5, 1, 1)] // Invalid pageSize, should default to 1
    [InlineData(1, 150, 1, 100)] // PageSize too large, should cap at 100
    public async Task GetPaginatedTreasureHunts_InvalidParameters_SanitizesInput(
        int inputPage, int inputPageSize, int expectedPage, int expectedPageSize)
    {
        // Arrange
        var testData = CreateTestResults(5);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetPaginatedTreasureHunts(inputPage, inputPageSize);

        // Assert
        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
    }

    [Fact]
    public void GenerateRandomTestData_ValidParameters_ReturnsValidMatrix()
    {
        // Arrange
        int n = 3, m = 4, p = 5;

        // Act
        var result = _dataService.GenerateRandomTestData(n, m, p);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(n, result.N);
        Assert.Equal(m, result.M);
        Assert.Equal(p, result.P);
        Assert.NotNull(result.Matrix);
        Assert.Equal(n, result.Matrix.Length);
        
        // Verify all rows have correct length
        foreach (var row in result.Matrix)
        {
            Assert.Equal(m, row.Length);
        }

        // Verify all numbers from 1 to p appear at least once
        var allNumbers = result.Matrix.SelectMany(row => row).ToList();
        for (int i = 1; i <= p; i++)
        {
            Assert.Contains(i, allNumbers);
        }

        // Verify all numbers are in valid range [1, p]
        Assert.True(allNumbers.All(num => num >= 1 && num <= p));
    }

    [Theory]
    [InlineData(0, 3, 3)]
    [InlineData(3, 0, 3)]
    [InlineData(3, 3, 0)]
    [InlineData(-1, 3, 3)]
    [InlineData(3, -1, 3)]
    [InlineData(3, 3, -1)]
    public void GenerateRandomTestData_InvalidParameters_ThrowsArgumentException(int n, int m, int p)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dataService.GenerateRandomTestData(n, m, p));
    }

    [Fact]
    public void GenerateRandomTestData_PGreaterThanMatrixSize_ThrowsArgumentException()
    {
        // Arrange
        int n = 2, m = 2, p = 5; // p > n*m

        // Act & Assert - The service throws ArgumentOutOfRangeException when accessing positions[chest-1]
        Assert.Throws<ArgumentOutOfRangeException>(() => _dataService.GenerateRandomTestData(n, m, p));
    }

    [Fact]
    public void GenerateRandomTestData_MinimumValidCase_Works()
    {
        // Arrange
        int n = 1, m = 1, p = 1;

        // Act
        var result = _dataService.GenerateRandomTestData(n, m, p);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.N);
        Assert.Equal(1, result.M);
        Assert.Equal(1, result.P);
        Assert.Single(result.Matrix);
        Assert.Single(result.Matrix[0]);
        Assert.Equal(1, result.Matrix[0][0]);
    }

    [Fact]
    public void GenerateRandomTestData_LargeMatrix_AllNumbersPresent()
    {
        // Arrange
        int n = 5, m = 6, p = 10; // Matrix size 30, need 10 different numbers

        // Act
        var result = _dataService.GenerateRandomTestData(n, m, p);

        // Assert
        Assert.NotNull(result);
        
        var allNumbers = result.Matrix.SelectMany(row => row).ToList();
        
        // Each number from 1 to p should appear at least once
        for (int i = 1; i <= p; i++)
        {
            Assert.Contains(i, allNumbers);
        }

        // Should have exactly n*m numbers total
        Assert.Equal(n * m, allNumbers.Count);
        
        // All numbers should be in range [1, p]
        Assert.True(allNumbers.All(num => num >= 1 && num <= p));
    }

    [Fact]
    public void GenerateRandomTestData_ExactFit_AllPositionsFilled()
    {
        // Arrange
        int n = 3, m = 3, p = 9; // Exact fit: matrix size = p

        // Act
        var result = _dataService.GenerateRandomTestData(n, m, p);

        // Assert
        Assert.NotNull(result);
        
        var allNumbers = result.Matrix.SelectMany(row => row).ToList();
        
        // Each number from 1 to p should appear exactly once
        for (int i = 1; i <= p; i++)
        {
            Assert.Single(allNumbers, num => num == i);
        }

        // Should have exactly 9 numbers
        Assert.Equal(9, allNumbers.Count);
    }

    [Fact]
    public void GenerateRandomTestData_MultipleCallsProduceDifferentResults()
    {
        // Arrange
        int n = 4, m = 4, p = 8;

        // Act
        var result1 = _dataService.GenerateRandomTestData(n, m, p);
        var result2 = _dataService.GenerateRandomTestData(n, m, p);

        // Assert
        // Results should be different (very unlikely to be identical with randomization)
        var matrix1 = result1.Matrix.SelectMany(row => row).ToArray();
        var matrix2 = result2.Matrix.SelectMany(row => row).ToArray();
        
        Assert.False(matrix1.SequenceEqual(matrix2));
    }

    private List<TreasureHuntResult> CreateTestResults(int count)
    {
        var results = new List<TreasureHuntResult>();
        var baseTime = DateTime.UtcNow.AddHours(-count);

        for (int i = 0; i < count; i++)
        {
            results.Add(new TreasureHuntResult
            {
                N = 3,
                M = 3,
                P = 3,
                MatrixJson = "[[1,2,3],[2,3,1],[3,1,2]]",
                PathJson = "[{\"chestNumber\":0,\"position\":{\"row\":1,\"col\":1}}]",
                MinFuel = 5.0 + i,
                CreatedAt = baseTime.AddMinutes(i * 10), // Spread out over time
                Status = SolveStatus.Completed
            });
        }

        return results;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
