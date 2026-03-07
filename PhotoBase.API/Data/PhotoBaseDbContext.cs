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

        // Seed a couple of sample plant records for demo/testing
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
    }
}
