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

        // Group positions by chest number (use sequential processing for consistency)
        var chestPositions = new Dictionary<int, List<(int row, int col)>>();
        
        // Initialize the dictionary for all chest numbers
        for (int chest = 1; chest <= p; chest++) {
            chestPositions[chest] = new List<(int row, int col)>();
        }
        
        // Scan the matrix for chest positions
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < m; j++) {
                var chestNum = matrix[i][j];
                // Only add positions that actually contain chests (ignore 0 values)
                if (chestNum > 0) {
                    chestPositions[chestNum].Add((i, j));
                }
            }
        }

        // Validate all chests from 1 to p exist
        for (int chest = 1; chest <= p; chest++) {
            if (!chestPositions.ContainsKey(chest) || chestPositions[chest].Count == 0)
                throw new ArgumentException($"Chest {chest} not found in matrix");
        }

        // Create list format for DP algorithm
        var chestOptions = new List<List<(int row, int col)>>();
        for (int chest = 1; chest <= p; chest++) {
            chestOptions.Add(chestPositions[chest]);
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
    /// Only the DP computation is parallelized for consistent results
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
        
        // Initialize first chest distances sequentially for consistency
        for (int j = 0; j < positionCounts[0]; j++) {
            dp[0][j] = CalculateDistance(startPos, chestOptions[0][j]);
            parent[0][j] = (-1, -1); // Coming from start
        }

        // Initialize other chests with infinity
        for (int i = 1; i < p; i++) {
            for (int j = 0; j < positionCounts[i]; j++) {
                dp[i][j] = double.MaxValue;
            }
        }
        
        // Fill the DP table using the recurrence relation
        // This is the most compute-intensive part, so we parallelize only this part
        for (int i = 0; i < p - 1; i++) { // For each chest (except the last one)
            int nextChestPositions = positionCounts[i+1];
            int currentChestPositions = positionCounts[i];
            
            // Parallelize the DP computation for each next chest position
            // This is the key computation that benefits from parallelization
            Parallel.For(0, nextChestPositions, new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism }, j => {
                var nextPos = chestOptions[i+1][j];
                
                // Local variables for thread-safe operation
                double minCost = double.MaxValue;
                int bestPrevPos = -1;
                
                // Find the minimum cost from all previous positions
                for (int k = 0; k < currentChestPositions; k++) {
                    var prevPos = chestOptions[i][k];
                    var cost = dp[i][k] + CalculateDistance(prevPos, nextPos);
                    
                    if (cost < minCost) {
                        minCost = cost;
                        bestPrevPos = k;
                    }
                }
                
                // Update DP table (each thread writes to a different position, so this is thread-safe)
                dp[i+1][j] = minCost;
                parent[i+1][j] = (i, bestPrevPos);
            });
        }

        // Find the minimum fuel position sequentially for consistency
        var lastChestIndex = p - 1;
        var lastChestPositionCount = positionCounts[lastChestIndex];
        
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
    /// Heuristic approach for larger p values with focused parallelization
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
        
        // For each chest, find the closest position
        for (int chest = 1; chest <= p; chest++)
        {
            var positions = chestOptions[chest - 1];
            
            // Parallelize the distance calculation for each position
            // This is the most compute-intensive part of the heuristic
            var distances = new double[positions.Count];
            
            Parallel.For(0, positions.Count, j => {
                distances[j] = CalculateDistance(currentPos, positions[j]);
            });
            
            // Find the minimum distance sequentially
            var minDistance = double.MaxValue;
            var bestIndex = 0;
            
            for (int j = 0; j < positions.Count; j++)
            {
                if (distances[j] < minDistance)
                {
                    minDistance = distances[j];
                    bestIndex = j;
                }
            }
            
            // Get the best position
            var (bestRow, bestCol) = positions[bestIndex];
            
            // Update total fuel and current position
            totalFuel += minDistance;
            currentPos = (bestRow, bestCol);
            
            // Add step to path
            path.Add(new PathStep
            {
                ChestNumber = chest,
                Row = bestRow,
                Col = bestCol,
                FuelUsed = minDistance,
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
