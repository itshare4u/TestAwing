using System.ComponentModel.DataAnnotations;

namespace TestAwing.Models;

public enum SolveStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4
}

public class TreasureHuntRequest
{
    [Required]
    [Range(1, 500)]
    public int N { get; set; }
    
    [Required]
    [Range(1, 500)]
    public int M { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int P { get; set; }
    
    [Required]
    public int[][] Matrix { get; set; } = [];
}

public class TreasureHuntResult
{
    public int Id { get; set; }
    public int N { get; set; }
    public int M { get; set; }
    public int P { get; set; }
    public string MatrixJson { get; set; } = string.Empty;
    public string PathJson { get; set; } = string.Empty; // Store the path as JSON
    public double MinFuel { get; set; }
    public DateTime CreatedAt { get; set; }
    public SolveStatus Status { get; set; } = SolveStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PathStep
{
    public int ChestNumber { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public double FuelUsed { get; set; }
    public double CumulativeFuel { get; set; }
}

public class TreasureHuntResponse
{
    public double MinFuel { get; set; }
    public int Id { get; set; }
    public List<PathStep> Path { get; set; } = [];
}

public class TreasureHuntResultWithPath
{
    public int Id { get; set; }
    public int N { get; set; }
    public int M { get; set; }
    public int P { get; set; }
    public int[][] Matrix { get; set; } = [];
    public List<PathStep> Path { get; set; } = [];
    public double MinFuel { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaginationRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; set; } = 8;
}

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class AsyncSolveRequest
{
    [Required]
    public TreasureHuntRequest TreasureHuntRequest { get; set; } = new();
}

public class AsyncSolveResponse
{
    public int SolveId { get; set; }
    public SolveStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SolveStatusResponse
{
    public int SolveId { get; set; }
    public SolveStatus Status { get; set; }
    public TreasureHuntResponse? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
