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

builder.Services.AddScoped<TreasureHuntService>();
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

// Treasure Hunt API endpoints
app.MapPost("/api/treasure-hunt", async (TreasureHuntRequest request, TreasureHuntService service) =>
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

app.MapGet("/api/treasure-hunts", async (TreasureHuntService service) =>
{
    var results = await service.GetAllTreasureHunts();
    return Results.Ok(results);
});

app.MapGet("/api/treasure-hunt/{id}", async (int id, TreasureHuntService service) =>
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
app.MapGet("/api/generate-random-data", (int? n, int? m, int? p, TreasureHuntService service) =>
{
    try
    {
        // Use provided parameters or defaults
        var rows = n ?? 3;
        var cols = m ?? 3;
        var maxChest = p ?? (rows * cols); // Default p to n×m for valid treasure hunt
        
        // Validate parameters
        if (rows < 1 || rows > 500 || cols < 1 || cols > 500 || maxChest < 1)
        {
            return Results.BadRequest(new { message = "Invalid parameters. n and m must be 1-500, p must be >= 1" });
        }
        
        // For valid treasure hunt: p must equal n×m (each chest number 1 to p appears exactly once)
        if (maxChest != rows * cols)
        {
            return Results.BadRequest(new { 
                message = $"For valid treasure hunt: p must equal n×m. Expected p={rows * cols}, got p={maxChest}. " +
                          "Each chest number from 1 to p must appear exactly once in the matrix." 
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

// Configure to run on port 5001
app.Urls.Add("http://localhost:5001");

app.Run();
