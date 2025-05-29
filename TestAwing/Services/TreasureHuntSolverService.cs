using System.Collections.Concurrent;
using System.Text.Json;
using TestAwing.Models;

namespace TestAwing.Services;

public class TreasureHuntSolverService(
    IServiceScopeFactory scopeFactory,
    ILogger<TreasureHuntSolverService> logger)
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _activeSolves = new();

    public async Task<AsyncSolveResponse> StartSolveAsync(TreasureHuntRequest request)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
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
            _ = Task.Run(async () => await ExecuteSolveAsync(dbResult.Id, request, cts.Token), cts.Token);

            return new AsyncSolveResponse
            {
                SolveId = dbResult.Id,
                Status = SolveStatus.Pending,
                Message = "Solve operation started successfully"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start async solve operation");
            throw;
        }
    }


    public async Task<SolveStatusResponse?> GetSolveStatusAsync(int solveId)
    {
        using var scope = scopeFactory.CreateScope();
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
        if (result.Status != SolveStatus.Completed) return response;
        var path = JsonSerializer.Deserialize<List<PathStep>>(result.PathJson) ?? [];
        response.Result = new TreasureHuntResponse
        {
            MinFuel = result.MinFuel,
            Id = result.Id,
            Path = path
        };

        return response;
    }


    public async Task<bool> CancelSolveAsync(int solveId)
    {
        try
        {
            logger.LogInformation("Attempting to cancel solve operation {SolveId}", solveId);

            // First check if the operation exists and is cancelable
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();

            var result = await context.TreasureHuntResults.FindAsync(solveId);
            if (result == null)
            {
                logger.LogWarning("Solve operation {SolveId} not found", solveId);
                return false;
            }

            if (result.Status != SolveStatus.Pending && result.Status != SolveStatus.InProgress)
            {
                logger.LogInformation("Solve operation {SolveId} cannot be cancelled (status: {Status})", solveId,
                    result.Status);
                return false;
            }

            // Cancel the operation if it's still active
            var tokenCancelled = false;
            if (_activeSolves.TryGetValue(solveId, out var cts))
            {
                await cts.CancelAsync();
                tokenCancelled = true;
                logger.LogInformation("Cancellation token triggered for solve operation {SolveId}", solveId);
            }

            // Update database status immediately
            result.Status = SolveStatus.Cancelled;
            result.CompletedAt = DateTime.UtcNow;
            result.ErrorMessage = "Operation was cancelled by user";
            await context.SaveChangesAsync();

            logger.LogInformation("Solve operation {SolveId} marked as cancelled in database", solveId);

            // Clean up the token after updating DB
            if (!tokenCancelled) return true;
            _activeSolves.TryRemove(solveId, out _);
            cts?.Dispose();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel solve operation {SolveId}", solveId);
            return false;
        }
    }


    private async Task ExecuteSolveAsync(int solveId, TreasureHuntRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Update status to InProgress
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
                var result =
                    await context.TreasureHuntResults.FindAsync([solveId], cancellationToken: cancellationToken);
                if (result == null) return;

                // Check if already cancelled before starting
                if (result.Status == SolveStatus.Cancelled)
                {
                    logger.LogInformation("Solve operation {SolveId} was already cancelled before starting", solveId);
                    return;
                }

                result.Status = SolveStatus.InProgress;
                result.StartedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Solve operation {SolveId} status updated to InProgress", solveId);
            }

            // Check for cancellation before starting heavy computation
            cancellationToken.ThrowIfCancellationRequested();

            // Perform the actual solve using the parallel service with cancellation support
            var solveResult = await SolveHuntAsync(request, cancellationToken);

            // Check for cancellation before saving results
            cancellationToken.ThrowIfCancellationRequested();

            // Update result with solution - use a separate scope and check status first
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
                var result =
                    await context.TreasureHuntResults.FindAsync([solveId], cancellationToken: cancellationToken);
                if (result is { Status: SolveStatus.InProgress })
                {
                    result.PathJson = JsonSerializer.Serialize(solveResult.Path);
                    result.MinFuel = solveResult.MinFuel;
                    result.Status = SolveStatus.Completed;
                    result.CompletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Solve operation {SolveId} completed successfully", solveId);
                }
                else
                {
                    logger.LogInformation(
                        "Solve operation {SolveId} was cancelled during execution (status: {Status})", solveId,
                        result?.Status);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation - only update if not already updated by CancelSolveAsync
            logger.LogInformation("Solve operation {SolveId} was cancelled via OperationCanceledException", solveId);

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
            var result = await context.TreasureHuntResults.FindAsync([solveId], cancellationToken: cancellationToken);
            if (result != null && result.Status != SolveStatus.Cancelled)
            {
                result.Status = SolveStatus.Cancelled;
                result.CompletedAt = DateTime.UtcNow;
                result.ErrorMessage = "Operation was cancelled";
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation(
                    "Solve operation {SolveId} status updated to Cancelled via exception handler", solveId);
            }
        }
        catch (Exception ex)
        {
            // Handle other errors
            await UpdateSolveStatus(solveId, SolveStatus.Failed, ex.Message);
            logger.LogError(ex, "Solve operation {SolveId} failed", solveId);
        }
        finally
        {
            // Clean up the cancellation token
            if (_activeSolves.TryRemove(solveId, out var cts))
            {
                cts?.Dispose();
                logger.LogInformation("Cleaned up cancellation token for solve operation {SolveId}", solveId);
            }
        }
    }


    private async Task UpdateSolveStatus(int solveId, SolveStatus status, string? errorMessage = null)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
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
            logger.LogError(ex, "Failed to update solve status for {SolveId}", solveId);
        }
    }


    private async Task<TreasureHuntResponse> SolveHuntAsync(TreasureHuntRequest request,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            // Create a temporary in-memory context for the ParallelTreasureHuntService
            using var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();

            // Use the same algorithm as the parallel service but with cancellation checks
            var result = SolvePathCancelable(request, cancellationToken);

            return new TreasureHuntResponse
            {
                MinFuel = result.MinFuel,
                Id = 0, // Will be set by caller
                Path = result.Path
            };
        }, cancellationToken);
    }


    private (double MinFuel, List<PathStep> Path) SolvePathCancelable(TreasureHuntRequest request,
        CancellationToken cancellationToken)
    {
        var n = request.N;
        var m = request.M;
        var p = request.P;
        var matrix = request.Matrix;

        logger.LogInformation("AsyncTreasureHuntService: Starting calculation with n={N}, m={M}, p={P}", n, m, p);

        // Group positions by chest number (same logic as ParallelTreasureHuntService)
        var chestPositions = new Dictionary<int, List<(int row, int col)>>();

        // Initialize the dictionary for all chest numbers
        for (var chest = 1; chest <= p; chest++)
        {
            chestPositions[chest] = [];
        }

        // Scan the matrix for chest positions
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < m; j++)
            {
                var chestNum = matrix[i][j];
                logger.LogInformation("AsyncTreasureHuntService: Matrix[{I}][{J}] = {ChestNum}", i, j, chestNum);
                // Only add positions that actually contain chests (ignore 0 values)
                if (chestNum <= 0) continue;
                chestPositions[chestNum].Add((i, j));
                logger.LogInformation("AsyncTreasureHuntService: Added chest {ChestNum} at position ({I}, {J})",
                    chestNum, i, j);
            }
        }

        logger.LogInformation("AsyncTreasureHuntService: Found {Count} chest types", chestPositions.Count);

        // Check for cancellation periodically during computation
        cancellationToken.ThrowIfCancellationRequested();

        // Validate all chests from 1 to p exist
        for (var chest = 1; chest <= p; chest++)
        {
            if (chestPositions.TryGetValue(chest, out var value) && value.Count != 0) continue;
            logger.LogError("AsyncTreasureHuntService: Chest {Chest} not found in matrix", chest);
            throw new ArgumentException($"Chest {chest} not found in matrix");
        }

        logger.LogInformation("AsyncTreasureHuntService: All chests validated successfully");

        // Create list format for DP algorithm (same as ParallelTreasureHuntService)
        var chestOptions = new List<List<(int row, int col)>>();
        for (var chest = 1; chest <= p; chest++)
        {
            chestOptions.Add(chestPositions[chest]);
        }

        // Use the parallel DP algorithm with cancellation support
        return SolvePathDpCancelable(chestOptions, p, cancellationToken);
    }


    /// <summary>
    /// Parallel DP algorithm with cancellation support (based on ParallelTreasureHuntService)
    /// </summary>
    private (double MinFuel, List<PathStep> Path) SolvePathDpCancelable(
        List<List<(int row, int col)>> chestOptions, int p, CancellationToken cancellationToken)
    {
        var startPos = (row: 0, col: 0);

        // Get all possible positions for each chest type
        var positionCounts = new int[p];
        for (var i = 0; i < p; i++)
        {
            positionCounts[i] = chestOptions[i].Count;
        }

        // Initialize DP table: dp[i][j] = minimum fuel to reach chest i+1 at position j
        var dp = new double[p][];
        var parent = new (int chest, int pos)[p][];

        for (var i = 0; i < p; i++)
        {
            dp[i] = new double[positionCounts[i]];
            parent[i] = new (int chest, int pos)[positionCounts[i]];
        }

        // Initialize first chest distances sequentially for consistency
        for (var j = 0; j < positionCounts[0]; j++)
        {
            dp[0][j] = CalculateDistance(startPos, chestOptions[0][j]);
            parent[0][j] = (-1, -1); // Coming from start
        }

        // Initialize other chests with infinity
        for (var i = 1; i < p; i++)
        {
            for (var j = 0; j < positionCounts[i]; j++)
            {
                dp[i][j] = double.MaxValue;
            }
        }

        // Fill the DP table using the recurrence relation with cancellation checks
        for (var i = 0; i < p - 1; i++)
        {
            // For each chest (except the last one)
            cancellationToken.ThrowIfCancellationRequested();

            var nextChestPositions = positionCounts[i + 1];
            var currentChestPositions = positionCounts[i];

            // Parallelize the DP computation for each next chest position
            // Use default parallelism (Environment.ProcessorCount)
            Parallel.For(0, nextChestPositions, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            }, j =>
            {
                var nextPos = chestOptions[i + 1][j];

                // Local variables for thread-safe operation
                var minCost = double.MaxValue;
                var bestPrevPos = -1;

                // Find the minimum cost from all previous positions
                for (var k = 0; k < currentChestPositions; k++)
                {
                    var prevPos = chestOptions[i][k];
                    var cost = dp[i][k] + CalculateDistance(prevPos, nextPos);

                    if (!(cost < minCost)) continue;
                    minCost = cost;
                    bestPrevPos = k;
                }

                // Update DP table (each thread writes to a different position, so this is thread-safe)
                dp[i + 1][j] = minCost;
                parent[i + 1][j] = (i, bestPrevPos);
            });
        }

        // Find the minimum fuel position sequentially for consistency
        var lastChestIndex = p - 1;
        var lastChestPositionCount = positionCounts[lastChestIndex];

        var minFuel = double.MaxValue;
        var lastChestBestPos = 0;

        for (var j = 0; j < lastChestPositionCount; j++)
        {
            if (!(dp[lastChestIndex][j] < minFuel)) continue;
            minFuel = dp[lastChestIndex][j];
            lastChestBestPos = j;
        }

        // Reconstruct the path (same as ParallelTreasureHuntService)
        var path = new List<PathStep>
        {
            // Add start position
            new PathStep
            {
                ChestNumber = 0,
                Row = startPos.row,
                Col = startPos.col,
                FuelUsed = 0,
                CumulativeFuel = 0
            }
        };

        // Backtrack to construct the path
        var pathPositions = new List<(int chest, int row, int col)>();
        var currentChestIdx = lastChestIndex;
        var currentPosIdx = lastChestBestPos;

        while (currentChestIdx >= 0)
        {
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

        foreach (var (chest, row, col) in pathPositions)
        {
            var targetPosition = (row, col);
            var fuelUsed = CalculateDistance(currentPosition, targetPosition);
            cumulativeFuel += fuelUsed;

            path.Add(new PathStep
            {
                ChestNumber = chest,
                Row = row,
                Col = col,
                FuelUsed = fuelUsed,
                CumulativeFuel = cumulativeFuel
            });

            currentPosition = targetPosition;
        }

        logger.LogInformation(
            "AsyncTreasureHuntService: Completed calculation with total fuel {TotalFuel} and {PathCount} steps",
            minFuel, path.Count);
        return (minFuel, path);
    }


    private static double CalculateDistance((int row, int col) from, (int row, int col) to)
    {
        var deltaX = to.row - from.row;
        var deltaY = to.col - from.col;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}
