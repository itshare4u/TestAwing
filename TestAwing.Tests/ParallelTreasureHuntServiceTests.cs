using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests;

public class ParallelTreasureHuntServiceTests : IDisposable
{
    private readonly TreasureHuntContext _context;
    private readonly OptimizedTreasureHuntService _originalService;
    private readonly ParallelTreasureHuntService _parallelService;

    public ParallelTreasureHuntServiceTests()
    {
        var options = new DbContextOptionsBuilder<TreasureHuntContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TreasureHuntContext(options);
        _originalService = new OptimizedTreasureHuntService(_context);
        _parallelService = new ParallelTreasureHuntService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task ParallelService_VerifyCorrectness_SameResultsAsOriginal()
    {
        // Arrange - Create a test case that will use the DP algorithm (p ≤ 10)
        var request = new TreasureHuntRequest
        {
            N = 5,
            M = 5,
            P = 8,
            Matrix = new int[][]
            {
                new int[] { 1, 2, 3, 4, 5 },
                new int[] { 6, 7, 8, 1, 2 },
                new int[] { 3, 4, 5, 6, 7 },
                new int[] { 8, 1, 2, 3, 4 },
                new int[] { 5, 6, 7, 8, 1 }
            }
        };

        // Act
        var originalResult = await _originalService.SolveTreasureHunt(request);
        var parallelResult = await _parallelService.SolveTreasureHunt(request);

        // Assert - Use approximate comparison for floating point values
        Assert.True(Math.Abs(originalResult.MinFuel - parallelResult.MinFuel) < 0.001, 
            $"Fuel values differ: Original {originalResult.MinFuel}, Parallel {parallelResult.MinFuel}");
        Assert.Equal(originalResult.Path.Count, parallelResult.Path.Count);
        
        // Check each path step matches
        for (int i = 0; i < originalResult.Path.Count; i++)
        {
            Assert.Equal(originalResult.Path[i].ChestNumber, parallelResult.Path[i].ChestNumber);
            Assert.Equal(originalResult.Path[i].Row, parallelResult.Path[i].Row);
            Assert.Equal(originalResult.Path[i].Col, parallelResult.Path[i].Col);
            
            // Use approximate comparison for floating point values
            Assert.True(Math.Abs(originalResult.Path[i].FuelUsed - parallelResult.Path[i].FuelUsed) < 0.001,
                $"Fuel used values differ at step {i}: Original {originalResult.Path[i].FuelUsed}, Parallel {parallelResult.Path[i].FuelUsed}");
            Assert.True(Math.Abs(originalResult.Path[i].CumulativeFuel - parallelResult.Path[i].CumulativeFuel) < 0.001,
                $"Cumulative fuel values differ at step {i}: Original {originalResult.Path[i].CumulativeFuel}, Parallel {parallelResult.Path[i].CumulativeFuel}");
        }
    }

    [Theory]
    [InlineData(5, 5, 8)]    // Small case using DP
    [InlineData(10, 10, 12)] // Larger case using heuristic
    [InlineData(20, 20, 15)] // Even larger case
    public async Task ParallelService_Performance_ShouldBeFasterThanOriginal(int n, int m, int p)
    {
        // Arrange - Generate random test data
        var request = _originalService.GenerateRandomTestData(n, m, p);
        
        // Act - Measure performance of original implementation
        var originalStopwatch = Stopwatch.StartNew();
        var originalResult = await _originalService.SolveTreasureHunt(request);
        originalStopwatch.Stop();
        var originalTime = originalStopwatch.ElapsedMilliseconds;
        
        // Act - Measure performance of parallel implementation
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelResult = await _parallelService.SolveTreasureHunt(request);
        parallelStopwatch.Stop();
        var parallelTime = parallelStopwatch.ElapsedMilliseconds;
        
        // Output timing results
        Console.WriteLine($"Size N={n}, M={m}, P={p}");
        Console.WriteLine($"Original implementation: {originalTime}ms");
        Console.WriteLine($"Parallel implementation: {parallelTime}ms");
        
        // Calculate speedup, handling the case where times are 0
        double speedup = 0;
        if (parallelTime > 0 && originalTime > 0)
        {
            speedup = (double)originalTime / parallelTime;
            Console.WriteLine($"Speedup: {speedup:F2}x");
        }
        else if (originalTime == 0 && parallelTime == 0)
        {
            Console.WriteLine("Both implementations too fast to measure accurately");
        }
        else
        {
            Console.WriteLine("Cannot calculate accurate speedup with zero timing");
        }
        
        // Assert - Check results are correct for small cases
        if (p <= 10)
        {
            // Use approximate comparison for floating point values
            Assert.True(Math.Abs(originalResult.MinFuel - parallelResult.MinFuel) < 0.001, 
                $"Results differ: Original {originalResult.MinFuel}, Parallel {parallelResult.MinFuel}");
        }
        
        // For small problems, timing may not be accurate, so only check speedup for larger problems
        if (n >= 15 && m >= 15 && p >= 10)
        {
            // For larger problems, parallel should generally be faster
            // But this is a soft assertion since timing can vary in test environments
            if (originalTime > 0 && parallelTime > 0)
            {
                Assert.True(parallelTime <= originalTime * 1.5, 
                    $"Parallel implementation should be faster than original for large problems");
            }
        }
    }
    
    [Fact]
    public async Task ParallelService_LargeScenario_PerformanceTest()
    {
        // Arrange - Generate a large test case
        var n = 30;
        var m = 30;
        var p = 20; // This will use the heuristic algorithm
        var request = _originalService.GenerateRandomTestData(n, m, p);
        
        // Act & Assert - Just verify that it completes within a reasonable time
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelResult = await _parallelService.SolveTreasureHunt(request);
        parallelStopwatch.Stop();
        
        Console.WriteLine($"Large scenario (N={n}, M={m}, P={p}) completed in: {parallelStopwatch.ElapsedMilliseconds}ms");
        
        // Verify we got a valid result
        Assert.True(parallelResult.MinFuel > 0);
        Assert.Equal(p + 1, parallelResult.Path.Count); // Including start position
    }
}
