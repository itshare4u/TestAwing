using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;

namespace TestAwing.Services;

public class TreasureHuntService(TreasureHuntContext context)
{
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

        // Calculate minimum fuel required
        var minFuel = CalculateMinimumFuel(request);

        // Save to database
        var result = new TreasureHuntResult
        {
            N = request.N,
            M = request.M,
            P = request.P,
            MatrixJson = JsonSerializer.Serialize(request.Matrix),
            MinFuel = minFuel,
            CreatedAt = DateTime.UtcNow
        };

        context.TreasureHuntResults.Add(result);
        await context.SaveChangesAsync();

        return new TreasureHuntResponse
        {
            MinFuel = minFuel,
            Id = result.Id
        };
    }

    public async Task<List<TreasureHuntResult>> GetAllTreasureHunts()
    {
        return await context.TreasureHuntResults
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    private double CalculateMinimumFuel(TreasureHuntRequest request)
    {
        var n = request.N;
        var m = request.M;
        var p = request.P;
        var matrix = request.Matrix;

        // Group positions by chest number
        var chestPositions = new Dictionary<int, List<(int row, int col)>>();
        
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < m; j++)
            {
                var chestNum = matrix[i][j];
                if (!chestPositions.ContainsKey(chestNum))
                    chestPositions[chestNum] = [];
                chestPositions[chestNum].Add((i, j));
            }
        }

        double totalFuel = 0;
        var currentRow = 0; // Starting at (1,1) which is (0,0) in 0-indexed
        var currentCol = 0;

        // Visit chests from 1 to p in order, finding optimal path within each group
        for (var chest = 1; chest <= p; chest++)
        {
            if (!chestPositions.TryGetValue(chest, out var positions))
                throw new ArgumentException($"Chest {chest} not found in matrix");

            // Find the closest chest of this number
            var minDistance = double.MaxValue;
            var (bestRow, bestCol) = (0, 0);
            
            foreach (var (targetRow, targetCol) in positions)
            {
                var deltaX = targetRow - currentRow;
                var deltaY = targetCol - currentCol;
                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (!(distance < minDistance)) continue;
                minDistance = distance;
                bestRow = targetRow;
                bestCol = targetCol;
            }
            
            totalFuel += minDistance;
            
            // Update current position
            currentRow = bestRow;
            currentCol = bestCol;
        }

        return totalFuel;
    }
}
