using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PhotoBase.API.Configuration;
using PhotoBase.API.Data;

namespace PhotoBase.Tests;

/// <summary>
/// Test host that replaces SQL Server with in-memory SQLite and uses a temp
/// directory for file storage. Each factory instance gets its own isolated DB + storage.
/// Configures JWT so tests can authenticate.
/// </summary>
public class PhotoBaseTestFactory : WebApplicationFactory<Program>
{
    public const string TestJwtSecret = "Test-Only-Secret-Key-For-PhotoBase-Unit-Tests-2024!!";

    private readonly SqliteConnection _connection;
    private readonly string _tempStoragePath;

    public PhotoBaseTestFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _tempStoragePath = Path.Combine(Path.GetTempPath(), "photobase-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempStoragePath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide JWT settings BEFORE the host builds so Program.cs reads them
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = TestJwtSecret,
                ["Jwt:Issuer"] = "PhotoBase",
                ["Jwt:Audience"] = "PhotoBase",
                ["Jwt:ExpiresInMinutes"] = "60"
            });
        });

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

            // Override JWT bearer token validation to use the test secret key.
            // Program.cs captures the signing key at build time from config, but
            // ConfigureAppConfiguration runs after the host builder's initial read.
            // PostConfigure ensures we overwrite the signing key regardless.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "PhotoBase",
                    ValidAudience = "PhotoBase",
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(TestJwtSecret)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

            // Also override JwtOptions so TokenService signs with the same key
            services.Configure<JwtOptions>(opts =>
            {
                opts.SecretKey = TestJwtSecret;
                opts.Issuer = "PhotoBase";
                opts.Audience = "PhotoBase";
                opts.ExpiresInMinutes = 60;
            });

            // Build a temporary service provider to create DB schema + seed
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PhotoBaseDbContext>();
            db.Database.EnsureCreated();
        });
    }

    /// <summary>
    /// Get a JWT token for the given dev user by calling POST /api/auth/login.
    /// </summary>
    public async Task<string> GetTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<PhotoBase.Shared.Responses.LoginResponse>();
        return login!.Token;
    }

    /// <summary>
    /// Create an HttpClient pre-authenticated as the admin dev user.
    /// </summary>
    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        var token = await GetTokenAsync(client, "admin@photobase.dev", "Admin123!");
        client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Create an HttpClient pre-authenticated as the internal dev user.
    /// </summary>
    public async Task<HttpClient> CreateInternalClientAsync()
    {
        var client = CreateClient();
        var token = await GetTokenAsync(client, "user@photobase.dev", "User123!");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();

        try { if (Directory.Exists(_tempStoragePath)) Directory.Delete(_tempStoragePath, recursive: true); }
        catch { /* best effort */ }
    }
}
