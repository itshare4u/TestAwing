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
    /// </summary>
    private async Task<TreasureHuntResponse> SolveTreasureHuntWithCancellation(TreasureHuntRequest request, CancellationToken cancellationToken)
    {
        // This is a wrapper around the parallel service that adds cancellation support
        // We'll run the solve in a task and monitor the cancellation token
        return await Task.Run(() =>
        {
            // Use the existing parallel service calculation but add periodic cancellation checks
            var result = CalculateOptimalPathWithCancellation(request, cancellationToken);
            
            return new TreasureHuntResponse
            {
                MinFuel = result.MinFuel,
                Id = 0, // Will be set by caller
                Path = result.Path
            };
        }, cancellationToken);
    }

    /// <summary>
    /// Modified version of CalculateOptimalPath with cancellation support
    /// </summary>
    private (double MinFuel, List<PathStep> Path) CalculateOptimalPathWithCancellation(TreasureHuntRequest request, CancellationToken cancellationToken)
    {
        var n = request.N;
        var m = request.M;
        var p = request.P;
        var matrix = request.Matrix;

        // Find positions of all chests
        var chestPositions = new Dictionary<int, (int row, int col)>();
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                if (matrix[i][j] >= 1 && matrix[i][j] <= p)
                {
                    chestPositions[matrix[i][j]] = (i, j);
                }
            }
        }

        // Check for cancellation periodically during computation
        cancellationToken.ThrowIfCancellationRequested();

        // Validate that all chests 1 to p exist
        for (int i = 1; i <= p; i++)
        {
            if (!chestPositions.ContainsKey(i))
            {
                throw new ArgumentException($"Chest number {i} not found in matrix");
            }
        }

        // DP table: dp[mask][last] = minimum fuel to collect chests in mask, ending at chest 'last'
        var dp = new double[1 << p, p];
        var parent = new int[1 << p, p]; // For path reconstruction

        // Initialize DP table
        for (int mask = 0; mask < (1 << p); mask++)
        {
            for (int last = 0; last < p; last++)
            {
                dp[mask, last] = double.PositiveInfinity;
                parent[mask, last] = -1;
            }
        }

        // Base case: start at each chest
        for (int i = 0; i < p; i++)
        {
            var (row, col) = chestPositions[i + 1];
            var fuelToReach = Math.Sqrt(row * row + col * col);
            dp[1 << i, i] = fuelToReach;
        }

        // Check for cancellation before main DP loop
        cancellationToken.ThrowIfCancellationRequested();

        // Fill DP table using parallel processing with cancellation checks
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        for (int mask = 1; mask < (1 << p); mask++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentChests = new List<int>();
            for (int i = 0; i < p; i++)
            {
                if ((mask & (1 << i)) != 0)
                    currentChests.Add(i);
            }

            if (currentChests.Count == 1) continue;

            try
            {
                Parallel.ForEach(currentChests, parallelOptions, last =>
                {
                    var maskWithoutLast = mask ^ (1 << last);
                    var (lastRow, lastCol) = chestPositions[last + 1];

                    for (int prev = 0; prev < p; prev++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if ((maskWithoutLast & (1 << prev)) != 0 && dp[maskWithoutLast, prev] != double.PositiveInfinity)
                        {
                            var (prevRow, prevCol) = chestPositions[prev + 1];
                            var fuelBetween = Math.Sqrt(Math.Pow(lastRow - prevRow, 2) + Math.Pow(lastCol - prevCol, 2));
                            var totalFuel = dp[maskWithoutLast, prev] + fuelBetween;

                            lock (dp) // Thread safety for DP table updates
                            {
                                if (totalFuel < dp[mask, last])
                                {
                                    dp[mask, last] = totalFuel;
                                    parent[mask, last] = prev;
                                }
                            }
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation exception
            }
        }

        // Final check for cancellation before path reconstruction
        cancellationToken.ThrowIfCancellationRequested();

        // Find minimum fuel among all possible ending chests
        var minFuel = double.PositiveInfinity;
        var bestLast = -1;
        var fullMask = (1 << p) - 1;

        for (int i = 0; i < p; i++)
        {
            if (dp[fullMask, i] < minFuel)
            {
                minFuel = dp[fullMask, i];
                bestLast = i;
            }
        }

        if (minFuel == double.PositiveInfinity)
        {
            throw new InvalidOperationException("No solution found");
        }

        // Reconstruct path
        var path = new List<PathStep>();
        var currentMask = fullMask;
        var currentLast = bestLast;
        var cumulativeFuel = 0.0;

        while (currentLast != -1)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chestNumber = currentLast + 1;
            var (row, col) = chestPositions[chestNumber];
            
            var fuelUsed = 0.0;
            if (path.Count == 0)
            {
                // First chest - fuel from origin
                fuelUsed = Math.Sqrt(row * row + col * col);
            }
            else
            {
                // Fuel from previous chest
                var prevChest = parent[currentMask, currentLast];
                if (prevChest != -1)
                {
                    var (prevRow, prevCol) = chestPositions[prevChest + 1];
                    fuelUsed = Math.Sqrt(Math.Pow(row - prevRow, 2) + Math.Pow(col - prevCol, 2));
                }
            }

            cumulativeFuel += fuelUsed;

            path.Insert(0, new PathStep
            {
                ChestNumber = chestNumber,
                Row = row,
                Col = col,
                FuelUsed = fuelUsed,
                CumulativeFuel = cumulativeFuel
            });

            var prevLast = parent[currentMask, currentLast];
            currentMask ^= (1 << currentLast);
            currentLast = prevLast;
        }

        // Recalculate cumulative fuel correctly (from start to end)
        cumulativeFuel = 0.0;
        for (int i = 0; i < path.Count; i++)
        {
            cumulativeFuel += path[i].FuelUsed;
            path[i].CumulativeFuel = cumulativeFuel;
        }

        return (minFuel, path);
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
