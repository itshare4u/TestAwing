using System.ComponentModel.DataAnnotations;

namespace TestAwing.Models;

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
    public int[][] Matrix { get; set; } = Array.Empty<int[]>();
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
    public List<PathStep> Path { get; set; } = new List<PathStep>();
}

public class TreasureHuntResultWithPath
{
    public int Id { get; set; }
    public int N { get; set; }
    public int M { get; set; }
    public int P { get; set; }
    public int[][] Matrix { get; set; } = Array.Empty<int[]>();
    public List<PathStep> Path { get; set; } = new List<PathStep>();
    public double MinFuel { get; set; }
    public DateTime CreatedAt { get; set; }
}
