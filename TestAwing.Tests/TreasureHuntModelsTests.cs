using System.ComponentModel.DataAnnotations;
using TestAwing.Models;

namespace TestAwing.Tests;

public class TreasureHuntModelsTests
{
    [Fact]
    public void TreasureHuntRequest_ValidData_PassesValidation()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new int[] { 1, 2, 3 },
                new int[] { 2, 3, 1 },
                new int[] { 3, 1, 2 }
            }
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(0, 3, 3)] // N too small
    [InlineData(501, 3, 3)] // N too large
    [InlineData(3, 0, 3)] // M too small
    [InlineData(3, 501, 3)] // M too large
    [InlineData(3, 3, 0)] // P too small
    public void TreasureHuntRequest_InvalidData_FailsValidation(int n, int m, int p)
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = n,
            M = m,
            P = p,
            Matrix = new int[][]
            {
                new int[] { 1, 2, 3 },
                new int[] { 2, 3, 1 },
                new int[] { 3, 1, 2 }
            }
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.NotEmpty(validationResults);
    }

    [Fact]
    public void TreasureHuntRequest_NullMatrix_FailsValidation()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = null!
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains("Matrix"));
    }

    [Fact]
    public void PathStep_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var pathStep = new PathStep();

        // Assert
        Assert.Equal(0, pathStep.ChestNumber);
        Assert.Equal(0, pathStep.Row);
        Assert.Equal(0, pathStep.Col);
        Assert.Equal(0, pathStep.FuelUsed);
        Assert.Equal(0, pathStep.CumulativeFuel);
    }

    [Fact]
    public void PathStep_SetProperties_WorksCorrectly()
    {
        // Arrange
        var pathStep = new PathStep
        {
            ChestNumber = 5,
            Row = 2,
            Col = 3,
            FuelUsed = 1.5,
            CumulativeFuel = 4.7
        };

        // Act & Assert
        Assert.Equal(5, pathStep.ChestNumber);
        Assert.Equal(2, pathStep.Row);
        Assert.Equal(3, pathStep.Col);
        Assert.Equal(1.5, pathStep.FuelUsed);
        Assert.Equal(4.7, pathStep.CumulativeFuel);
    }

    [Fact]
    public void TreasureHuntResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new TreasureHuntResponse();

        // Assert
        Assert.Equal(0, response.MinFuel);
        Assert.Equal(0, response.Id);
        Assert.NotNull(response.Path);
        Assert.Empty(response.Path);
    }

    [Fact]
    public void TreasureHuntResult_Properties_CanBeSetAndGet()
    {
        // Arrange
        var result = new TreasureHuntResult
        {
            Id = 1,
            N = 3,
            M = 4,
            P = 5,
            MatrixJson = "[[1,2,3,4],[5,1,2,3],[4,5,1,2]]",
            PathJson = "[{\"ChestNumber\":0,\"Row\":0,\"Col\":0}]",
            MinFuel = 7.5,
            CreatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act & Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(3, result.N);
        Assert.Equal(4, result.M);
        Assert.Equal(5, result.P);
        Assert.Equal("[[1,2,3,4],[5,1,2,3],[4,5,1,2]]", result.MatrixJson);
        Assert.Equal("[{\"ChestNumber\":0,\"Row\":0,\"Col\":0}]", result.PathJson);
        Assert.Equal(7.5, result.MinFuel);
        Assert.Equal(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc), result.CreatedAt);
    }

    [Fact]
    public void TreasureHuntResultWithPath_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new TreasureHuntResultWithPath();

        // Assert
        Assert.Equal(0, result.Id);
        Assert.Equal(0, result.N);
        Assert.Equal(0, result.M);
        Assert.Equal(0, result.P);
        Assert.NotNull(result.Matrix);
        Assert.Empty(result.Matrix);
        Assert.NotNull(result.Path);
        Assert.Empty(result.Path);
        Assert.Equal(0, result.MinFuel);
        Assert.Equal(default(DateTime), result.CreatedAt);
    }

    [Fact]
    public void TreasureHuntResultWithPath_Properties_CanBeSetAndGet()
    {
        // Arrange
        var matrix = new int[][]
        {
            new int[] { 1, 2 },
            new int[] { 2, 1 }
        };

        var path = new List<PathStep>
        {
            new PathStep { ChestNumber = 0, Row = 0, Col = 0, FuelUsed = 0, CumulativeFuel = 0 },
            new PathStep { ChestNumber = 1, Row = 0, Col = 0, FuelUsed = 0, CumulativeFuel = 0 }
        };

        var result = new TreasureHuntResultWithPath
        {
            Id = 1,
            N = 2,
            M = 2,
            P = 2,
            Matrix = matrix,
            Path = path,
            MinFuel = 1.0,
            CreatedAt = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act & Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(2, result.N);
        Assert.Equal(2, result.M);
        Assert.Equal(2, result.P);
        Assert.Equal(matrix, result.Matrix);
        Assert.Equal(path, result.Path);
        Assert.Equal(1.0, result.MinFuel);
        Assert.Equal(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc), result.CreatedAt);
    }

    [Theory]
    [InlineData(1, 500)] // Valid range boundaries
    [InlineData(500, 1)]
    [InlineData(250, 250)]
    public void TreasureHuntRequest_ValidBoundaryValues_PassValidation(int n, int m)
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = n,
            M = m,
            P = 1,
            Matrix = new int[n][]
        };

        // Initialize matrix
        for (int i = 0; i < n; i++)
        {
            request.Matrix[i] = new int[m];
            for (int j = 0; j < m; j++)
            {
                request.Matrix[i][j] = 1;
            }
        }

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}
