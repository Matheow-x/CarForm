using System;
using System.Threading;
using System.Threading.Tasks;
using CarForm.Models;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<SalesDocument> SalesDocuments => Set<SalesDocument>();
    public DbSet<DocumentLineItem> DocumentLineItems => Set<DocumentLineItem>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- Company ----------------
        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("Companies");
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Address).HasMaxLength(500);
        });

        // ---------------- Owner ----------------
        modelBuilder.Entity<Owner>(b =>
        {
            b.ToTable("Owners");
            b.Property(x => x.Type).HasConversion<int>();
            b.Property(x => x.FirstName).HasMaxLength(120);
            b.Property(x => x.LastName).HasMaxLength(120);
            b.Property(x => x.NationalCode).HasMaxLength(32);
            b.Property(x => x.CompanyName).HasMaxLength(200);
            b.HasIndex(x => x.NationalCode);
            b.HasIndex(x => x.CompanyName);
        });

        // ---------------- Vehicle catalogue ----------------
        modelBuilder.Entity<VehicleMake>(b =>
        {
            b.ToTable("VehicleMakes");
            b.Property(x => x.Name).IsRequired().HasMaxLength(120);
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<VehicleModel>(b =>
        {
            b.ToTable("VehicleModels");
            b.Property(x => x.Name).IsRequired().HasMaxLength(120);
            b.HasOne(x => x.Make)
                .WithMany(x => x.Models)
                .HasForeignKey(x => x.MakeId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.MakeId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Vehicle>(b =>
        {
            b.ToTable("Vehicles");
            b.Property(x => x.Vin).HasMaxLength(64);
            b.Property(x => x.PlateNumber).HasMaxLength(32);
            b.Property(x => x.Color).HasMaxLength(64);
            b.HasOne(x => x.Model)
                .WithMany(x => x.Vehicles)
                .HasForeignKey(x => x.ModelId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.Vin);
            b.HasIndex(x => x.ModelId);
        });

        // ---------------- Documents ----------------
        modelBuilder.Entity<SalesDocument>(b =>
        {
            b.ToTable("SalesDocuments");
            b.Property(x => x.DocumentNumber).IsRequired().HasMaxLength(64);
            b.Property(x => x.BuyerType).HasConversion<int>();
            b.HasIndex(x => x.DocumentNumber);
            b.HasIndex(x => x.DocumentDate);
            b.HasIndex(x => x.BuyerName);
            b.HasOne<Vehicle>()
                .WithMany()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne<Owner>()
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocumentLineItem>(b =>
        {
            b.ToTable("DocumentLineItems");
            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Amount).HasColumnType("TEXT");
            b.HasOne(x => x.Document)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.SalesDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.SalesDocumentId);
        });

        // ---------------- Audit ----------------
        modelBuilder.Entity<AuditEntry>(b =>
        {
            b.ToTable("AuditEntries");
            b.Property(x => x.EntityType).HasConversion<int>();
            b.Property(x => x.Action).HasConversion<int>();
            b.Property(x => x.Summary).IsRequired().HasMaxLength(500);
            b.HasIndex(x => x.Timestamp);
            b.HasIndex(x => x.EntityType);
        });

        // ---------------- Settings ----------------
        modelBuilder.Entity<Setting>(b =>
        {
            b.ToTable("Settings");
            b.HasKey(x => x.Key);
            b.Property(x => x.Key).HasMaxLength(100);
        });
    }

    /// <summary>Keeps CreatedAt / UpdatedAt in sync for tracked entities.</summary>
    public override int SaveChanges()
    {
        Stamp();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Stamp();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void Stamp()
    {
        var now = DateTime.Now;
        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case SalesDocument document:
                    if (entry.State == EntityState.Added) document.CreatedAt = now;
                    document.UpdatedAt = now;
                    break;
                case Owner owner:
                    if (entry.State == EntityState.Added) owner.CreatedAt = now;
                    owner.UpdatedAt = now;
                    break;
                case Vehicle vehicle:
                    if (entry.State == EntityState.Added) vehicle.CreatedAt = now;
                    vehicle.UpdatedAt = now;
                    break;
                case Company company:
                    company.UpdatedAt = now;
                    break;
            }
        }
    }
}
