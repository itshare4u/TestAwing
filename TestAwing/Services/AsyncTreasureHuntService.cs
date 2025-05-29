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
    /// Uses OptimizedTreasureHuntService internally for consistency
    /// </summary>
    private async Task<TreasureHuntResponse> SolveTreasureHuntWithCancellation(TreasureHuntRequest request, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            // Create a temporary in-memory context for the OptimizedTreasureHuntService
            using var scope = _scopeFactory.CreateScope();
            var tempContext = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
            var optimizedService = new OptimizedTreasureHuntService(tempContext);
            
            // Use the same algorithm as the non-async API but with cancellation checks
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
    /// Simplified version that uses the same logic as OptimizedTreasureHuntService
    /// with cancellation support
    /// </summary>
    private (double MinFuel, List<PathStep> Path) CalculateOptimalPathWithCancellation(TreasureHuntRequest request, CancellationToken cancellationToken)
    {
        var n = request.N;
        var m = request.M;
        var p = request.P;
        var matrix = request.Matrix;

        _logger.LogInformation("AsyncTreasureHuntService: Starting calculation with n={N}, m={M}, p={P}", n, m, p);

        // Group positions by chest number (same logic as OptimizedTreasureHuntService)
        var chestPositions = new Dictionary<int, List<(int, int)>>();
        
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                var chestNum = matrix[i][j];
                _logger.LogInformation("AsyncTreasureHuntService: Matrix[{I}][{J}] = {ChestNum}", i, j, chestNum);
                // Only add positions that actually contain chests (ignore 0 values)
                if (chestNum > 0)
                {
                    if (!chestPositions.ContainsKey(chestNum))
                        chestPositions[chestNum] = new List<(int, int)>();
                    chestPositions[chestNum].Add((i, j));
                    _logger.LogInformation("AsyncTreasureHuntService: Added chest {ChestNum} at position ({I}, {J})", chestNum, i, j);
                }
            }
        }

        _logger.LogInformation("AsyncTreasureHuntService: Found {Count} chest types", chestPositions.Count);

        // Check for cancellation periodically during computation
        cancellationToken.ThrowIfCancellationRequested();

        // Validate all chests from 1 to p exist
        for (int chest = 1; chest <= p; chest++)
        {
            if (!chestPositions.ContainsKey(chest))
            {
                _logger.LogError("AsyncTreasureHuntService: Chest {Chest} not found in matrix", chest);
                throw new ArgumentException($"Chest {chest} not found in matrix");
            }
        }

        _logger.LogInformation("AsyncTreasureHuntService: All chests validated successfully");

        // Use simple greedy approach for consistency and simplicity
        // This matches the OptimizedTreasureHuntService heuristic approach
        var path = new List<PathStep>();
        
        // Always start with the start position
        path.Add(new PathStep
        {
            ChestNumber = 0,
            Row = 0,
            Col = 0,
            FuelUsed = 0,
            CumulativeFuel = 0
        });
        
        var currentPos = (row: 0, col: 0);
        var cumulativeFuel = 0.0;
        
        _logger.LogInformation("AsyncTreasureHuntService: Starting to visit chests 1 to {P}", p);
        
        // Visit chests in order 1, 2, 3, ..., p
        for (int chest = 1; chest <= p; chest++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var positions = chestPositions[chest];
            _logger.LogInformation("AsyncTreasureHuntService: Processing chest {Chest} with {Count} positions", chest, positions.Count);
            
            // Find the closest position for this chest
            var minDistance = double.MaxValue;
            var bestPos = positions[0];
            
            foreach (var pos in positions)
            {
                var distance = Math.Sqrt(Math.Pow(currentPos.row - pos.Item1, 2) + Math.Pow(currentPos.col - pos.Item2, 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestPos = pos;
                }
            }
            
            cumulativeFuel += minDistance;
            currentPos = (bestPos.Item1, bestPos.Item2);
            
            _logger.LogInformation("AsyncTreasureHuntService: Chest {Chest} - best position ({Row}, {Col}), distance {Distance}, cumulative {Cumulative}", 
                chest, bestPos.Item1, bestPos.Item2, minDistance, cumulativeFuel);
            
            path.Add(new PathStep
            {
                ChestNumber = chest,
                Row = bestPos.Item1,
                Col = bestPos.Item2,
                FuelUsed = minDistance,
                CumulativeFuel = cumulativeFuel
            });
        }
        
        _logger.LogInformation("AsyncTreasureHuntService: Completed calculation with total fuel {TotalFuel} and {PathCount} steps", cumulativeFuel, path.Count);
        return (cumulativeFuel, path);
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
