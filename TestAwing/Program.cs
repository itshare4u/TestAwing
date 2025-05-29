using Microsoft.EntityFrameworkCore;
using TestAwing.Models;
using TestAwing.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Configure database based on settings
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider", "InMemory");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

switch (databaseProvider.ToLower())
{
    case "sqlite":
        builder.Services.AddDbContext<TreasureHuntContext>(options =>
            options.UseSqlite(connectionString ?? "Data Source=treasurehunt.db"));
        break;
        
    case "sqlserver":
        builder.Services.AddDbContext<TreasureHuntContext>(options =>
            options.UseSqlServer(connectionString ?? 
                "Server=(localdb)\\mssqllocaldb;Database=TreasureHuntDb;Trusted_Connection=true;"));
        break;
        
    case "mysql":
        var version = new MySqlServerVersion(new Version(8, 0, 29));
        builder.Services.AddDbContext<TreasureHuntContext>(options =>
            options.UseMySql(connectionString ?? 
                "Server=localhost;Database=TreasureHuntDb;Uid=root;Pwd=;", version));
        break;
        
    case "inmemory":
    default:
        builder.Services.AddDbContext<TreasureHuntContext>(options =>
            options.UseInMemoryDatabase("TreasureHuntDb"));
        break;
}

builder.Services.AddScoped<OptimizedTreasureHuntService>();
builder.Services.AddScoped<ParallelTreasureHuntService>();
builder.Services.AddScoped<AsyncTreasureHuntService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

// Create database if not using InMemory
if (databaseProvider.ToLower() != "inmemory")
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
        
        try
        {
            // For SQL Server and MySQL, use migrations if available, otherwise create database
            if (databaseProvider.ToLower() == "sqlserver" || databaseProvider.ToLower() == "mysql")
            {
                context.Database.Migrate();
            }
            else
            {
                context.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            // Log error and fall back to EnsureCreated
            Console.WriteLine($"Migration failed: {ex.Message}. Falling back to EnsureCreated.");
            context.Database.EnsureCreated();
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");

// Async Treasure Hunt API endpoints
app.MapPost("/api/treasure-hunt/async", async (AsyncSolveRequest request, AsyncTreasureHuntService asyncService) =>
{
    try
    {
        var result = await asyncService.StartSolveAsync(request.TreasureHuntRequest);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/treasure-hunt/async/{solveId}/status", async (int solveId, AsyncTreasureHuntService asyncService) =>
{
    try
    {
        var result = await asyncService.GetSolveStatusAsync(solveId);
        if (result == null)
        {
            return Results.NotFound(new { message = "Solve operation not found" });
        }
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/treasure-hunt/async/{solveId}/cancel", async (int solveId, AsyncTreasureHuntService asyncService) =>
{
    try
    {
        var success = await asyncService.CancelSolveAsync(solveId);
        if (success)
        {
            return Results.Ok(new { message = "Solve operation cancelled successfully" });
        }
        return Results.BadRequest(new { message = "Could not cancel solve operation. It may have already completed or been cancelled." });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Treasure Hunt API endpoints
app.MapPost("/api/treasure-hunt", async (TreasureHuntRequest request, OptimizedTreasureHuntService service) =>
{
    try
    {
        var result = await service.SolveTreasureHunt(request);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/treasurehunts", async (int? page, int? pageSize, OptimizedTreasureHuntService service) =>
{
    var pageNum = page ?? 1;
    var pageSizeNum = pageSize ?? 8;
    
    var results = await service.GetPaginatedTreasureHunts(pageNum, pageSizeNum);
    return Results.Ok(results);
});

app.MapGet("/api/treasure-hunt/{id}", async (int id, OptimizedTreasureHuntService service) =>
{
    try
    {
        var result = await service.GetTreasureHuntById(id);
        if (result == null)
        {
            return Results.NotFound(new { message = "Treasure hunt not found" });
        }
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Generate random test data endpoint
app.MapGet("/api/generate-random-data", (int? n, int? m, int? p, OptimizedTreasureHuntService service) =>
{
    try
    {
        // Use provided parameters or defaults
        var rows = n ?? 3;
        var cols = m ?? 3;
        var maxChest = p ?? Math.Min(rows * cols, 10); // Default p to reasonable value
        
        // Validate parameters
        if (rows < 1 || rows > 500 || cols < 1 || cols > 500 || maxChest < 1)
        {
            return Results.BadRequest(new { message = "Invalid parameters. n and m must be 1-500, p must be >= 1" });
        }
        
        // Ensure we have enough positions for all chest numbers (each chest 1 to p must appear at least once)
        if (rows * cols < maxChest)
        {
            return Results.BadRequest(new { 
                message = $"Matrix size (n×m = {rows * cols}) must be at least p ({maxChest}) to fit all chest numbers from 1 to p" 
            });
        }

        var randomData = service.GenerateRandomTestData(rows, cols, maxChest);
        return Results.Ok(randomData);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Parallel implementation endpoint
app.MapPost("/api/treasure-hunt/parallel", async (TreasureHuntRequest request, ParallelTreasureHuntService service) =>
{
    try
    {
        var result = await service.SolveTreasureHunt(request);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Async solve endpoints
app.MapPost("/api/treasure-hunt/solve-async", async (AsyncSolveRequest request, AsyncTreasureHuntService service) =>
{
    try
    {
        var result = await service.StartSolveAsync(request.TreasureHuntRequest);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/treasure-hunt/solve-status/{id}", async (int id, AsyncTreasureHuntService service) =>
{
    try
    {
        var status = await service.GetSolveStatusAsync(id);
        if (status == null)
        {
            return Results.NotFound(new { message = "Solve operation not found" });
        }
        return Results.Ok(status);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/treasure-hunt/cancel-solve/{id}", async (int id, AsyncTreasureHuntService service) =>
{
    try
    {
        var success = await service.CancelSolveAsync(id);
        if (success)
        {
            return Results.Ok(new { message = "Solve operation cancelled successfully" });
        }
        else
        {
            return Results.BadRequest(new { message = "Unable to cancel solve operation" });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Configure to run on port 5001
app.Urls.Add("http://localhost:5001");

app.Run();

// Make Program class accessible for testing
public partial class Program { }
