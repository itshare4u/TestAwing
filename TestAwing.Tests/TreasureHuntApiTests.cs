using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestAwing.Models;

namespace TestAwing.Tests;

[Collection("TreasureHuntApiTests")]
public class TreasureHuntApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public TreasureHuntApiTests(WebApplicationFactory<Program> factory)
    {
        _databaseName = Guid.NewGuid().ToString();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing context registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TreasureHuntContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<TreasureHuntContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task PostTreasureHunt_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 3,
            P = 3,
            Matrix = new int[][]
            {
                new int[] { 3, 2, 2 },
                new int[] { 2, 2, 2 },
                new int[] { 2, 2, 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/treasure-hunt", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntResponse>();
        Assert.NotNull(result);
        Assert.True(result.MinFuel > 0);
        Assert.True(result.Id > 0);
        Assert.NotEmpty(result.Path);
    }

    [Fact]
    public async Task PostTreasureHunt_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 0, // Invalid
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
        var response = await _client.PostAsJsonAsync("/api/treasure-hunt", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTreasureHunts_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/treasure-hunts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var paginatedResults = await response.Content.ReadFromJsonAsync<PaginatedResponse<TreasureHuntResult>>();
        Assert.NotNull(paginatedResults);
        Assert.Empty(paginatedResults.Data);
        Assert.Equal(0, paginatedResults.TotalCount);
        Assert.Equal(1, paginatedResults.Page);
    }

    [Fact]
    public async Task GetTreasureHunts_WithData_ReturnsResults()
    {
        // Arrange - First, create some data
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][] { new int[] { 1, 2 }, new int[] { 2, 1 } }
        };

        await _client.PostAsJsonAsync("/api/treasure-hunt", request);

        // Act
        var response = await _client.GetAsync("/api/treasure-hunts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var paginatedResults = await response.Content.ReadFromJsonAsync<PaginatedResponse<TreasureHuntResult>>();
        Assert.NotNull(paginatedResults);
        Assert.Single(paginatedResults.Data);
        Assert.Equal(1, paginatedResults.TotalCount);
        Assert.Equal(2, paginatedResults.Data[0].N);
        Assert.Equal(2, paginatedResults.Data[0].M);
        Assert.Equal(2, paginatedResults.Data[0].P);
    }

    [Theory]
    [InlineData(3, 3, 5)]
    [InlineData(4, 4, 8)]
    [InlineData(2, 5, 3)]
    public async Task GetGenerateRandomData_ValidParameters_ReturnsValidData(int n, int m, int p)
    {
        // Act
        var response = await _client.GetAsync($"/api/generate-random-data?n={n}&m={m}&p={p}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntRequest>();
        Assert.NotNull(result);
        Assert.Equal(n, result.N);
        Assert.Equal(m, result.M);
        Assert.Equal(p, result.P);
        Assert.Equal(n, result.Matrix.Length);
        Assert.All(result.Matrix, row => Assert.Equal(m, row.Length));
        
        // Verify each chest number from 1 to p appears at least once
        var allValues = result.Matrix.SelectMany(row => row).ToList();
        for (int chest = 1; chest <= p; chest++)
        {
            Assert.Contains(chest, allValues);
        }
    }

    [Fact]
    public async Task GetGenerateRandomData_DefaultParameters_ReturnsValidData()
    {
        // Act
        var response = await _client.GetAsync("/api/generate-random-data");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntRequest>();
        Assert.NotNull(result);
        Assert.Equal(3, result.N); // Default value
        Assert.Equal(3, result.M); // Default value
        Assert.True(result.P <= 10); // Default max value
    }

    [Fact]
    public async Task GetGenerateRandomData_InvalidParameters_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/generate-random-data?n=0&m=3&p=3");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetGenerateRandomData_InsufficientMatrixSize_ReturnsBadRequest()
    {
        // Arrange - p > n*m
        var response = await _client.GetAsync("/api/generate-random-data?n=2&m=2&p=5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTreasureHuntById_ExistingId_ReturnsResult()
    {
        // Arrange - Create a treasure hunt first
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 2,
            Matrix = new int[][] { new int[] { 1, 2 }, new int[] { 2, 1 } }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/treasure-hunt", request);
        var createResult = await createResponse.Content.ReadFromJsonAsync<TreasureHuntResponse>();
        
        // Act
        var response = await _client.GetAsync($"/api/treasure-hunt/{createResult!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntResultWithPath>();
        Assert.NotNull(result);
        Assert.Equal(createResult.Id, result.Id);
        Assert.NotNull(result.Matrix);
        Assert.NotNull(result.Path);
    }

    [Fact]
    public async Task GetTreasureHuntById_NonExistingId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/treasure-hunt/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostTreasureHunt_ComplexMatrix_HandlesCorrectly()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 3,
            M = 4,
            P = 12,
            Matrix = new int[][]
            {
                new int[] { 1, 2, 3, 4 },
                new int[] { 8, 7, 6, 5 },
                new int[] { 9, 10, 11, 12 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/treasure-hunt", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<TreasureHuntResponse>();
        Assert.NotNull(result);
        Assert.True(result.MinFuel > 0);
        Assert.Equal(13, result.Path.Count); // Start + 12 chests
    }

    [Fact]
    public async Task PostTreasureHunt_MissingChestInMatrix_ReturnsProblem()
    {
        // Arrange
        var request = new TreasureHuntRequest
        {
            N = 2,
            M = 2,
            P = 3, // But chest 3 is not in the matrix
            Matrix = new int[][]
            {
                new int[] { 1, 2 },
                new int[] { 2, 1 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/treasure-hunt", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Integration_FullWorkflow_WorksCorrectly()
    {
        // 1. Generate random data
        var randomResponse = await _client.GetAsync("/api/generate-random-data?n=3&m=3&p=4");
        Assert.Equal(HttpStatusCode.OK, randomResponse.StatusCode);
        
        var randomData = await randomResponse.Content.ReadFromJsonAsync<TreasureHuntRequest>();
        Assert.NotNull(randomData);

        // 2. Solve the generated treasure hunt
        var solveResponse = await _client.PostAsJsonAsync("/api/treasure-hunt", randomData);
        Assert.Equal(HttpStatusCode.OK, solveResponse.StatusCode);
        
        var solveResult = await solveResponse.Content.ReadFromJsonAsync<TreasureHuntResponse>();
        Assert.NotNull(solveResult);
        Assert.True(solveResult.Id > 0);

        // 3. Get all treasure hunts
        var allResponse = await _client.GetAsync("/api/treasure-hunts");
        Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
        
        var paginatedResults = await allResponse.Content.ReadFromJsonAsync<PaginatedResponse<TreasureHuntResult>>();
        Assert.NotNull(paginatedResults);
        Assert.Contains(paginatedResults.Data, r => r.Id == solveResult.Id);

        // 4. Get specific treasure hunt by ID
        var specificResponse = await _client.GetAsync($"/api/treasure-hunt/{solveResult.Id}");
        Assert.Equal(HttpStatusCode.OK, specificResponse.StatusCode);
        
        var specificResult = await specificResponse.Content.ReadFromJsonAsync<TreasureHuntResultWithPath>();
        Assert.NotNull(specificResult);
        Assert.Equal(solveResult.Id, specificResult.Id);
        Assert.NotEmpty(specificResult.Path);
    }
}
