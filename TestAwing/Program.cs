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

builder.Services.AddScoped<TreasureHuntDataService>();
builder.Services.AddScoped<TreasureHuntSolverService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

// Create database if not using InMemory
if (!databaseProvider.Equals("inmemory", StringComparison.CurrentCultureIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TreasureHuntContext>();
        
    try
    {
        // For SQL Server and MySQL, use migrations if available, otherwise create database
        if (databaseProvider.Equals("sqlserver", StringComparison.CurrentCultureIgnoreCase) || databaseProvider.Equals("mysql", StringComparison.CurrentCultureIgnoreCase))
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");

// Async Treasure Hunt API endpoints
app.MapPost("/api/treasure-hunt/async", async (AsyncSolveRequest request, TreasureHuntSolverService solverService) =>
{
    try
    {
        var result = await solverService.StartSolveAsync(request.TreasureHuntRequest);
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

app.MapGet("/api/treasure-hunt/async/{solveId}/status", async (int solveId, TreasureHuntSolverService solverService) =>
{
    try
    {
        var result = await solverService.GetSolveStatusAsync(solveId);
        return result == null ? Results.NotFound(new { message = "Solve operation not found" }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/treasure-hunt/async/{solveId}/cancel", async (int solveId, TreasureHuntSolverService solverService) =>
{
    try
    {
        var success = await solverService.CancelSolveAsync(solveId);
        return success ? Results.Ok(new { message = "Solve operation cancelled successfully" }) : Results.BadRequest(new { message = "Could not cancel solve operation. It may have already completed or been cancelled." });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/treasure-hunts", async (int? page, int? pageSize, TreasureHuntDataService dataService) =>
{
    var pageNum = page ?? 1;
    var pageSizeNum = pageSize ?? 8;
    
    var results = await dataService.GetPaginatedTreasureHunts(pageNum, pageSizeNum);
    return Results.Ok(results);
});

app.MapGet("/api/treasure-hunt/{id}", async (int id, TreasureHuntDataService dataService) =>
{
    try
    {
        var result = await dataService.GetTreasureHuntById(id);
        return result == null ? Results.NotFound(new { message = "Treasure hunt not found" }) : Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Generate random test data endpoint
app.MapGet("/api/generate-random-data", (int? n, int? m, int? p, TreasureHuntDataService dataService) =>
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

        var randomData = dataService.GenerateRandomTestData(rows, cols, maxChest);
        return Results.Ok(randomData);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// Async solve endpoints
app.MapPost("/api/treasure-hunt/solve-async", async (AsyncSolveRequest request, TreasureHuntSolverService solverService) =>
{
    try
    {
        var result = await solverService.StartSolveAsync(request.TreasureHuntRequest);
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

app.MapGet("/api/treasure-hunt/solve-status/{id}", async (int id, TreasureHuntSolverService solverService) =>
{
    try
    {
        var status = await solverService.GetSolveStatusAsync(id);
        return status == null ? Results.NotFound(new { message = "Solve operation not found" }) : Results.Ok(status);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/treasure-hunt/cancel-solve/{id}", async (int id, TreasureHuntSolverService solverService) =>
{
    try
    {
        var success = await solverService.CancelSolveAsync(id);
        return success ? Results.Ok(new { message = "Solve operation cancelled successfully" }) : Results.BadRequest(new { message = "Unable to cancel solve operation" });
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
