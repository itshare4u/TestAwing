using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;

namespace TestAwing.Services;

/// <summary>
/// A parallel implementation of the treasure hunt solver using multi-threading
/// to optimize the dynamic programming algorithm
/// </summary>
public class ParallelTreasureHuntService
{
    private readonly TreasureHuntContext _context;
    private readonly int _maxDegreeOfParallelism;

    public ParallelTreasureHuntService(TreasureHuntContext context)
    {
        _context = context;
        // Set default parallelism to number of processors
        _maxDegreeOfParallelism = Environment.ProcessorCount;
    }

    public ParallelTreasureHuntService(TreasureHuntContext context, int maxDegreeOfParallelism)
    {
        _context = context;
        _maxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount;
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

        // Calculate minimum fuel required and get the path using parallel implementation
        var result = CalculateOptimalPath(request);
        var minFuel = result.MinFuel;
        var path = result.Path;

        // Save to database
        var dbResult = new TreasureHuntResult
        {
            N = request.N,
            M = request.M,
            P = request.P,
            MatrixJson = JsonSerializer.Serialize(request.Matrix),
            PathJson = JsonSerializer.Serialize(path),
            MinFuel = minFuel,
            CreatedAt = DateTime.UtcNow
        };

        _context.TreasureHuntResults.Add(dbResult);
        await _context.SaveChangesAsync();

        return new TreasureHuntResponse
        {
            MinFuel = minFuel,
            Id = dbResult.Id,
            Path = path
        };
    }

