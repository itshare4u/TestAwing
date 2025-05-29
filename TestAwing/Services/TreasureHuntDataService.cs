using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TestAwing.Models;

namespace TestAwing.Services;

public class TreasureHuntDataService(TreasureHuntContext context)
{
    public async Task<PaginatedResponse<TreasureHuntResult>> GetPaginatedTreasureHunts(int page = 1, int pageSize = 8)
    {
        // Validate pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Limit max page size to 100

        var totalCount = await context.TreasureHuntResults.CountAsync();

        var data = await context.TreasureHuntResults
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
        for (var i = 0; i < n; i++)
        {
            matrix[i] = new int[m];
        }

        // Create a list of all positions
        var positions = new List<(int row, int col)>();
        for (var row = 0; row < n; row++)
        {
            for (var col = 0; col < m; col++)
            {
                positions.Add((row, col));
            }
        }

        // Shuffle positions using Fisher-Yates algorithm
        for (var i = positions.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        // Ensure each chest number from 1 to p appears at least once
        for (var chest = 1; chest <= p; chest++)
        {
            var pos = positions[chest - 1];
            matrix[pos.row][pos.col] = chest;
        }

        // Fill remaining positions with random values from 1 to (p-1)
        // This ensures the maximum chest number (p) appears exactly once
        for (var i = p; i < positions.Count; i++)
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
        var result = await context.TreasureHuntResults.FindAsync(id);
        if (result == null) return null;

        var matrix = JsonSerializer.Deserialize<int[][]>(result.MatrixJson);
        var path = !string.IsNullOrEmpty(result.PathJson)
            ? JsonSerializer.Deserialize<List<PathStep>>(result.PathJson) ?? []
            : [];

        return new TreasureHuntResultWithPath
        {
            Id = result.Id,
            N = result.N,
            M = result.M,
            P = result.P,
            Matrix = matrix ?? [],
            Path = path,
            MinFuel = result.MinFuel,
            CreatedAt = result.CreatedAt
        };
    }
}