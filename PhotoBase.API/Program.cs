using Microsoft.EntityFrameworkCore;
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

// Application services
builder.Services.AddSingleton<IHashService, Sha256HashService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<IThumbnailService, ImageSharpThumbnailService>();
builder.Services.AddScoped<TsvImportService>();

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

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();


app.UseAuthorization();
app.MapRazorPages();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// Make the implicit Program class visible to WebApplicationFactory
public partial class Program { }
