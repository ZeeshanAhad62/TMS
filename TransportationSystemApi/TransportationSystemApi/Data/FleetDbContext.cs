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
    public DbSet<User> Users => Set<User>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<DriverDocument> DriverDocuments => Set<DriverDocument>();
    public DbSet<DriverVehicleAssignment> DriverVehicleAssignments => Set<DriverVehicleAssignment>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderItem> WorkOrderItems => Set<WorkOrderItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<FuelEntry> FuelEntries => Set<FuelEntry>();
    public DbSet<TripExpense> TripExpenses => Set<TripExpense>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AlertConfig> AlertConfigs => Set<AlertConfig>();
    public DbSet<AlertLog> AlertLog => Set<AlertLog>();
    public DbSet<TyreEvent> TyreEvents => Set<TyreEvent>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<CompanyProfile>().ToTable("CompanyProfile");

        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.ToTable("LoginHistory");
            entity.HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

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
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(v => v.MaintenanceRecords)
                .WithOne(m => m.Vehicle)
                .HasForeignKey(m => m.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tyre>(entity =>
        {
            entity.Property(t => t.InstallationOdometer).HasPrecision(18, 2);
            entity.Property(t => t.PurchaseCost).HasPrecision(18, 2);
            entity.Property(t => t.TotalDistanceRunCarried).HasPrecision(18, 2);

            entity.HasMany(t => t.ReplacementHistory)
                .WithOne(r => r.Tyre)
                .HasForeignKey(r => r.TyreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.Events)
                .WithOne(e => e.Tyre)
                .HasForeignKey(e => e.TyreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TyreEvent>(entity =>
        {
            entity.Property(e => e.Odometer).HasPrecision(18, 2);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
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

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasIndex(d => d.DriverCode).IsUnique();
            entity.HasIndex(d => d.LicenseNumber).IsUnique();

            entity.HasMany(d => d.Documents)
                .WithOne(doc => doc.Driver)
                .HasForeignKey(doc => doc.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(d => d.Assignments)
                .WithOne(a => a.Driver)
                .HasForeignKey(a => a.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DriverVehicleAssignment>(entity =>
        {
            entity.HasOne(a => a.Vehicle)
                .WithMany()
                .HasForeignKey(a => a.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasIndex(t => t.TripCode).IsUnique();
            entity.Property(t => t.Revenue).HasPrecision(18, 2);

            entity.HasOne(t => t.Vehicle)
                .WithMany(v => v.Trips)
                .HasForeignKey(t => t.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Driver)
                .WithMany()
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(t => t.Expenses)
                .WithOne(e => e.Trip)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TripExpense>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(i => i.InvoiceNumber).IsUnique();
            entity.Property(i => i.TaxPercent).HasPrecision(9, 4);

            entity.HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(i => i.Lines)
                .WithOne(l => l.Invoice)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(i => i.Payments)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.Property(l => l.Quantity).HasPrecision(18, 2);
            entity.Property(l => l.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(l => l.TripId);
            // TripId is a soft link only -- no relationship / FK configured.
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasIndex(w => w.WorkOrderCode).IsUnique();
            entity.Property(w => w.Odometer).HasPrecision(18, 2);
            entity.Property(w => w.LabourCost).HasPrecision(18, 2);

            entity.HasOne(w => w.Vehicle)
                .WithMany(v => v.WorkOrders)
                .HasForeignKey(w => w.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(w => w.Items)
                .WithOne(i => i.WorkOrder)
                .HasForeignKey(i => i.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkOrderItem>(entity =>
        {
            entity.Property(i => i.Quantity).HasPrecision(18, 2);
            entity.Property(i => i.UnitCost).HasPrecision(18, 2);

            entity.HasOne(i => i.Part)
                .WithMany()
                .HasForeignKey(i => i.PartId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasIndex(p => p.PartNumber).IsUnique();
            entity.Property(p => p.ReorderLevel).HasPrecision(18, 2);
            entity.Property(p => p.StandardCost).HasPrecision(18, 2);

            entity.HasMany(p => p.Movements)
                .WithOne(m => m.Part)
                .HasForeignKey(m => m.PartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.Property(m => m.Quantity).HasPrecision(18, 2);
            entity.Property(m => m.UnitCost).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.CustomerCode).IsUnique();
            entity.Property(c => c.CreditLimit).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FuelEntry>(entity =>
        {
            entity.HasIndex(f => f.FuelEntryCode).IsUnique();
            entity.HasIndex(f => new { f.VehicleId, f.Date });
            entity.Property(f => f.OdometerReading).HasPrecision(18, 2);
            entity.Property(f => f.Litres).HasPrecision(18, 2);
            entity.Property(f => f.RatePerLitre).HasPrecision(18, 2);
            entity.Property(f => f.TotalCost).HasPrecision(18, 2);

            entity.HasOne(f => f.Vehicle)
                .WithMany()
                .HasForeignKey(f => f.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Driver)
                .WithMany()
                .HasForeignKey(f => f.DriverId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(f => f.Trip)
                .WithMany()
                .HasForeignKey(f => f.TripId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AlertLog>(entity =>
        {
            entity.HasIndex(l => new { l.EntityType, l.EntityId, l.DocumentType, l.ExpiryDate, l.Severity }).IsUnique();
        });
    }
}
