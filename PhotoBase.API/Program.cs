using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhotoBase.API.Configuration;
using PhotoBase.API.Data;
using PhotoBase.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// EF Core — SQL Server (ADR-0001 updated to SQL Server per user request)
builder.Services.AddDbContext<PhotoBaseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PhotoBaseDb")));

// Configuration options
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// JWT Authentication
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"] ?? "PhotoBase",
            ValidAudience = jwtSection["Audience"] ?? "PhotoBase",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

// Application services
builder.Services.AddSingleton<IHashService, Sha256HashService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<IThumbnailService, ImageSharpThumbnailService>();
builder.Services.AddScoped<TsvImportService>();
builder.Services.AddScoped<TokenService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-apply pending migrations in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PhotoBaseDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}

// Skip HTTPS redirect in test environment — WebApplicationFactory uses HTTP
// and redirects strip the Authorization header, breaking authenticated tests.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// Make the implicit Program class visible to WebApplicationFactory
public partial class Program { }
