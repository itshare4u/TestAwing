using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using TestAwing.Models;

namespace TestAwing.Tests.Integration;

public class TreasureHuntApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TreasureHuntApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing context registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TreasureHuntContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<TreasureHuntContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase-" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task SolveTreasureHunt_ValidRequest_ReturnsCorrectSolution()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new[] { 3, 2, 2 },
                new[] { 2, 2, 2 },
                new[] { 2, 2, 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/treasure-hunt", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntResponse>();
        
        Assert.NotNull(result);
        Assert.True(result.MinFuel > 0);
        Assert.NotNull(result.Path);
        Assert.True(result.Path.Count > 0);
        
        // Verify path starts at (1,1) with chest 0
        Assert.Equal(0, result.Path[0].ChestNumber);
        Assert.Equal(1, result.Path[0].Row);
        Assert.Equal(1, result.Path[0].Col);
        
        // Verify path ends with chest P
        Assert.Equal(request.P, result.Path.Last().ChestNumber);
    }

    [Fact]
    public async Task SolveTreasureHunt_InvalidMatrix_ReturnsBadRequest()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 5, // P > N*M
            Matrix = new int[][]
            {
                new[] { 1, 2 },
                new[] { 2, 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/treasure-hunt", request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SolveAsync_ValidRequest_ReturnsAsyncResponse()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][]
            {
                new[] { 1, 2 },
                new[] { 2, 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/treasure-hunt/solve-async", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AsyncSolveResponse>();
        
        Assert.NotNull(result);
        Assert.True(result.SolveId > 0);
        Assert.Equal(SolveStatus.Pending, result.Status);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task GetSolveStatus_ExistingSolve_ReturnsStatus()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][]
            {
                new[] { 1, 2 },
                new[] { 2, 1 }
            }
        };

        var solveResponse = await _client.PostAsJsonAsync("/api/treasure-hunt/solve-async", request);
        var asyncResult = await solveResponse.Content.ReadFromJsonAsync<AsyncSolveResponse>();

        // Act
        var response = await _client.GetAsync($"/api/treasure-hunt/solve-status/{asyncResult!.SolveId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SolveStatusResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(asyncResult.SolveId, result.SolveId);
        Assert.True(result.Status == SolveStatus.Pending || 
                   result.Status == SolveStatus.InProgress || 
                   result.Status == SolveStatus.Completed);
    }

    [Fact]
    public async Task GetSolveStatus_NonExistentSolve_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/treasure-hunt/solve-status/999");

        // Assert
        // Should return 404 for non-existent solve operations
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelSolve_ExistingSolve_ReturnsCancelled()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new[] { 3, 2, 2 },
                new[] { 2, 2, 2 },
                new[] { 2, 2, 1 }
            }
        };

        var solveResponse = await _client.PostAsJsonAsync("/api/treasure-hunt/solve-async", request);
        var asyncResult = await solveResponse.Content.ReadFromJsonAsync<AsyncSolveResponse>();

        // Act
        var response = await _client.PostAsync($"/api/treasure-hunt/cancel-solve/{asyncResult!.SolveId}", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AsyncSolveResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(asyncResult.SolveId, result.SolveId);
        Assert.Equal(SolveStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task GetTreasureHunts_EmptyDatabase_ReturnsEmptyPaginatedResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/treasure-hunts");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<TreasureHuntResult>>();
        
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(8, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetTreasureHunts_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Create some test data by solving problems
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][]
            {
                new[] { 1, 2 },
                new[] { 2, 1 }
            }
        };

        // Create multiple solved problems
        for (int i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync("/api/treasure-hunt", request);
        }

        // Act
        var response = await _client.GetAsync("/api/treasure-hunts?page=1&pageSize=2");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<TreasureHuntResult>>();
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetTreasureHunt_ExistingId_ReturnsFullDetails()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][]
            {
                new[] { 1, 2 },
                new[] { 2, 1 }
            }
        };

        var solveResponse = await _client.PostAsJsonAsync("/api/treasure-hunt", request);
        var solved = await solveResponse.Content.ReadFromJsonAsync<TreasureHuntResponse>();

        // Act
        var response = await _client.GetAsync($"/api/treasure-hunt/{solved!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntResultWithPath>();
        
        Assert.NotNull(result);
        Assert.Equal(solved.Id, result.Id);
        Assert.Equal(request.N, result.N);
        Assert.Equal(request.M, result.M);
        Assert.Equal(request.P, result.P);
        Assert.NotNull(result.Matrix);
        Assert.NotNull(result.Path);
    }

    [Fact]
    public async Task GetTreasureHunt_NonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/treasure-hunt/999");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenerateRandomData_ValidParameters_ReturnsValidMatrix()
    {
        // Act
        var response = await _client.GetAsync("/api/generate-random-data?n=3&m=4&p=5");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntRequest>();
        
        Assert.NotNull(result);
        Assert.Equal(3, result.N);
        Assert.Equal(4, result.M);
        Assert.Equal(5, result.P);
        Assert.NotNull(result.Matrix);
        Assert.Equal(3, result.Matrix.Length);
        Assert.All(result.Matrix, row => Assert.Equal(4, row.Length));
        
        // Verify all numbers from 1 to 5 appear at least once
        var allNumbers = result.Matrix.SelectMany(row => row).ToList();
        for (int i = 1; i <= 5; i++)
        {
            Assert.Contains(i, allNumbers);
        }
    }

    [Fact]
    public async Task GenerateRandomData_DefaultParameters_ReturnsValidMatrix()
    {
        // Act
        var response = await _client.GetAsync("/api/generate-random-data");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntRequest>();
        
        Assert.NotNull(result);
        Assert.True(result.N > 0);
        Assert.True(result.M > 0);
        Assert.True(result.P > 0);
        Assert.NotNull(result.Matrix);
    }

    [Fact]
    public async Task GenerateRandomData_InvalidParameters_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/generate-random-data?n=2&m=2&p=5");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
