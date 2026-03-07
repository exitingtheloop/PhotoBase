namespace PhotoBase.API.Data;

using Microsoft.EntityFrameworkCore;
using PhotoBase.API.Entities;

public class PhotoBaseDbContext : DbContext
{
    public PhotoBaseDbContext(DbContextOptions<PhotoBaseDbContext> options)
   : base(options)
    {
    }

    public DbSet<PlantRecord> PlantRecords => Set<PlantRecord>();
    public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- PlantRecord ----------
        modelBuilder.Entity<PlantRecord>(e =>
        {
            e.ToTable("PlantRecords");
            e.HasKey(p => p.AccessionNumber);
        });

        // ---------- ImageAsset ----------
        modelBuilder.Entity<ImageAsset>(e =>
           {
               e.ToTable("ImageAssets");
               e.HasKey(a => a.Id);

               // Index on SHA-256 hash for duplicate detection (ADR-0005)
               // Non-unique: dedup is enforced at the application level via controller check.
               // ForceOverrideDuplicate allows intentional re-uploads of identical files.
               e.HasIndex(a => a.HashSha256)
            .HasDatabaseName("IX_ImageAssets_HashSha256");

               // Index on AccessionNumber for search joins
               e.HasIndex(a => a.AccessionNumber)
            .HasDatabaseName("IX_ImageAssets_AccessionNumber");

               // FK to PlantRecord (optional)
               e.HasOne(a => a.PlantRecord)
            .WithMany(p => p.ImageAssets)
       .HasForeignKey(a => a.AccessionNumber)
         .OnDelete(DeleteBehavior.SetNull);
           });

        // ---------- AppUser ----------
        modelBuilder.Entity<AppUser>(e =>
     {
         e.ToTable("Users");
         e.HasKey(u => u.Id);
         e.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");
     });

        // ---------- Seed: Plant Records ----------
        modelBuilder.Entity<PlantRecord>().HasData(
                 new PlantRecord
                 {
                     AccessionNumber = "2024-0001",
                     PlantName = "Magnolia zenii",
                     Location = "Garden A - Section 3",
                     CollectionTrip = "Spring 2024 East Asia",
                     UpdatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                 },
                 new PlantRecord
                 {
                     AccessionNumber = "2024-0002",
                     PlantName = "Quercus alba",
                     Location = "Garden B - Oak Collection",
                     CollectionTrip = "Fall 2023 Appalachian",
                     UpdatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                 },
             new PlantRecord
             {
                 AccessionNumber = "2024-0003",
                 PlantName = "Acer palmatum",
                 Location = "Garden C - Japanese Maples",
                 CollectionTrip = null,
                 UpdatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
             }
             );

        // ---------- Seed: Dev Users ----------
        // Passwords are BCrypt hashed at migration-generation time.
        // admin@photobase.dev / Admin123!
        // user@photobase.dev  / User123!
        modelBuilder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
                Email = "admin@photobase.dev",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
        new AppUser
        {
            Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
            Email = "user@photobase.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
            Role = "Internal",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }
        );
    }
}
