using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhotoBase.API.Configuration;
using PhotoBase.API.Data;

namespace PhotoBase.Tests;

/// <summary>
/// Test host that replaces SQL Server with in-memory SQLite and uses a temp
/// directory for file storage. Each factory instance gets its own isolated DB + storage.
/// </summary>
public class PhotoBaseTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    private readonly string _tempStoragePath;

    public PhotoBaseTestFactory()
    {
        // Keep the SQLite connection open for the lifetime of the factory
        // so the in-memory DB doesn't get wiped between requests.
        _connection = new SqliteConnection("DataSource=:memory:");
   _connection.Open();

      _tempStoragePath = Path.Combine(Path.GetTempPath(), "photobase-tests-" + Guid.NewGuid().ToString("N")[..8]);
    Directory.CreateDirectory(_tempStoragePath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration
            var descriptor = services.SingleOrDefault(
         d => d.ServiceType == typeof(DbContextOptions<PhotoBaseDbContext>));
            if (descriptor is not null)
    services.Remove(descriptor);

      // Add SQLite in-memory
            services.AddDbContext<PhotoBaseDbContext>(options =>
                options.UseSqlite(_connection));

       // Override storage paths to use temp directory
      services.Configure<StorageOptions>(opts =>
  {
         opts.BasePath = _tempStoragePath;
            opts.OriginalsFolder = "originals";
         opts.ThumbsFolder = "thumbs";
            });

  // Build a temporary service provider to run migrations + seed
    var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoBaseDbContext>();
         db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
        _connection.Dispose();

      // Clean up temp storage
     try { if (Directory.Exists(_tempStoragePath)) Directory.Delete(_tempStoragePath, recursive: true); }
        catch { /* best effort */ }
    }
}
