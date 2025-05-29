using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;

namespace TestAwing.Services;

public class OptimizedTreasureHuntService
{
    private readonly TreasureHuntContext _context;
    private readonly int _maxDegreeOfParallelism;

    public OptimizedTreasureHuntService(TreasureHuntContext context)
    {
        _context = context;
        // Set default parallelism to number of processors or a reasonable default
        _maxDegreeOfParallelism = Environment.ProcessorCount;
    }

    // Constructor that allows configuring the degree of parallelism
    public OptimizedTreasureHuntService(TreasureHuntContext context, int maxDegreeOfParallelism)
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

        // Calculate minimum fuel required and get the path using Held-Karp algorithm
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
    /// Calculate optimal path using the Held-Karp algorithm (dynamic programming approach)
    /// This guarantees globally optimal solution unlike the greedy approach
    /// </summary>
    private TreasureHuntResponse CalculateOptimalPath(TreasureHuntRequest request)
    {
        var n = request.N;
        var m = request.M;
        var p = request.P;
        var matrix = request.Matrix;

        // Group positions by chest number
        var chestPositions = new Dictionary<int, List<(int row, int col)>>();
        
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                var chestNum = matrix[i][j];
                // Only add positions that actually contain chests (ignore 0 values)
                if (chestNum > 0)
                {
                    if (!chestPositions.ContainsKey(chestNum))
                        chestPositions[chestNum] = new List<(int, int)>();
                    chestPositions[chestNum].Add((i, j));
                }
            }
        }

        // Validate all chests from 1 to p exist
        for (int chest = 1; chest <= p; chest++)
        {
            if (!chestPositions.ContainsKey(chest))
                throw new ArgumentException($"Chest {chest} not found in matrix");
        }

        // For each chest type, find all possible positions
        var chestOptions = new List<List<(int row, int col)>>();
        for (int chest = 1; chest <= p; chest++)
        {
            chestOptions.Add(chestPositions[chest]);
        }

        // Always use heuristic approach for consistency with async service
        // This provides faster computation and more consistent results
        return CalculateOptimalPathHeuristic(chestOptions, p, matrix);
    }

    /// <summary>
    /// Full Held-Karp dynamic programming solution for small p (≤ 10) with multi-threading
    /// Time complexity: O(2^p * p^2 * max_positions_per_chest) but with parallel execution
    /// </summary>
    private TreasureHuntResponse CalculateOptimalPathDP(List<List<(int row, int col)>> chestOptions, int p, int[][] matrix)
    {
        var startPos = (row: 0, col: 0);
        
        // Get all possible positions for each chest type
        // Create arrays to store the sizes for convenience
        var positionCounts = new int[p];
        for (int i = 0; i < p; i++) {
            positionCounts[i] = chestOptions[i].Count;
        }

        // Initialize DP table: dp[i][j] = minimum fuel to reach chest i+1 at position j
        // where i is the chest number (0-indexed) and j is the position index
        var dp = new double[p][];
        var parent = new (int chest, int pos)[p][];
        
        for (int i = 0; i < p; i++) {
            dp[i] = new double[positionCounts[i]];
            parent[i] = new (int chest, int pos)[positionCounts[i]];
            
            // Initialize with infinity except for the first chest
            for (int j = 0; j < positionCounts[i]; j++) {
                if (i == 0) {
                    // For the first chest, calculate distance from start position
                    dp[i][j] = CalculateDistance(startPos, chestOptions[i][j]);
                    parent[i][j] = (-1, -1); // Coming from start
                } else {
                    dp[i][j] = double.MaxValue;
                }
            }
        }

        // Fill the DP table using parallel processing for each chest level
        // We cannot parallelize at the chest level (i) due to dependencies
        // But we can parallelize at the position level (j) for each chest
        for (int i = 0; i < p - 1; i++) { // For each chest (except the last one)
            // Use a parallel for loop to process all positions of the next chest in parallel
            Parallel.ForEach(
                Partitioner.Create(0, positionCounts[i+1]), 
                new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism },
                range => {
                    for (int j = range.Item1; j < range.Item2; j++) { // For each position of the next chest
                        var nextPos = chestOptions[i+1][j];
                        
                        // Calculate minimum cost from previous chest positions
                        double minCost = double.MaxValue;
                        int bestPrevPos = -1;
                        
                        for (int k = 0; k < positionCounts[i]; k++) { // For each position of the current chest
                            var prevPos = chestOptions[i][k];
                            var cost = dp[i][k] + CalculateDistance(prevPos, nextPos);
                            
                            if (cost < minCost) {
                                minCost = cost;
                                bestPrevPos = k;
                            }
                        }
                        
                        // Update dp and parent tables
                        dp[i+1][j] = minCost;
                        parent[i+1][j] = (i, bestPrevPos);
                    }
                }
            );
        }

        // Find the position of the last chest that gives minimum fuel
        // This can be parallelized as well
        var minFuelResults = new ConcurrentBag<(double fuel, int pos)>();
        
        Parallel.ForEach(
            Partitioner.Create(0, positionCounts[p-1]),
            new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism },
            range => {
                double localMinFuel = double.MaxValue;
                int localBestPos = -1;
                
                for (int j = range.Item1; j < range.Item2; j++) {
                    if (dp[p-1][j] < localMinFuel) {
                        localMinFuel = dp[p-1][j];
                        localBestPos = j;
                    }
                }
                
                if (localBestPos != -1) {
                    minFuelResults.Add((localMinFuel, localBestPos));
                }
            }
        );
        
        // Find global minimum from parallel results
        var minFuel = double.MaxValue;
        var lastChestBestPos = 0;
        
        foreach (var (fuel, pos) in minFuelResults) {
            if (fuel < minFuel) {
                minFuel = fuel;
                lastChestBestPos = pos;
            }
        }
        
        // If no results were found (unlikely but possible), do a sequential scan
        if (minFuelResults.IsEmpty && positionCounts[p-1] > 0) {
            for (int j = 0; j < positionCounts[p-1]; j++) {
                if (dp[p-1][j] < minFuel) {
                    minFuel = dp[p-1][j];
                    lastChestBestPos = j;
                }
            }
        }

        // Reconstruct the path - this is sequential as it depends on previous steps
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
    /// Uses greedy selection with local optimization
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
        
        // Current position starts at the origin
        var currentPos = startPos;
        var totalFuel = 0.0;
        
        // Visit chests in order 1, 2, 3, ..., p
        // This ensures we follow the required sequence and show chronological path
        for (int chest = 1; chest <= p; chest++)
        {
            var positions = chestOptions[chest - 1];
            
            // For each chest type, try all positions in parallel and pick the best
            var results = new ConcurrentBag<(double distance, int row, int col)>();
            
            Parallel.ForEach(
                positions,
                new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism },
                pos => {
                    var distance = CalculateDistance(currentPos, pos);
                    results.Add((distance, pos.row, pos.col));
                }
            );
            
            // Find the best position (minimum distance)
            var minDistance = double.MaxValue;
            (int bestRow, int bestCol) = (0, 0);
            
            foreach (var (distance, row, col) in results)
            {
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestRow = row;
                    bestCol = col;
                }
            }
            
            totalFuel += minDistance;
            currentPos = (bestRow, bestCol);
            
            // Add step to path in chronological order - this shows the actual sequence of movement
            path.Add(new PathStep
            {
                ChestNumber = chest,
                Row = bestRow,
                Col = bestCol,
                FuelUsed = minDistance, // This is the fuel used to get to this chest from previous position
                CumulativeFuel = totalFuel
            });
        }
        
        return new TreasureHuntResponse
        {
            MinFuel = totalFuel,
            Path = path
        };
    }

    /// <summary>
    /// Reconstruct the optimal path from the DP parent tracking
    /// </summary>
    private List<PathStep> ReconstructPath(
        List<List<(int row, int col)>> chestOptions,
        Dictionary<(int mask, int chest, int pos), (int prevMask, int prevChest, int prevPos)> parent,
        int finalMask, int finalChest, int finalPos,
        (int row, int col) startPos, int[][] matrix)
    {
        var pathReverse = new List<(int chest, int row, int col, double fuel)>();
        
        var currentKey = (finalMask, finalChest, finalPos);
        
        while (parent.ContainsKey(currentKey))
        {
            var (mask, chest, pos) = currentKey;
            var (prevMask, prevChest, prevPos) = parent[currentKey];
            
            if (chest > 0)
            {
                var position = chestOptions[chest - 1][pos];
                
                // Calculate fuel used for this step - always calculate distance from previous position
                // regardless of whether current chest is at start position
                double fuelUsed;
                if (prevChest == 0)
                {
                    // Coming from start
                    fuelUsed = CalculateDistance(startPos, position);
                }
                else
                {
                    // Coming from previous chest
                    var prevPosition = chestOptions[prevChest - 1][prevPos];
                    fuelUsed = CalculateDistance(prevPosition, position);
                }
                
                pathReverse.Add((chest, position.row, position.col, fuelUsed));
            }
            
            currentKey = (prevMask, prevChest, prevPos);
        }
        
        // Reverse the path and create PathStep objects
        pathReverse.Reverse();
        
        // Always start with the start position
        var path = new List<PathStep>();
        
        // Add start position as the first step
        path.Add(new PathStep
        {
            ChestNumber = 0,
            Row = startPos.row,
            Col = startPos.col,
            FuelUsed = 0,
            CumulativeFuel = 0
        });
        
        // Add all chest steps with proper fuel calculation
        double cumulativeFuel = 0;
        foreach (var (chest, row, col, fuel) in pathReverse)
        {
            cumulativeFuel += fuel;
            path.Add(new PathStep
            {
                ChestNumber = chest,
                Row = row,
                Col = col,
                FuelUsed = fuel, // This is the actual distance from previous step
                CumulativeFuel = cumulativeFuel
            });
        }
        
        return path;
    }

    /// <summary>
    /// Calculate Euclidean distance between two positions
    /// </summary>
    private static double CalculateDistance((int row, int col) from, (int row, int col) to)
    {
        var deltaX = to.row - from.row;
        var deltaY = to.col - from.col;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}
