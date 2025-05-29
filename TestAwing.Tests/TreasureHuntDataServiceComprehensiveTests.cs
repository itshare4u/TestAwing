using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests.Services;

public class TreasureHuntDataServiceComprehensiveTests : IDisposable
{
    private readonly TreasureHuntContext _context;
    private readonly TreasureHuntDataService _dataService;

    public TreasureHuntDataServiceComprehensiveTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<TreasureHuntContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TreasureHuntContext(options);
        _dataService = new TreasureHuntDataService(_context);
    }

    #region GetTreasureHuntById Tests

    [Fact]
    public async Task GetTreasureHuntById_ValidId_ReturnsCorrectResult()
    {
        // Arrange
        var matrix = new int[][] { new int[] { 1, 2 }, new int[] { 3, 1 } };
        var path = new List<PathStep>
        {
            new() { ChestNumber = 1, Position = new Position { Row = 0, Col = 0 } },
            new() { ChestNumber = 2, Position = new Position { Row = 0, Col = 1 } }
        };

        var testResult = new TreasureHuntResult
        {
            N = 2,
            M = 2,
            P = 3,
            MatrixJson = JsonSerializer.Serialize(matrix),
            PathJson = JsonSerializer.Serialize(path),
            MinFuel = 1.5,
            CreatedAt = DateTime.UtcNow,
            Status = SolveStatus.Completed
        };

        _context.TreasureHuntResults.Add(testResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetTreasureHuntById(testResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testResult.Id, result.Id);
        Assert.Equal(2, result.N);
        Assert.Equal(2, result.M);
        Assert.Equal(3, result.P);
        Assert.Equal(1.5, result.MinFuel);
        Assert.Equal(2, result.Matrix.Length);
        Assert.Equal(2, result.Matrix[0].Length);
        Assert.Equal(1, result.Matrix[0][0]);
        Assert.Equal(2, result.Matrix[0][1]);
        Assert.Equal(2, result.Path.Count);
        Assert.Equal(1, result.Path[0].ChestNumber);
        Assert.Equal(0, result.Path[0].Position.Row);
        Assert.Equal(0, result.Path[0].Position.Col);
    }

    [Fact]
    public async Task GetTreasureHuntById_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _dataService.GetTreasureHuntById(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTreasureHuntById_NegativeId_ReturnsNull()
    {
        // Act
        var result = await _dataService.GetTreasureHuntById(-1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTreasureHuntById_ZeroId_ReturnsNull()
    {
        // Act
        var result = await _dataService.GetTreasureHuntById(0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTreasureHuntById_EmptyPathJson_ReturnsEmptyPath()
    {
        // Arrange
        var matrix = new int[][] { new int[] { 1 } };
        var testResult = new TreasureHuntResult
        {
            N = 1,
            M = 1,
            P = 1,
            MatrixJson = JsonSerializer.Serialize(matrix),
            PathJson = "", // Empty path
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow,
            Status = SolveStatus.Completed
        };

        _context.TreasureHuntResults.Add(testResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetTreasureHuntById(testResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Path);
    }

    [Fact]
    public async Task GetTreasureHuntById_NullPathJson_ReturnsEmptyPath()
    {
        // Arrange
        var matrix = new int[][] { new int[] { 1 } };
        var testResult = new TreasureHuntResult
        {
            N = 1,
            M = 1,
            P = 1,
            MatrixJson = JsonSerializer.Serialize(matrix),
            PathJson = null, // Null path
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow,
            Status = SolveStatus.Completed
        };

        _context.TreasureHuntResults.Add(testResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetTreasureHuntById(testResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Path);
    }

    [Fact]
    public async Task GetTreasureHuntById_InvalidMatrixJson_HandlesGracefully()
    {
        // Arrange
        var testResult = new TreasureHuntResult
        {
            N = 1,
            M = 1,
            P = 1,
            MatrixJson = "invalid json", // Invalid JSON
            PathJson = "[]",
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow,
            Status = SolveStatus.Completed
        };

        _context.TreasureHuntResults.Add(testResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetTreasureHuntById(testResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Matrix); // Should default to empty array
    }

    [Fact]
    public async Task GetTreasureHuntById_InvalidPathJson_HandlesGracefully()
    {
        // Arrange
        var matrix = new int[][] { new int[] { 1 } };
        var testResult = new TreasureHuntResult
        {
            N = 1,
            M = 1,
            P = 1,
            MatrixJson = JsonSerializer.Serialize(matrix),
            PathJson = "invalid json", // Invalid JSON
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow,
            Status = SolveStatus.Completed
        };

        _context.TreasureHuntResults.Add(testResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetTreasureHuntById(testResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Path); // Should default to empty list
    }

    #endregion

    #region Large Dataset Performance Tests

    [Fact]
    public async Task GetPaginatedTreasureHunts_LargeDataset_PerformsWell()
    {
        // Arrange
        const int largeDatasetSize = 1000;
        var testData = CreateTestResults(largeDatasetSize);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _dataService.GetPaginatedTreasureHunts(1, 20);

        // Assert
        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should complete within 1 second
        Assert.Equal(20, result.Data.Count);
        Assert.Equal(largeDatasetSize, result.TotalCount);
    }

    [Fact]
    public async Task GetPaginatedTreasureHunts_MiddlePages_PerformsConsistently()
    {
        // Arrange
        const int datasetSize = 500;
        var testData = CreateTestResults(datasetSize);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        var times = new List<long>();

        // Act - Test different pages
        for (int page = 1; page <= 5; page++)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _dataService.GetPaginatedTreasureHunts(page, 50);
            stopwatch.Stop();
            times.Add(stopwatch.ElapsedMilliseconds);

            Assert.Equal(50, result.Data.Count);
            Assert.Equal(datasetSize, result.TotalCount);
        }

        // Assert - Performance should be consistent across pages
        var maxTime = times.Max();
        var minTime = times.Min();
        Assert.True(maxTime - minTime < 100); // Variance should be minimal
    }

    #endregion

    #region Boundary and Edge Case Tests

    [Fact]
    public async Task GetPaginatedTreasureHunts_ExactPageBoundary_HandlesCorrectly()
    {
        // Arrange
        const int totalItems = 40;
        const int pageSize = 10;
        var testData = CreateTestResults(totalItems);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act - Request exact last page
        var result = await _dataService.GetPaginatedTreasureHunts(4, pageSize);

        // Assert
        Assert.Equal(pageSize, result.Data.Count);
        Assert.Equal(totalItems, result.TotalCount);
        Assert.Equal(4, result.Page);
        Assert.Equal(pageSize, result.PageSize);
    }

    [Fact]
    public async Task GetPaginatedTreasureHunts_BeyondLastPage_ReturnsEmptyResults()
    {
        // Arrange
        var testData = CreateTestResults(5);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetPaginatedTreasureHunts(10, 8); // Page way beyond data

        // Assert
        Assert.Empty(result.Data);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(10, result.Page);
        Assert.Equal(8, result.PageSize);
    }

    [Theory]
    [InlineData(int.MaxValue, 8)]
    [InlineData(1, int.MaxValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public async Task GetPaginatedTreasureHunts_ExtremeValues_HandlesGracefully(int page, int pageSize)
    {
        // Arrange
        var testData = CreateTestResults(3);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act & Assert - Should not throw
        var result = await _dataService.GetPaginatedTreasureHunts(page, pageSize);
        Assert.NotNull(result);
        Assert.True(result.PageSize <= 100); // Should be capped
        Assert.True(result.Page >= 1); // Should be at least 1
    }

    #endregion

    #region Random Test Data Generation Edge Cases

    [Theory]
    [InlineData(1, 1000, 1)] // Very wide matrix
    [InlineData(1000, 1, 1)] // Very tall matrix
    [InlineData(100, 100, 1)] // Large matrix, single chest type
    public void GenerateRandomTestData_ExtremeDimensions_WorksCorrectly(int n, int m, int p)
    {
        // Act
        var result = _dataService.GenerateRandomTestData(n, m, p);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(n, result.N);
        Assert.Equal(m, result.M);
        Assert.Equal(p, result.P);
        Assert.Equal(n, result.Matrix.Length);

        // Verify matrix structure
        foreach (var row in result.Matrix)
        {
            Assert.Equal(m, row.Length);
        }

        // Verify all numbers are in valid range
        var allNumbers = result.Matrix.SelectMany(row => row).ToList();
        Assert.True(allNumbers.All(num => num >= 1 && num <= p));

        // Verify each required chest number appears
        for (int i = 1; i <= p; i++)
        {
            Assert.Contains(i, allNumbers);
        }
    }

    [Fact]
    public void GenerateRandomTestData_PEqualsMatrixSize_AllNumbersUniqueAndPresent()
    {
        // Arrange
        int n = 3, m = 3, p = 9; // Exact fit

        // Act
        var result = _dataService.GenerateRandomTestData(n, m, p);

        // Assert
        var allNumbers = result.Matrix.SelectMany(row => row).ToList();
        
        // Each number from 1 to 9 should appear exactly once
        for (int i = 1; i <= 9; i++)
        {
            Assert.Single(allNumbers, num => num == i);
        }
        
        Assert.Equal(9, allNumbers.Count);
        Assert.Equal(9, allNumbers.Distinct().Count());
    }

    [Theory]
    [InlineData(10, 10, 50)] // p is half of matrix size
    [InlineData(5, 4, 10)] // p is half of matrix size
    public void GenerateRandomTestData_PIsHalfMatrixSize_CorrectDistribution(int n, int m, int p)
    {
        // Act
        var result = _dataService.GenerateRandomTestData(n, m, p);

        // Assert
        var allNumbers = result.Matrix.SelectMany(row => row).ToList();
        
        // Each number from 1 to p should appear at least once
        for (int i = 1; i <= p; i++)
        {
            Assert.Contains(i, allNumbers);
        }

        // Total count should be n*m
        Assert.Equal(n * m, allNumbers.Count);
        
        // The highest number (p) should appear exactly once
        Assert.Single(allNumbers, num => num == p);
    }

    [Fact]
    public void GenerateRandomTestData_RandomnessVerification_ProducesVariation()
    {
        // Arrange
        const int iterations = 10;
        const int n = 4, m = 4, p = 8;
        var results = new List<int[]>();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var result = _dataService.GenerateRandomTestData(n, m, p);
            results.Add(result.Matrix.SelectMany(row => row).ToArray());
        }

        // Assert - At least some results should be different
        var uniqueResults = results.Select(r => string.Join(",", r)).Distinct().Count();
        Assert.True(uniqueResults > 1, "Generated data should show variation across multiple calls");
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task GetPaginatedTreasureHunts_ConcurrentReads_HandlesSafely()
    {
        // Arrange
        var testData = CreateTestResults(100);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act - Execute multiple concurrent reads
        var tasks = new List<Task<PaginatedResponse<TreasureHuntResult>>>();
        for (int i = 1; i <= 10; i++)
        {
            int page = i;
            tasks.Add(Task.Run(() => _dataService.GetPaginatedTreasureHunts(page, 10)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.Equal(100, result.TotalCount);
            Assert.True(result.Data.Count <= 10);
        }
    }

    [Fact]
    public async Task GetTreasureHuntById_ConcurrentReads_HandlesSafely()
    {
        // Arrange
        var matrix = new int[][] { new int[] { 1 } };
        var testResult = new TreasureHuntResult
        {
            N = 1,
            M = 1,
            P = 1,
            MatrixJson = JsonSerializer.Serialize(matrix),
            PathJson = "[]",
            MinFuel = 0,
            CreatedAt = DateTime.UtcNow,
            Status = SolveStatus.Completed
        };

        _context.TreasureHuntResults.Add(testResult);
        await _context.SaveChangesAsync();

        // Act - Execute multiple concurrent reads of the same item
        var tasks = new List<Task<TreasureHuntResultWithPath?>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _dataService.GetTreasureHuntById(testResult.Id)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.Equal(testResult.Id, result.Id);
        }
    }

    #endregion

    #region Memory and Resource Management Tests

    [Fact]
    public async Task GetPaginatedTreasureHunts_LargePages_DoesNotExceedMemoryLimits()
    {
        // Arrange
        var testData = CreateTestResults(1000);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        var initialMemory = GC.GetTotalMemory(false);

        // Act
        var result = await _dataService.GetPaginatedTreasureHunts(1, 100); // Max allowed page size

        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        Assert.Equal(100, result.Data.Count);
        
        // Memory growth should be reasonable (less than 10MB for this test)
        var memoryGrowth = finalMemory - initialMemory;
        Assert.True(memoryGrowth < 10 * 1024 * 1024, $"Memory growth {memoryGrowth} bytes is too high");
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task GetPaginatedTreasureHunts_OrderingConsistency_MaintainsOrder()
    {
        // Arrange
        var testData = CreateTestResults(50);
        _context.TreasureHuntResults.AddRange(testData);
        await _context.SaveChangesAsync();

        // Act - Get multiple pages
        var page1 = await _dataService.GetPaginatedTreasureHunts(1, 20);
        var page2 = await _dataService.GetPaginatedTreasureHunts(2, 20);
        var page3 = await _dataService.GetPaginatedTreasureHunts(3, 20);

        // Assert - Ordering should be consistent (newest first)
        var allDates = page1.Data.Concat(page2.Data).Concat(page3.Data)
            .Select(x => x.CreatedAt).ToList();

        Assert.True(allDates.SequenceEqual(allDates.OrderByDescending(d => d)),
            "Data should be consistently ordered by CreatedAt descending across pages");
    }

    [Fact]
    public async Task GetTreasureHuntById_LargeMatrixSerialization_PreservesData()
    {
        // Arrange
        var largeMatrix = new int[50][];
        for (int i = 0; i < 50; i++)
        {
            largeMatrix[i] = new int[50];
            for (int j = 0; j < 50; j++)
            {
                largeMatrix[i][j] = (i * 50 + j) % 100 + 1; // Some pattern
            }
        }

        var testResult = new TreasureHuntResult
        {
            N = 50,
            M = 50,
            P = 100,
            MatrixJson = JsonSerializer.Serialize(largeMatrix),
            PathJson = "[]",
            MinFuel = 1000,
            CreatedAt = DateTime.UtcNow,
            Status = SolveStatus.Completed
        };

        _context.TreasureHuntResults.Add(testResult);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataService.GetTreasureHuntById(testResult.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Matrix.Length);
        Assert.Equal(50, result.Matrix[0].Length);
        
        // Verify data integrity
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 50; j++)
            {
                Assert.Equal(largeMatrix[i][j], result.Matrix[i][j]);
            }
        }
    }

    #endregion

    private List<TreasureHuntResult> CreateTestResults(int count)
    {
        var results = new List<TreasureHuntResult>();
        var baseTime = DateTime.UtcNow.AddHours(-count);

        for (int i = 0; i < count; i++)
        {
            results.Add(new TreasureHuntResult
            {
                N = 3 + (i % 5), // Vary dimensions
                M = 3 + (i % 4),
                P = 3 + (i % 6),
                MatrixJson = $"[[{1 + (i % 3)},{2 + (i % 3)},{3 + (i % 3)}],[{2 + (i % 3)},{3 + (i % 3)},{1 + (i % 3)}],[{3 + (i % 3)},{1 + (i % 3)},{2 + (i % 3)}]]",
                PathJson = $"[{{\"chestNumber\":{i % 3},\"position\":{{\"row\":{i % 3},\"col\":{i % 3}}}}}]",
                MinFuel = 5.0 + i * 0.1,
                CreatedAt = baseTime.AddMinutes(i * 10), // Spread out over time
                Status = (SolveStatus)(i % 4) // Vary status
            });
        }

        return results;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