    public async Task<List<TreasureHuntResult>> GetAllTreasureHunts()
    {
        return await _context.TreasureHuntResults
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaginatedResponse<TreasureHuntResult>> GetPaginatedTreasureHunts(int page = 1, int pageSize = 8)
    {
        // Validate pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Limit max page size to 100

        var totalCount = await _context.TreasureHuntResults.CountAsync();
        
        var data = await _context.TreasureHuntResults
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<TreasureHuntResult>
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public TreasureHuntRequest GenerateRandomTestData(int n, int m, int p)
    {
        // Validate basic constraints
        if (n <= 0 || m <= 0 || p <= 0)
        {
            throw new ArgumentException("n, m, and p must be positive integers");
        }
        
        var random = new Random();
        var matrix = new int[n][];
        
        // Initialize matrix
        for (int i = 0; i < n; i++)
        {
            matrix[i] = new int[m];
        }

        // Create a list of all positions
        var positions = new List<(int row, int col)>();
        for (int row = 0; row < n; row++)
        {
            for (int col = 0; col < m; col++)
            {
                positions.Add((row, col));
            }
        }

        // Shuffle positions using Fisher-Yates algorithm
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        // Ensure each chest number from 1 to p appears at least once
        for (int chest = 1; chest <= p; chest++)
        {
            var pos = positions[chest - 1];
            matrix[pos.row][pos.col] = chest;
        }

        // Fill remaining positions with random values from 1 to (p-1)
        // This ensures the maximum chest number (p) appears exactly once
        for (int i = p; i < positions.Count; i++)
        {
            var pos = positions[i];
            matrix[pos.row][pos.col] = random.Next(1, p); // p-1 is the max value (exclusive upper bound)
        }

        return new TreasureHuntRequest
        {
            N = n,
            M = m,
            P = p,
            Matrix = matrix
        };
    }

    public async Task<TreasureHuntResultWithPath?> GetTreasureHuntById(int id)
    {
        var result = await _context.TreasureHuntResults.FindAsync(id);
        if (result == null) return null;

        var matrix = JsonSerializer.Deserialize<int[][]>(result.MatrixJson);
        var path = !string.IsNullOrEmpty(result.PathJson) 
            ? JsonSerializer.Deserialize<List<PathStep>>(result.PathJson) ?? new List<PathStep>()
            : new List<PathStep>();

        return new TreasureHuntResultWithPath
        {
            Id = result.Id,
            N = result.N,
            M = result.M,
            P = result.P,
            Matrix = matrix ?? Array.Empty<int[]>(),
            Path = path,
            MinFuel = result.MinFuel,
            CreatedAt = result.CreatedAt
        };
    }

    /// <summary>
    /// Calculate optimal path using the Held-Karp algorithm with parallel processing
    /// </summary>
    private TreasureHuntResponse CalculateOptimalPath(TreasureHuntRequest request)
    {
        var n = request.N;
        var m = request.M;
        var p = request.P;
        var matrix = request.Matrix;

        // Group positions by chest number - this can be parallelized
        var chestPositions = new ConcurrentDictionary<int, ConcurrentBag<(int row, int col)>>();
        
        // Initialize the dictionary with empty bags for all chest numbers
        for (int chest = 1; chest <= p; chest++) {
            chestPositions[chest] = new ConcurrentBag<(int row, int col)>();
        }
        
        // Use parallel processing to scan the matrix
        Parallel.For(0, n, new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, i => {
            for (int j = 0; j < m; j++) {
                var chestNum = matrix[i][j];
                // Only add positions that actually contain chests (ignore 0 values)
                if (chestNum > 0) {
                    chestPositions.GetOrAdd(chestNum, _ => new ConcurrentBag<(int, int)>()).Add((i, j));
                }
            }
        });

        // Validate all chests from 1 to p exist
        for (int chest = 1; chest <= p; chest++) {
            if (!chestPositions.ContainsKey(chest) || chestPositions[chest].IsEmpty)
                throw new ArgumentException($"Chest {chest} not found in matrix");
        }

        // Convert to List format for DP algorithm
        var chestOptions = new List<List<(int row, int col)>>();
        for (int chest = 1; chest <= p; chest++) {
            chestOptions.Add(chestPositions[chest].ToList());
        }

        // Choose algorithm based on problem size
        if (p <= 10) {
            return CalculateOptimalPathDP(chestOptions, p, matrix);
        } else {
            return CalculateOptimalPathHeuristic(chestOptions, p, matrix);
        }
    }

    /// <summary>
    /// Parallel implementation of the Held-Karp dynamic programming algorithm
    /// </summary>
    private TreasureHuntResponse CalculateOptimalPathDP(List<List<(int row, int col)>> chestOptions, int p, int[][] matrix)
    {
        var startPos = (row: 0, col: 0);
        
        // Get all possible positions for each chest type
        var positionCounts = new int[p];
        for (int i = 0; i < p; i++) {
            positionCounts[i] = chestOptions[i].Count;
        }

        // Initialize DP table: dp[i][j] = minimum fuel to reach chest i+1 at position j
        var dp = new double[p][];
        var parent = new (int chest, int pos)[p][];
        
        for (int i = 0; i < p; i++) {
            dp[i] = new double[positionCounts[i]];
            parent[i] = new (int chest, int pos)[positionCounts[i]];
        }
        
        // Initialize first chest distances in parallel
        Parallel.For(0, positionCounts[0], new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, j => {
            dp[0][j] = CalculateDistance(startPos, chestOptions[0][j]);
            parent[0][j] = (-1, -1); // Coming from start
        });

        // Initialize other chests with infinity
        for (int i = 1; i < p; i++) {
            Parallel.For(0, positionCounts[i], new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, j => {
                dp[i][j] = double.MaxValue;
            });
        }

        // Determine the optimal number of chunks for partitioning based on problem size
        // For small problems, don't use too many partitions
        int partitionCount = Math.Min(Environment.ProcessorCount, Math.Max(1, positionCounts.Max() / 10));
        
        // Fill the DP table using the recurrence relation with parallel processing
        for (int i = 0; i < p - 1; i++) { // For each chest (except the last one)
            int nextChestPositions = positionCounts[i+1];
            int currentChestPositions = positionCounts[i];
            
            // Use partitioning for better load balancing
            // For very small position counts, use simpler parallelization or even sequential processing
            if (nextChestPositions < 10) {
                // For small position counts, process sequentially to avoid parallelization overhead
                for (int j = 0; j < nextChestPositions; j++) {
                    var nextPos = chestOptions[i+1][j];
                    double minCost = double.MaxValue;
                    int bestPrevPos = -1;
                    
                    for (int k = 0; k < currentChestPositions; k++) {
                        var prevPos = chestOptions[i][k];
                        var cost = dp[i][k] + CalculateDistance(prevPos, nextPos);
                        
                        if (cost < minCost) {
                            minCost = cost;
                            bestPrevPos = k;
                        }
                    }
                    
                    dp[i+1][j] = minCost;
                    parent[i+1][j] = (i, bestPrevPos);
                }
            }
            else {
                Parallel.ForEach(
                    Partitioner.Create(0, nextChestPositions, nextChestPositions / partitionCount), 
                    new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism },
                    range => {
                        for (int j = range.Item1; j < range.Item2; j++) {
                            var nextPos = chestOptions[i+1][j];
                            
                            // Local variables for thread-safe operation
                            double minCost = double.MaxValue;
                            int bestPrevPos = -1;
                            
                            for (int k = 0; k < currentChestPositions; k++) {
                                var prevPos = chestOptions[i][k];
                                var cost = dp[i][k] + CalculateDistance(prevPos, nextPos);
                                
                                if (cost < minCost) {
                                    minCost = cost;
                                    bestPrevPos = k;
                                }
                            }
                            
                            // Thread-safe update of the shared dp and parent arrays
                            dp[i+1][j] = minCost;
                            parent[i+1][j] = (i, bestPrevPos);
                        }
                    }
                );
            }
        }

        // Find the minimum fuel position in parallel
        var lastChestIndex = p - 1;
        var lastChestPositionCount = positionCounts[lastChestIndex];
        
        // For small position counts, use simple approach
        if (lastChestPositionCount < 10) {
            var minFuel = double.MaxValue;
            var lastChestBestPos = 0;
            
            for (int j = 0; j < lastChestPositionCount; j++) {
                if (dp[lastChestIndex][j] < minFuel) {
                    minFuel = dp[lastChestIndex][j];
                    lastChestBestPos = j;
                }
            }
            
            return ReconstructPath(startPos, chestOptions, dp, parent, lastChestIndex, lastChestBestPos, minFuel);
        }
        else {
            // For larger problems, use parallel approach
            var resultsBuffer = new ConcurrentBag<(double fuel, int pos)>();
            
            Parallel.ForEach(
                Partitioner.Create(0, lastChestPositionCount, lastChestPositionCount / partitionCount),
                new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism },
                range => {
                    double localMinFuel = double.MaxValue;
                    int localBestPos = -1;
                    
                    for (int j = range.Item1; j < range.Item2; j++) {
                        if (dp[lastChestIndex][j] < localMinFuel) {
                            localMinFuel = dp[lastChestIndex][j];
                            localBestPos = j;
                        }
                    }
                    
                    if (localBestPos != -1) {
                        resultsBuffer.Add((localMinFuel, localBestPos));
                    }
                }
            );
            
            // Find global minimum
            var minFuel = double.MaxValue;
            var lastChestBestPos = 0;
            
            foreach (var (fuel, pos) in resultsBuffer) {
                if (fuel < minFuel) {
                    minFuel = fuel;
                    lastChestBestPos = pos;
                }
            }
            
            // Fallback if parallel search failed
            if (minFuel == double.MaxValue && lastChestPositionCount > 0) {
                for (int j = 0; j < lastChestPositionCount; j++) {
                    if (dp[lastChestIndex][j] < minFuel) {
                        minFuel = dp[lastChestIndex][j];
                        lastChestBestPos = j;
                    }
                }
            }
            
            return ReconstructPath(startPos, chestOptions, dp, parent, lastChestIndex, lastChestBestPos, minFuel);
        }
    }

