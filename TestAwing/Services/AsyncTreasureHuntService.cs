using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;

namespace TestAwing.Services;

/// <summary>
/// Service for handling async treasure hunt solving with cancellation support
/// </summary>
public class AsyncTreasureHuntService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _activeSolves;
    private readonly ILogger<AsyncTreasureHuntService> _logger;

    public AsyncTreasureHuntService(
        IServiceScopeFactory scopeFactory,
        ILogger<AsyncTreasureHuntService> logger)
    {
        _scopeFactory = scopeFactory;
        _activeSolves = new ConcurrentDictionary<int, CancellationTokenSource>();
        _logger = logger;
    }

    /// <summary>
    /// Starts an async solve operation and returns immediately with a solve ID
    /// </summary>
    public async Task<AsyncSolveResponse> StartSolveAsync(TreasureHuntRequest request)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
            
            // Create initial result record with Pending status
            var dbResult = new TreasureHuntResult
            {
                N = request.N,
                M = request.M,
                P = request.P,
                MatrixJson = JsonSerializer.Serialize(request.Matrix),
                PathJson = "[]", // Empty until solve completes
                MinFuel = 0,
                CreatedAt = DateTime.UtcNow,
                Status = SolveStatus.Pending
            };

            context.TreasureHuntResults.Add(dbResult);
            await context.SaveChangesAsync();

            // Create cancellation token for this solve
            var cts = new CancellationTokenSource();
            _activeSolves.TryAdd(dbResult.Id, cts);

            // Start the solve operation in the background
            _ = Task.Run(async () => await ExecuteSolveAsync(dbResult.Id, request, cts.Token));

            return new AsyncSolveResponse
            {
                SolveId = dbResult.Id,
                Status = SolveStatus.Pending,
                Message = "Solve operation started successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start async solve operation");
            throw;
        }
    }

    /// <summary>
    /// Gets the current status of a solve operation
    /// </summary>
    public async Task<SolveStatusResponse?> GetSolveStatusAsync(int solveId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
        
        var result = await context.TreasureHuntResults.FindAsync(solveId);
        if (result == null)
            return null;

        var response = new SolveStatusResponse
        {
            SolveId = result.Id,
            Status = result.Status,
            ErrorMessage = result.ErrorMessage,
            CreatedAt = result.CreatedAt,
            StartedAt = result.StartedAt,
            CompletedAt = result.CompletedAt
        };

        // If completed successfully, include the result
        if (result.Status == SolveStatus.Completed)
        {
            var path = JsonSerializer.Deserialize<List<PathStep>>(result.PathJson) ?? new List<PathStep>();
            response.Result = new TreasureHuntResponse
            {
                MinFuel = result.MinFuel,
                Id = result.Id,
                Path = path
            };
        }

        return response;
    }

    /// <summary>
    /// Cancels a solve operation
    /// </summary>
    public async Task<bool> CancelSolveAsync(int solveId)
    {
        try
        {
            // Cancel the operation if it's still active
            if (_activeSolves.TryRemove(solveId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            // Update database status
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
            
            var result = await context.TreasureHuntResults.FindAsync(solveId);
            if (result != null && (result.Status == SolveStatus.Pending || result.Status == SolveStatus.InProgress))
            {
                result.Status = SolveStatus.Cancelled;
                result.CompletedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel solve operation {SolveId}", solveId);
            return false;
        }
    }

    /// <summary>
    /// Executes the actual solve operation in the background
    /// </summary>
    private async Task ExecuteSolveAsync(int solveId, TreasureHuntRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Update status to InProgress
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
                var result = await context.TreasureHuntResults.FindAsync(solveId);
                if (result == null) return;

                result.Status = SolveStatus.InProgress;
                result.StartedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            // Check for cancellation before starting heavy computation
            cancellationToken.ThrowIfCancellationRequested();

            // Perform the actual solve using the parallel service with cancellation support
            var solveResult = await SolveTreasureHuntWithCancellation(request, cancellationToken);

            // Check for cancellation before saving results
            cancellationToken.ThrowIfCancellationRequested();

            // Update result with solution
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
                var result = await context.TreasureHuntResults.FindAsync(solveId);
                if (result != null)
                {
                    result.PathJson = JsonSerializer.Serialize(solveResult.Path);
                    result.MinFuel = solveResult.MinFuel;
                    result.Status = SolveStatus.Completed;
                    result.CompletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }

            _logger.LogInformation("Solve operation {SolveId} completed successfully", solveId);
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation
            await UpdateSolveStatus(solveId, SolveStatus.Cancelled, "Operation was cancelled");
            _logger.LogInformation("Solve operation {SolveId} was cancelled", solveId);
        }
        catch (Exception ex)
        {
            // Handle other errors
            await UpdateSolveStatus(solveId, SolveStatus.Failed, ex.Message);
            _logger.LogError(ex, "Solve operation {SolveId} failed", solveId);
        }
        finally
        {
            // Clean up the cancellation token
            _activeSolves.TryRemove(solveId, out var cts);
            cts?.Dispose();
        }
    }

    /// <summary>
    /// Updates the solve status in the database
    /// </summary>
    private async Task UpdateSolveStatus(int solveId, SolveStatus status, string? errorMessage = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
            
            var result = await context.TreasureHuntResults.FindAsync(solveId);
            if (result != null)
            {
                result.Status = status;
                result.CompletedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(errorMessage))
                    result.ErrorMessage = errorMessage;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update solve status for {SolveId}", solveId);
        }
    }

    /// <summary>
    /// Solve treasure hunt with cancellation token support
    /// Uses ParallelTreasureHuntService internally for consistency
    /// </summary>
    private async Task<TreasureHuntResponse> SolveTreasureHuntWithCancellation(TreasureHuntRequest request, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            // Create a temporary in-memory context for the ParallelTreasureHuntService
            using var scope = _scopeFactory.CreateScope();
            var tempContext = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
            var parallelService = new ParallelTreasureHuntService(tempContext);
            
            // Use the same algorithm as the parallel service but with cancellation checks
            var result = CalculateOptimalPathWithCancellation(request, cancellationToken, parallelService);
            
            return new TreasureHuntResponse
            {
                MinFuel = result.MinFuel,
                Id = 0, // Will be set by caller
                Path = result.Path
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Calculate optimal path using ParallelTreasureHuntService with cancellation support
    /// </summary>
    private (double MinFuel, List<PathStep> Path) CalculateOptimalPathWithCancellation(TreasureHuntRequest request, CancellationToken cancellationToken, ParallelTreasureHuntService parallelService)
    {
        var n = request.N;
        var m = request.M;
        var p = request.P;
        var matrix = request.Matrix;

        _logger.LogInformation("AsyncTreasureHuntService: Starting calculation with n={N}, m={M}, p={P}", n, m, p);

        // Group positions by chest number (same logic as ParallelTreasureHuntService)
        var chestPositions = new Dictionary<int, List<(int row, int col)>>();
        
        // Initialize the dictionary for all chest numbers
        for (int chest = 1; chest <= p; chest++) {
            chestPositions[chest] = new List<(int row, int col)>();
        }
        
        // Scan the matrix for chest positions
        for (int i = 0; i < n; i++) {
            for (int j = 0; j < m; j++) {
                var chestNum = matrix[i][j];
                _logger.LogInformation("AsyncTreasureHuntService: Matrix[{I}][{J}] = {ChestNum}", i, j, chestNum);
                // Only add positions that actually contain chests (ignore 0 values)
                if (chestNum > 0) {
                    chestPositions[chestNum].Add((i, j));
                    _logger.LogInformation("AsyncTreasureHuntService: Added chest {ChestNum} at position ({I}, {J})", chestNum, i, j);
                }
            }
        }

        _logger.LogInformation("AsyncTreasureHuntService: Found {Count} chest types", chestPositions.Count);

        // Check for cancellation periodically during computation
        cancellationToken.ThrowIfCancellationRequested();

        // Validate all chests from 1 to p exist
        for (int chest = 1; chest <= p; chest++) {
            if (!chestPositions.ContainsKey(chest) || chestPositions[chest].Count == 0)
            {
                _logger.LogError("AsyncTreasureHuntService: Chest {Chest} not found in matrix", chest);
                throw new ArgumentException($"Chest {chest} not found in matrix");
            }
        }

        _logger.LogInformation("AsyncTreasureHuntService: All chests validated successfully");

        // Create list format for DP algorithm (same as ParallelTreasureHuntService)
        var chestOptions = new List<List<(int row, int col)>>();
        for (int chest = 1; chest <= p; chest++) {
            chestOptions.Add(chestPositions[chest]);
        }

        // Use the parallel DP algorithm with cancellation support
        return CalculateOptimalPathDPWithCancellation(chestOptions, p, matrix, cancellationToken);
    }

    /// <summary>
    /// Parallel DP algorithm with cancellation support (based on ParallelTreasureHuntService)
    /// </summary>
    private (double MinFuel, List<PathStep> Path) CalculateOptimalPathDPWithCancellation(List<List<(int row, int col)>> chestOptions, int p, int[][] matrix, CancellationToken cancellationToken)
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
        
        // Fill the DP table using the recurrence relation with cancellation checks
        for (int i = 0; i < p - 1; i++) { // For each chest (except the last one)
            cancellationToken.ThrowIfCancellationRequested();
            
            int nextChestPositions = positionCounts[i+1];
            int currentChestPositions = positionCounts[i];
            
            // Parallelize the DP computation for each next chest position
            // Use default parallelism (Environment.ProcessorCount)
            Parallel.For(0, nextChestPositions, new ParallelOptions { 
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken 
            }, j => {
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
        
        // Reconstruct the path (same as ParallelTreasureHuntService)
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
        
        _logger.LogInformation("AsyncTreasureHuntService: Completed calculation with total fuel {TotalFuel} and {PathCount} steps", minFuel, path.Count);
        return (minFuel, path);
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

    /// <summary>
    /// Gets all active solve operations
    /// </summary>
    public int GetActiveSolveCount()
    {
        return _activeSolves.Count;
    }

    /// <summary>
    /// Cleanup method to cancel all active solves (useful for shutdown)
    /// </summary>
    public void CancelAllActiveSolves()
    {
        foreach (var kvp in _activeSolves)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }
        _activeSolves.Clear();
    }
}
