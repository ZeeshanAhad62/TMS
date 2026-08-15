using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Data;

public class FleetDbContext : DbContext
{
    public FleetDbContext(DbContextOptions<FleetDbContext> options) : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleDocument> VehicleDocuments => Set<VehicleDocument>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Tyre> Tyres => Set<Tyre>();
    public DbSet<TyreReplacementHistory> TyreReplacementHistories => Set<TyreReplacementHistory>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<BookingRecord> BookingRecords => Set<BookingRecord>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<CompanyProfile>().ToTable("CompanyProfile");

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(v => v.VehicleCode).IsUnique();
            entity.HasIndex(v => v.RegistrationNumber).IsUnique();
            entity.Property(v => v.LoadCapacity).HasPrecision(18, 2);
            entity.Property(v => v.ContainerLiftCapacity).HasPrecision(18, 2);
            entity.Property(v => v.CurrentOdometerReading).HasPrecision(18, 2);
            entity.Property(v => v.FuelConsumptionAverage).HasPrecision(18, 2);
            entity.Property(v => v.LastOilChangeOdometer).HasPrecision(18, 2);
            entity.Property(v => v.NextOilChangeDueOdometer).HasPrecision(18, 2);
            entity.Property(v => v.ServiceIntervalKm).HasPrecision(18, 2);
            entity.Property(v => v.PurchasePrice).HasPrecision(18, 2);
            entity.Property(v => v.RunningCostPerKm).HasPrecision(18, 2);
            entity.Property(v => v.FuelCostTracking).HasPrecision(18, 2);

            entity.HasMany(v => v.Documents)
                .WithOne(d => d.Vehicle)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.AlertRules)
                .WithOne(a => a.Vehicle)
                .HasForeignKey(a => a.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.Tyres)
                .WithOne(t => t.Vehicle)
                .HasForeignKey(t => t.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.MaintenanceRecords)
                .WithOne(m => m.Vehicle)
                .HasForeignKey(m => m.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.BookingRecords)
                .WithOne(b => b.Vehicle)
                .HasForeignKey(b => b.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tyre>(entity =>
        {
            entity.Property(t => t.InstallationOdometer).HasPrecision(18, 2);

            entity.HasMany(t => t.ReplacementHistory)
                .WithOne(r => r.Tyre)
                .HasForeignKey(r => r.TyreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TyreReplacementHistory>(entity =>
        {
            entity.Property(r => r.OdometerAtReplacement).HasPrecision(18, 2);
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.Property(m => m.Odometer).HasPrecision(18, 2);
            entity.Property(m => m.Cost).HasPrecision(18, 2);
        });
    }
}
