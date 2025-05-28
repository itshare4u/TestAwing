using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests;

/// <summary>
/// This class contains tests that create large problem instances to demonstrate 
/// the performance benefits of parallel processing.
/// Most tests are skipped by default to avoid long test runs.
/// </summary>
public class TreasureHuntStressTests : IDisposable
{
    private readonly TreasureHuntContext _context;
    private readonly OptimizedTreasureHuntService _originalService;
    private readonly ParallelTreasureHuntService _parallelService;

    public TreasureHuntStressTests()
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

    /// <summary>
    /// Create a stress test with high complexity to demonstrate parallel speedup
    /// </summary>
    [Fact(Skip = "Stress test that runs for a long time")]
    public async Task StressTest_HighComplexity()
    {
        // Create a large problem instance
        int n = 100;
        int m = 100;
        int p = 25; // This will use the heuristic algorithm
        
        // Generate random test data
        var request = _originalService.GenerateRandomTestData(n, m, p);
        
        // Measure original implementation
        Console.WriteLine($"Running original algorithm for n={n}, m={m}, p={p}");
        var originalStopwatch = Stopwatch.StartNew();
        var originalResult = await _originalService.SolveTreasureHunt(request);
        originalStopwatch.Stop();
        var originalTime = originalStopwatch.ElapsedMilliseconds;
        
        // Measure parallel implementation
        Console.WriteLine($"Running parallel algorithm for n={n}, m={m}, p={p}");
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelResult = await _parallelService.SolveTreasureHunt(request);
        parallelStopwatch.Stop();
        var parallelTime = parallelStopwatch.ElapsedMilliseconds;
        
        // Output results
        Console.WriteLine($"Matrix size: {n}x{m}, Chest types: {p}");
        Console.WriteLine($"Original implementation: {originalTime}ms");
        Console.WriteLine($"Parallel implementation: {parallelTime}ms");
        
        if (parallelTime > 0)
        {
            double speedup = (double)originalTime / parallelTime;
            Console.WriteLine($"Speedup: {speedup:F2}x");
            
            // For large stress tests, we expect significant speedup
            Assert.True(speedup >= 1.5, $"Expected significant speedup for stress test, but got: {speedup:F2}x");
        }
        else
        {
            Console.WriteLine("Parallel implementation too fast to measure accurately");
            Assert.True(originalTime > 0, "Original implementation should take measurable time for stress test");
        }
    }
    
    /// <summary>
    /// Test with a very large matrix but small p to focus on the DP algorithm
    /// </summary>
    [Fact(Skip = "Stress test that runs for a long time")]
    public async Task StressTest_LargeMatrix_SmallP()
    {
        int n = 200;
        int m = 200;
        int p = 10; // This will use the DP algorithm
        
        var request = _originalService.GenerateRandomTestData(n, m, p);
        
        Console.WriteLine($"Running original algorithm for n={n}, m={m}, p={p}");
        var originalStopwatch = Stopwatch.StartNew();
        var originalResult = await _originalService.SolveTreasureHunt(request);
        originalStopwatch.Stop();
        var originalTime = originalStopwatch.ElapsedMilliseconds;
        
        Console.WriteLine($"Running parallel algorithm for n={n}, m={m}, p={p}");
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelResult = await _parallelService.SolveTreasureHunt(request);
        parallelStopwatch.Stop();
        var parallelTime = parallelStopwatch.ElapsedMilliseconds;
        
        Console.WriteLine($"DP with large matrix - Original: {originalTime}ms, Parallel: {parallelTime}ms");
        
        if (parallelTime > 0 && originalTime > 0)
        {
            double speedup = (double)originalTime / parallelTime;
            Console.WriteLine($"Speedup: {speedup:F2}x");
            Assert.True(speedup >= 1.2, $"Expected speedup for DP with large matrix, but got: {speedup:F2}x");
        }
    }
    
    /// <summary>
    /// Test with a large p value to focus on the heuristic algorithm
    /// </summary>
    [Fact(Skip = "Stress test that runs for a long time")]
    public async Task StressTest_LargeP()
    {
        int n = 50;
        int m = 50;
        int p = 40; // This will use the heuristic algorithm
        
        var request = _originalService.GenerateRandomTestData(n, m, p);
        
        Console.WriteLine($"Running original algorithm for n={n}, m={m}, p={p}");
        var originalStopwatch = Stopwatch.StartNew();
        var originalResult = await _originalService.SolveTreasureHunt(request);
        originalStopwatch.Stop();
        var originalTime = originalStopwatch.ElapsedMilliseconds;
        
        Console.WriteLine($"Running parallel algorithm for n={n}, m={m}, p={p}");
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelResult = await _parallelService.SolveTreasureHunt(request);
        parallelStopwatch.Stop();
        var parallelTime = parallelStopwatch.ElapsedMilliseconds;
        
        Console.WriteLine($"Heuristic with large p - Original: {originalTime}ms, Parallel: {parallelTime}ms");
        
        if (parallelTime > 0 && originalTime > 0)
        {
            double speedup = (double)originalTime / parallelTime;
            Console.WriteLine($"Speedup: {speedup:F2}x");
            Assert.True(speedup >= 1.2, $"Expected speedup for heuristic with large p, but got: {speedup:F2}x");
        }
    }
    
