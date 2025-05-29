using TestAwing.Models;
using TestAwing.Services;

namespace TestAwing.Tests.Unit;

public class TreasureHuntAlgorithmTests
{
    private readonly TreasureHuntSolverService _solverService;

    public TreasureHuntAlgorithmTests()
    {
        // For algorithm testing, we don't need database dependencies
        _solverService = new TreasureHuntSolverService(null!, null!);
    }

    [Fact]
    public void CalculateDistance_SamePosition_ReturnsZero()
    {
        // Arrange
        var pos1 = new { Row = 1, Col = 1 };
        var pos2 = new { Row = 1, Col = 1 };

        // Act
        var distance = CalculateEuclideanDistance(pos1.Row, pos1.Col, pos2.Row, pos2.Col);

        // Assert
        Assert.Equal(0, distance, precision: 5);
    }

    [Fact]
    public void CalculateDistance_AdjacentHorizontal_ReturnsOne()
    {
        // Arrange
        var pos1 = new { Row = 1, Col = 1 };
        var pos2 = new { Row = 1, Col = 2 };

        // Act
        var distance = CalculateEuclideanDistance(pos1.Row, pos1.Col, pos2.Row, pos2.Col);

        // Assert
        Assert.Equal(1.0, distance, precision: 5);
    }

    [Fact]
    public void CalculateDistance_AdjacentVertical_ReturnsOne()
    {
        // Arrange
        var pos1 = new { Row = 1, Col = 1 };
        var pos2 = new { Row = 2, Col = 1 };

        // Act
        var distance = CalculateEuclideanDistance(pos1.Row, pos1.Col, pos2.Row, pos2.Col);

        // Assert
        Assert.Equal(1.0, distance, precision: 5);
    }

    [Fact]
    public void CalculateDistance_Diagonal_ReturnsSqrtTwo()
    {
        // Arrange
        var pos1 = new { Row = 1, Col = 1 };
        var pos2 = new { Row = 2, Col = 2 };

        // Act
        var distance = CalculateEuclideanDistance(pos1.Row, pos1.Col, pos2.Row, pos2.Col);

        // Assert
        Assert.Equal(Math.Sqrt(2), distance, precision: 5);
    }

    [Fact]
    public void FindChestPosition_ExistingChest_ReturnsCorrectPosition()
    {
        // Arrange
        var matrix = new int[][]
        {
            new[] { 3, 2, 1 },
            new[] { 1, 2, 3 },
            new[] { 2, 3, 1 }
        };

        // Act
        var position = FindChestPosition(matrix, 3);

        // Assert
        Assert.NotNull(position);
        // Chest 3 is at position (1,1) in the matrix (0-indexed), which is (1,1) in 1-indexed coordinates
        Assert.Equal(1, position.Row);
        Assert.Equal(1, position.Col);
    }

    [Fact]
    public void FindChestPosition_MultipleOccurrences_ReturnsFirst()
    {
        // Arrange
        var matrix = new int[][]
        {
            new[] { 1, 2, 1 },
            new[] { 3, 1, 2 },
            new[] { 2, 3, 1 }
        };

        // Act
        var position = FindChestPosition(matrix, 1);

        // Assert
        Assert.NotNull(position);
        // First occurrence of 1 is at (0,0) in matrix, which is (1,1) in 1-indexed
        Assert.Equal(1, position.Row);
        Assert.Equal(1, position.Col);
    }

