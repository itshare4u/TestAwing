using Microsoft.EntityFrameworkCore;
using TreasureHunt.Models;
using TreasureHunt.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddDbContext<TreasureHuntContext>(options =>
    options.UseInMemoryDatabase("TreasureHuntDb"));
builder.Services.AddScoped<TreasureHuntService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

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

// Configure to run on port 5001
app.Urls.Add("http://localhost:5001");

app.Run();
