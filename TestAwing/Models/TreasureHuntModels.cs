using System.ComponentModel.DataAnnotations;

namespace TestAwing.Models;

public abstract class TreasureHuntRequest
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
    public double MinFuel { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TreasureHuntResponse
{
    public double MinFuel { get; set; }
    public int Id { get; set; }
}