    /// <summary>
    /// Helper method to reconstruct path from DP table
    /// </summary>
    private TreasureHuntResponse ReconstructPath(
        (int row, int col) startPos, 
        List<List<(int row, int col)>> chestOptions,
        double[][] dp,
        (int chest, int pos)[][] parent,
        int lastChestIndex,
        int lastChestBestPos,
        double minFuel)
    {
        // Reconstruct the path (this is sequential since it follows dependencies)
        var path = new List<PathStep>();
        
        // Add start position
        path.Add(new PathStep {
            ChestNumber = 0,
            Row = startPos.row,
            Col = startPos.col,
            FuelUsed = 0,
            CumulativeFuel = 0
        });

        // Backtrack to construct the path
        var pathPositions = new List<(int chest, int row, int col)>();
        int currentChestIdx = lastChestIndex;
        int currentPosIdx = lastChestBestPos;
        
        while (currentChestIdx >= 0) {
            var position = chestOptions[currentChestIdx][currentPosIdx];
            pathPositions.Add((currentChestIdx + 1, position.row, position.col));
            
            if (currentChestIdx == 0) break;
            
            var (prevChest, prevPos) = parent[currentChestIdx][currentPosIdx];
            currentChestIdx = prevChest;
            currentPosIdx = prevPos;
        }
        
        // Reverse the path to get chronological order
        pathPositions.Reverse();
        
        // Convert to PathStep objects with proper fuel calculations
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

    /// <summary>
    /// Heuristic approach for larger p values with parallel processing
    /// </summary>
    private TreasureHuntResponse CalculateOptimalPathHeuristic(List<List<(int row, int col)>> chestOptions, int p, int[][] matrix)
    {
        var startPos = (row: 0, col: 0);
        var path = new List<PathStep>();
        
        // Always start with the start position
        path.Add(new PathStep
        {
            ChestNumber = 0,
            Row = startPos.row,
            Col = startPos.col,
            FuelUsed = 0,
            CumulativeFuel = 0
        });
        
        // Visit chests in order 1, 2, 3, ..., p
        var currentPos = startPos;
        var totalFuel = 0.0;
        
        // For very large problems, we can use a sliding window approach
        // This processes multiple chest types in parallel, while maintaining the order constraint
        // Define window size for parallel processing of chest types
        int windowSize = Math.Min(4, p); // Process up to 4 chest types at once, adjust as needed
        
        for (int chestWindow = 1; chestWindow <= p; chestWindow += windowSize)
        {
            // Calculate the actual window size for this iteration
            int actualWindowSize = Math.Min(windowSize, p - chestWindow + 1);
            
            // Create arrays to store best positions and distances for this window
            var bestPositions = new (int row, int col, double distance)[actualWindowSize];
            
            // Initialize with default values
            for (int i = 0; i < actualWindowSize; i++)
            {
                bestPositions[i] = (-1, -1, double.MaxValue);
            }
            
            // Process each chest type in this window
            Parallel.For(0, actualWindowSize, new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, windowIndex =>
            {
                int chestType = chestWindow + windowIndex;
                var positions = chestOptions[chestType - 1];
                
                // Skip if this chest is beyond p
                if (chestType > p) return;
                
                // For each position of this chest type, find the minimum distance
                double minDistance = double.MaxValue;
                (int row, int col) bestPosition = (-1, -1);
                
                foreach (var (row, col) in positions)
                {
                    var distance = CalculateDistance(currentPos, (row, col));
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestPosition = (row, col);
                    }
                }
                
                // Store the best position for this chest type
                bestPositions[windowIndex] = (bestPosition.row, bestPosition.col, minDistance);
            });
            
            // Process the results sequentially to maintain the correct order
            for (int i = 0; i < actualWindowSize; i++)
            {
                int chestType = chestWindow + i;
                
                // Skip if this chest is beyond p
                if (chestType > p) break;
                
                var (row, col, distance) = bestPositions[i];
                
                // Verify we found a valid position
                if (row == -1 || col == -1)
                {
                    throw new InvalidOperationException($"Failed to find a valid position for chest {chestType}");
                }
                
                // Update the current position and total fuel
                totalFuel += distance;
                currentPos = (row, col);
                
                // Add to path
                path.Add(new PathStep
                {
                    ChestNumber = chestType,
                    Row = row,
                    Col = col,
                    FuelUsed = distance,
                    CumulativeFuel = totalFuel
                });
            }
        }
        
        return new TreasureHuntResponse
        {
            MinFuel = totalFuel,
            Path = path
        };
    }

    /// <summary>
    /// Calculate Euclidean distance between two positions
    /// Optimized for performance using squared distance comparison when possible
    /// </summary>
    private static double CalculateDistance((int row, int col) from, (int row, int col) to)
    {
        var deltaX = to.row - from.row;
        var deltaY = to.col - from.col;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
    
    /// <summary>
    /// Calculate squared Euclidean distance between two positions
    /// Use this for comparisons when you don't need the exact distance
    /// This avoids the expensive square root operation
    /// </summary>
    private static double CalculateSquaredDistance((int row, int col) from, (int row, int col) to)
    {
        var deltaX = to.row - from.row;
        var deltaY = to.col - from.col;
        return deltaX * deltaX + deltaY * deltaY;
    }
}