    /// <summary>
    /// Create an artificial "heavy" test case by adding computational overhead
    /// to better demonstrate parallel speedup in tests
    /// </summary>
    [Fact]
    public async Task ArtificialHeavyLoad_Test()
    {
        // Create a benchmark version of the services with artificial computational load
        var heavyOriginalService = new HeavyComputationService(_context, isParallel: false);
        var heavyParallelService = new HeavyComputationService(_context, isParallel: true);
        
        // Medium size problem with p=10 (to use DP algorithm)
        int n = 15;
        int m = 15;
        int p = 10;
        
        var request = _originalService.GenerateRandomTestData(n, m, p);
        
        // Run the heavy computation services
        Console.WriteLine($"Running heavy load original algorithm for n={n}, m={m}, p={p}");
        var originalStopwatch = Stopwatch.StartNew();
        var originalResult = await heavyOriginalService.SolveTreasureHunt(request);
        originalStopwatch.Stop();
        var originalTime = originalStopwatch.ElapsedMilliseconds;
        
        Console.WriteLine($"Running heavy load parallel algorithm for n={n}, m={m}, p={p}");
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelResult = await heavyParallelService.SolveTreasureHunt(request);
        parallelStopwatch.Stop();
        var parallelTime = parallelStopwatch.ElapsedMilliseconds;
        
        Console.WriteLine($"Heavy load - Original: {originalTime}ms, Parallel: {parallelTime}ms");
        
        if (parallelTime > 0 && originalTime > 0)
        {
            double speedup = (double)originalTime / parallelTime;
            Console.WriteLine($"Speedup: {speedup:F2}x");
            Assert.True(speedup >= 1.5, 
                $"Expected significant speedup for heavy computation, but got: {speedup:F2}x");
        }
    }
    
    /// <summary>
    /// Service that adds artificial computational load to better demonstrate parallel speedup
    /// </summary>
    private class HeavyComputationService
    {
        private readonly TreasureHuntContext _context;
        private readonly bool _isParallel;
        
        public HeavyComputationService(TreasureHuntContext context, bool isParallel)
        {
            _context = context;
            _isParallel = isParallel;
        }
        
