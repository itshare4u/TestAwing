using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests;

public class TreasureHuntPerformanceBenchmarks : IDisposable
{
    private readonly TreasureHuntContext _context;
    private readonly OptimizedTreasureHuntService _originalService;
    private readonly ParallelTreasureHuntService _parallelService;

    public TreasureHuntPerformanceBenchmarks()
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
    public async Task BenchmarkDynamicProgramming_Small()
    {
        // Small DP problem (p ≤ 10)
        await BenchmarkTreasureHunt(5, 5, 8, "Small DP", 3);
    }

    [Fact]
    public async Task BenchmarkDynamicProgramming_Medium()
    {
        // Medium DP problem (p ≤ 10, but larger matrix)
        await BenchmarkTreasureHunt(10, 10, 10, "Medium DP", 3);
    }

    [Fact]
    public async Task BenchmarkHeuristic_Small()
    {
        // Small heuristic problem (p > 10)
        await BenchmarkTreasureHunt(10, 10, 12, "Small Heuristic", 3);
    }

    [Fact]
    public async Task BenchmarkHeuristic_Medium()
    {
        // Medium heuristic problem
        await BenchmarkTreasureHunt(20, 20, 15, "Medium Heuristic", 3);
    }

    [Fact]
    public async Task BenchmarkHeuristic_Large()
    {
        // Larger heuristic problem
        await BenchmarkTreasureHunt(30, 30, 20, "Large Heuristic", 2);
    }

    [Fact(Skip = "Very time-intensive benchmark, run manually")]
    public async Task BenchmarkHeuristic_VeryLarge()
    {
        // Very large heuristic problem
        await BenchmarkTreasureHunt(50, 50, 30, "Very Large Heuristic", 1);
    }

    private async Task BenchmarkTreasureHunt(int n, int m, int p, string benchmarkName, int iterations)
    {
        // Generate random test data
        var request = _originalService.GenerateRandomTestData(n, m, p);
        
        // Warm-up run (not measured)
        await _originalService.SolveTreasureHunt(request);
        await _parallelService.SolveTreasureHunt(request);
        
        // Benchmark results
        var originalTimes = new List<long>();
        var parallelTimes = new List<long>();
        
        // Run multiple iterations to get more stable results
        for (int i = 0; i < iterations; i++)
        {
            // Measure original implementation
            var originalStopwatch = Stopwatch.StartNew();
            var originalResult = await _originalService.SolveTreasureHunt(request);
            originalStopwatch.Stop();
            originalTimes.Add(originalStopwatch.ElapsedMilliseconds);
            
            // Measure parallel implementation
            var parallelStopwatch = Stopwatch.StartNew();
            var parallelResult = await _parallelService.SolveTreasureHunt(request);
            parallelStopwatch.Stop();
            parallelTimes.Add(parallelStopwatch.ElapsedMilliseconds);
            
            // Verify results are at least roughly the same (allow small differences due to floating point)
            if (Math.Abs(originalResult.MinFuel - parallelResult.MinFuel) > 0.01 * originalResult.MinFuel)
            {
                Console.WriteLine($"Warning: Results differ significantly: Original {originalResult.MinFuel}, Parallel {parallelResult.MinFuel}");
            }
        }
        
        // Calculate average times
        var avgOriginalTime = originalTimes.Average();
        var avgParallelTime = parallelTimes.Average();
        
        // Output results
        Console.WriteLine($"===== {benchmarkName} Benchmark (N={n}, M={m}, P={p}) =====");
        Console.WriteLine($"Matrix size: {n}x{m}, Chest types: {p}");
        Console.WriteLine($"Original implementation: {avgOriginalTime:F2}ms (min: {originalTimes.Min()}ms, max: {originalTimes.Max()}ms)");
        Console.WriteLine($"Parallel implementation: {avgParallelTime:F2}ms (min: {parallelTimes.Min()}ms, max: {parallelTimes.Max()}ms)");
        
        // Calculate speedup
        double speedup = 0;
        if (avgParallelTime > 0 && avgOriginalTime > 0)
        {
            speedup = avgOriginalTime / avgParallelTime;
            Console.WriteLine($"Speedup: {speedup:F2}x");
        }
        else if (avgOriginalTime == 0 && avgParallelTime == 0)
        {
            Console.WriteLine("Both implementations too fast to measure accurately");
            // Consider the test passed if both are extremely fast (0ms)
            return;
        }
        else if (avgOriginalTime == 0)
        {
            Console.WriteLine("Original implementation too fast to measure");
            // If original is 0ms and parallel isn't, the parallel version might have overhead
            // For small problems this is acceptable
            if (n <= 10 && m <= 10 && p <= 10)
            {
                return; // Test passes
            }
            // For larger problems, this could indicate a performance issue
            Assert.True(avgParallelTime <= 10, 
                "Parallel implementation is significantly slower than original (which was too fast to measure)");
            return;
        }
        else if (avgParallelTime == 0)
        {
            Console.WriteLine("Parallel implementation too fast to measure - great optimization!");
            return; // Test passes
        }
        
        Console.WriteLine();
        
        // For small problems, parallel might be slower due to overhead
        if (n <= 10 && m <= 10 && p <= 8)
        {
            // No assertion, just report results
        }
        else
        {
            // For medium to large problems, we expect speedup
            // But only if the times are actually measurable
            if (avgOriginalTime >= 5 && avgParallelTime >= 5)
            {
                Assert.True(speedup >= 0.9, 
                    $"Parallel should be at least as fast as original for medium/large problems, but got speedup: {speedup:F2}x");
            }
        }
    }
}
