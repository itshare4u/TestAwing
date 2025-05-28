using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;

namespace TestAwing.Services;

public class TreasureHuntService
{
    private readonly TreasureHuntContext _context;

    public TreasureHuntService(TreasureHuntContext context)
    {
        _context = context;
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

        // Calculate minimum fuel required and get the path
        var result = CalculateMinimumFuelWithPath(request);
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

    private TreasureHuntResponse CalculateMinimumFuelWithPath(TreasureHuntRequest request)
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
                if (!chestPositions.ContainsKey(chestNum))
                    chestPositions[chestNum] = new List<(int, int)>();
                chestPositions[chestNum].Add((i, j));
            }
        }

        double totalFuel = 0;
        int currentRow = 0; // Starting at (1,1) which is (0,0) in 0-indexed
        int currentCol = 0;
        var path = new List<PathStep>();

        // Add starting position
        path.Add(new PathStep
        {
            ChestNumber = 0,
            Row = currentRow,
            Col = currentCol,
            FuelUsed = 0,
            CumulativeFuel = 0
        });

        // Visit chests from 1 to p in order, finding optimal path within each group
        for (int chest = 1; chest <= p; chest++)
        {
            if (!chestPositions.ContainsKey(chest))
                throw new ArgumentException($"Chest {chest} not found in matrix");

            var positions = chestPositions[chest];
            
            // Find the closest chest of this number
            double minDistance = double.MaxValue;
            (int bestRow, int bestCol) = (0, 0);
            
            foreach (var (targetRow, targetCol) in positions)
            {
                var deltaX = targetRow - currentRow;
                var deltaY = targetCol - currentCol;
                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestRow = targetRow;
                    bestCol = targetCol;
                }
            }
            
            totalFuel += minDistance;
            
            // Add step to path
            path.Add(new PathStep
            {
                ChestNumber = chest,
                Row = bestRow,
                Col = bestCol,
                FuelUsed = minDistance,
                CumulativeFuel = totalFuel
            });
            
            // Update current position
            currentRow = bestRow;
            currentCol = bestCol;
        }

        return new TreasureHuntResponse
        {
            MinFuel = totalFuel,
            Path = path
        };
    }

    private double CalculateMinimumFuel(TreasureHuntRequest request)
    {
        return CalculateMinimumFuelWithPath(request).MinFuel;
    }
}