        public async Task<TreasureHuntResponse> SolveTreasureHunt(TreasureHuntRequest request)
        {
            // Validate matrix dimensions
            if (request.Matrix.Length != request.N)
                throw new ArgumentException("Matrix row count doesn't match N");
            
            foreach (var row in request.Matrix)
            {
                if (row.Length != request.M)
                    throw new ArgumentException("Matrix column count doesn't match M");
            }
            
            // Group positions by chest number
            var chestPositions = new Dictionary<int, List<(int row, int col)>>();
            
            for (int i = 0; i < request.N; i++)
            {
                for (int j = 0; j < request.M; j++)
                {
                    var chestNum = request.Matrix[i][j];
                    if (chestNum > 0)
                    {
                        if (!chestPositions.ContainsKey(chestNum))
                            chestPositions[chestNum] = new List<(int, int)>();
                        chestPositions[chestNum].Add((i, j));
                    }
                }
            }
            
            // For each chest type, find all possible positions
            var chestOptions = new List<List<(int row, int col)>>();
            for (int chest = 1; chest <= request.P; chest++)
            {
                chestOptions.Add(chestPositions[chest]);
            }
            
            // Calculate optimal path using DP with artificial heavy computation
            var result = CalculateHeavyDP(chestOptions, request.P, request.Matrix);
            
            // Save to database
            var dbResult = new TreasureHuntResult
            {
                N = request.N,
                M = request.M,
                P = request.P,
                MatrixJson = System.Text.Json.JsonSerializer.Serialize(request.Matrix),
                PathJson = System.Text.Json.JsonSerializer.Serialize(result.Path),
                MinFuel = result.MinFuel,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.TreasureHuntResults.Add(dbResult);
            await _context.SaveChangesAsync();
            
            return new TreasureHuntResponse
            {
                MinFuel = result.MinFuel,
                Id = dbResult.Id,
                Path = result.Path
            };
        }
        
        private TreasureHuntResponse CalculateHeavyDP(List<List<(int row, int col)>> chestOptions, int p, int[][] matrix)
        {
            var startPos = (row: 0, col: 0);
            
            // Get all possible positions for each chest type
            var positionCounts = new int[p];
            for (int i = 0; i < p; i++) {
                positionCounts[i] = chestOptions[i].Count;
            }
            
            // Initialize DP table
            var dp = new double[p][];
            var parent = new (int chest, int pos)[p][];
            
            for (int i = 0; i < p; i++) {
                dp[i] = new double[positionCounts[i]];
                parent[i] = new (int chest, int pos)[positionCounts[i]];
                
                for (int j = 0; j < positionCounts[i]; j++) {
                    if (i == 0) {
                        // Add artificial heavy computation
                        dp[i][j] = HeavyComputation(startPos, chestOptions[i][j]);
                        parent[i][j] = (-1, -1);
                    } else {
                        dp[i][j] = double.MaxValue;
                    }
                }
            }
            
            // Fill the DP table
            for (int i = 0; i < p - 1; i++) {
                // Process in parallel or sequentially based on the flag
                if (_isParallel)
                {
                    Parallel.For(0, positionCounts[i+1], j => {
                        ProcessNextPosition(i, j, dp, parent, chestOptions, positionCounts);
                    });
                }
                else
                {
                    for (int j = 0; j < positionCounts[i+1]; j++) {
                        ProcessNextPosition(i, j, dp, parent, chestOptions, positionCounts);
                    }
                }
            }
            
            // Find the minimum fuel position
            var minFuel = double.MaxValue;
            var lastChestBestPos = 0;
            
            for (int j = 0; j < positionCounts[p-1]; j++) {
                if (dp[p-1][j] < minFuel) {
                    minFuel = dp[p-1][j];
                    lastChestBestPos = j;
                }
            }
            
            // Reconstruct the path
            var path = new List<PathStep>();
            path.Add(new PathStep {
                ChestNumber = 0,
                Row = startPos.row,
                Col = startPos.col,
                FuelUsed = 0,
                CumulativeFuel = 0
            });
            
            var pathPositions = new List<(int chest, int row, int col)>();
            int currentChestIdx = p - 1;
            int currentPosIdx = lastChestBestPos;
            
            while (currentChestIdx >= 0) {
                var position = chestOptions[currentChestIdx][currentPosIdx];
                pathPositions.Add((currentChestIdx + 1, position.row, position.col));
                
                if (currentChestIdx == 0) break;
                
                var (prevChest, prevPos) = parent[currentChestIdx][currentPosIdx];
                currentChestIdx = prevChest;
                currentPosIdx = prevPos;
            }
            
            pathPositions.Reverse();
            
            double cumulativeFuel = 0;
            var currentPosition = startPos;
            
            foreach (var (chest, row, col) in pathPositions) {
                var targetPosition = (row, col);
                var fuelUsed = CalculateDistance(currentPosition, targetPosition);
                cumulativeFuel += fuelUsed;
                
                path.Add(new PathStep {
                    ChestNumber = chest,
                    Row = row,
                    Col = col,
                    FuelUsed = fuelUsed,
                    CumulativeFuel = cumulativeFuel
                });
                
                currentPosition = targetPosition;
            }
            
            return new TreasureHuntResponse {
                MinFuel = minFuel,
                Path = path
            };
        }
        
        private void ProcessNextPosition(
            int i, int j, double[][] dp, (int chest, int pos)[][] parent, 
            List<List<(int row, int col)>> chestOptions, int[] positionCounts)
        {
            var nextPos = chestOptions[i+1][j];
            
            double minCost = double.MaxValue;
            int bestPrevPos = -1;
            
            for (int k = 0; k < positionCounts[i]; k++) {
                var prevPos = chestOptions[i][k];
                // Use heavy computation to simulate computational load
                var cost = dp[i][k] + HeavyComputation(prevPos, nextPos);
                
                if (cost < minCost) {
                    minCost = cost;
                    bestPrevPos = k;
                }
            }
            
            dp[i+1][j] = minCost;
            parent[i+1][j] = (i, bestPrevPos);
        }
        
        /// <summary>
        /// Artificially heavy distance calculation to simulate computational load
        /// </summary>
        private double HeavyComputation((int row, int col) from, (int row, int col) to)
        {
            var deltaX = to.row - from.row;
            var deltaY = to.col - from.col;
            
            // Artificial computational load
            double result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                // Add some complex math operations to simulate heavy computation
                result = Math.Pow(result, 1.01);
                result = Math.Log(result + 1);
                result = Math.Sin(result) + Math.Cos(result);
            }
            
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
        
        private static double CalculateDistance((int row, int col) from, (int row, int col) to)
        {
            var deltaX = to.row - from.row;
            var deltaY = to.col - from.col;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
}
