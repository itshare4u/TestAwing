using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestAwing.Models;

namespace TestAwing.Tests.Base;

/// <summary>
/// Base class for tests that need to use a SQL Server database with Docker
/// </summary>
public abstract class SqlServerTestBase : IDisposable
{
    protected readonly ServiceProvider ServiceProvider;
    protected readonly TreasureHuntContext Context;
    
    // Connection string cho SQL Server chạy trong Docker
    private const string TestConnectionString = 
        "Server=localhost,1434;Database=TreasureHuntTestDb;User=sa;Password=TestPassword@2024;TrustServerCertificate=true;";

    protected SqlServerTestBase()
    {
        // Thiết lập database với SQL Server
        var services = new ServiceCollection();
        
        // Sử dụng SQL Server thay vì InMemory
        services.AddDbContext<TreasureHuntContext>(options =>
            options.UseSqlServer(TestConnectionString));
        
        ServiceProvider = services.BuildServiceProvider();
        Context = ServiceProvider.GetRequiredService<TreasureHuntContext>();
        
        // Đảm bảo database được tạo
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        // Xóa database sau khi test hoàn thành
        Context.Database.EnsureDeleted();
        Context?.Dispose();
        ServiceProvider?.Dispose();
    }
}