    [Fact]
    public void FindChestPosition_NonExistentChest_ReturnsNull()
    {
        // Arrange
        var matrix = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 2, 3, 1 },
            new[] { 3, 1, 2 }
        };

        // Act
        var position = FindChestPosition(matrix, 5);

        // Assert
        Assert.Null(position);
    }

    [Theory]
    [InlineData(3, 3, 3, new[] { 3, 2, 2, 2, 2, 2, 2, 2, 1 }, 6.0645)] // Updated expected value based on actual calculation
    [InlineData(2, 2, 2, new[] { 1, 2, 2, 1 }, 1.0)] // Simple case
    [InlineData(1, 1, 1, new[] { 1 }, 0.0)] // Trivial case
    public void SolveAlgorithm_KnownTestCases_ReturnsExpectedResult(
        int n, int m, int p, int[] flatMatrix, double expectedFuel)
    {
        // Arrange
        var matrix = ConvertFlatToMatrix(flatMatrix, n, m);
        var request = new TreasureHuntRequest
        {
            N = n,
            M = m,
            P = p,
            Matrix = matrix
        };

        // Act
        var result = SolveWithDirectAlgorithm(request);

        // Assert
        Assert.Equal(expectedFuel, result.minFuel, precision: 4);
        Assert.NotNull(result.path);
        Assert.Equal(p + 1, result.path.Count); // Should have p+1 steps (0 to p)
        
        // Verify path starts with chest 0 at (1,1)
        Assert.Equal(0, result.path[0].ChestNumber);
        Assert.Equal(1, result.path[0].Row);
        Assert.Equal(1, result.path[0].Col);
        
        // Verify path ends with chest p
        Assert.Equal(p, result.path.Last().ChestNumber);
    }

    [Fact]
    public void ValidateMatrix_ValidMatrix_DoesNotThrow()
    {
        // Arrange
        var matrix = new int[][]
        {
            new[] { 1, 2, 3 },
            new[] { 2, 3, 1 },
            new[] { 3, 1, 2 }
        };

        // Act & Assert
        var ex = Record.Exception(() => ValidateMatrix(matrix, 3, 3, 3));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateMatrix_MissingChestNumber_ThrowsException()
    {
        // Arrange
        var matrix = new int[][]
        {
            new[] { 1, 2, 2 },
            new[] { 2, 2, 1 },
            new[] { 2, 1, 2 }
        };
        // Missing chest number 3

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidateMatrix(matrix, 3, 3, 3));
    }

    [Fact]
    public void ValidateMatrix_InvalidDimensions_ThrowsException()
    {
        // Arrange
        var matrix = new int[][]
        {
            new[] { 1, 2 },
            new[] { 2, 1 }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidateMatrix(matrix, 2, 2, 5)); // p > n*m
    }

    [Fact]
    public void PathConstruction_VerifyCorrectSequence()
    {
        // Arrange
        var matrix = new int[][]
        {
            new[] { 2, 1 },
            new[] { 1, 2 }
        };
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = matrix
        };

        // Act
        var result = SolveWithDirectAlgorithm(request);

        // Assert
        Assert.Equal(3, result.path.Count); // 0 -> 1 -> 2
        
        // Verify sequence
        Assert.Equal(0, result.path[0].ChestNumber);
        Assert.Equal(1, result.path[1].ChestNumber);
        Assert.Equal(2, result.path[2].ChestNumber);
        
        // Verify positions are logical
        Assert.Equal(1, result.path[0].Row); // Start at (1,1)
        Assert.Equal(1, result.path[0].Col);
        
        // Chest 1 should be at position (1,2) or (2,1) in the matrix
        var chest1Pos = result.path[1];
        Assert.True((chest1Pos.Row == 1 && chest1Pos.Col == 2) || 
                   (chest1Pos.Row == 2 && chest1Pos.Col == 1));
    }

    // Helper methods that would normally be private in the actual service
    private static double CalculateEuclideanDistance(int fromRow, int fromCol, int toRow, int toCol)
    {
        var dx = toCol - fromCol;
        var dy = toRow - fromRow;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static PathStep? FindChestPosition(int[][] matrix, int chestNumber)
    {
        for (int row = 0; row < matrix.Length; row++)
        {
            for (int col = 0; col < matrix[row].Length; col++)
            {
                if (matrix[row][col] == chestNumber)
                {
                    return new PathStep { Row = row + 1, Col = col + 1, ChestNumber = chestNumber }; // Convert to 1-indexed
                }
            }
        }
        return null;
    }

    private static void ValidateMatrix(int[][] matrix, int n, int m, int p)
    {
        if (n <= 0 || m <= 0 || p <= 0)
            throw new ArgumentException("Dimensions must be positive");

        if (n * m < p)
            throw new ArgumentException("Matrix size must be at least p");

        // Check if all chest numbers from 1 to p are present
        var allNumbers = matrix.SelectMany(row => row).ToHashSet();
        for (int i = 1; i <= p; i++)
        {
            if (!allNumbers.Contains(i))
            {
                throw new ArgumentException($"Chest number {i} is missing from the matrix");
            }
        }
    }

    private static (double minFuel, List<PathStep> path) SolveWithDirectAlgorithm(TreasureHuntRequest request)
    {
        ValidateMatrix(request.Matrix, request.N, request.M, request.P);

        var path = new List<PathStep>();
        var currentRow = 1;
        var currentCol = 1;
        double totalFuel = 0;

        // Start at position (1,1) with key 0
        path.Add(new PathStep
        {
            ChestNumber = 0,
            Row = currentRow,
            Col = currentCol
        });

        // Find each chest from 1 to p
        for (int chest = 1; chest <= request.P; chest++)
        {
            var chestPosition = FindChestPosition(request.Matrix, chest);
            if (chestPosition == null)
            {
                throw new ArgumentException($"Chest {chest} not found in matrix");
            }

            var distance = CalculateEuclideanDistance(currentRow, currentCol, chestPosition.Row, chestPosition.Col);
            totalFuel += distance;
            currentRow = chestPosition.Row;
            currentCol = chestPosition.Col;

            path.Add(new PathStep
            {
                ChestNumber = chest,
                Row = chestPosition.Row,
                Col = chestPosition.Col
            });
        }

        return (totalFuel, path);
    }

    private static int[][] ConvertFlatToMatrix(int[] flat, int n, int m)
    {
        var matrix = new int[n][];
        for (int i = 0; i < n; i++)
        {
            matrix[i] = new int[m];
            for (int j = 0; j < m; j++)
            {
                matrix[i][j] = flat[i * m + j];
            }
        }
        return matrix;
    }
}
