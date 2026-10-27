using HEMS.Application.Interfaces;
using HEMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HEMS.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<HourMeterLog> HourMeterLogs => Set<HourMeterLog>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<WorkOrderPart> WorkOrderParts => Set<WorkOrderPart>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Konfigurasi Equipments
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EquipmentCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.EquipmentCode).IsUnique();
            entity.Property(e => e.SerialNumber).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.SerialNumber).IsUnique();
            entity.Property(e => e.Brand).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CurrentHourMeter).HasPrecision(10, 2);
            entity.Property(e => e.LastServiceHourMeter).HasPrecision(10, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.SiteLocation).HasMaxLength(50).IsRequired();
        });

        // 2. Konfigurasi HourMeterLogs
        modelBuilder.Entity<HourMeterLog>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.PreviousHM).HasPrecision(10, 2);
            entity.Property(h => h.CurrentHM).HasPrecision(10, 2);
            entity.Property(h => h.RecordedBy).HasMaxLength(50).IsRequired();

            entity.HasOne(h => h.Equipment)
                  .WithMany(e => e.HourMeterLogs)
                  .HasForeignKey(h => h.EquipmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Konfigurasi WorkOrders
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.WorkOrderNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(w => w.WorkOrderNumber).IsUnique();
            entity.Property(w => w.MaintenanceType).HasConversion<string>().HasMaxLength(20);
            entity.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(w => w.ProblemDescription).HasMaxLength(500);
            entity.Property(w => w.AssignedMechanic).HasMaxLength(100);

            entity.HasOne(w => w.Equipment)
                  .WithMany(e => e.WorkOrders)
                  .HasForeignKey(w => w.EquipmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 4. Konfigurasi SpareParts
        modelBuilder.Entity<SparePart>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.PartNumber).HasMaxLength(50).IsRequired();
            entity.HasIndex(s => s.PartNumber).IsUnique();
            entity.Property(s => s.PartName).HasMaxLength(150).IsRequired();
            entity.Property(s => s.UnitPrice).HasPrecision(18, 2);
        });

        // 5. Konfigurasi WorkOrderParts (Many-to-Many Pivot Table dengan data tambahan)
        modelBuilder.Entity<WorkOrderPart>(entity =>
        {
            entity.HasKey(wp => wp.Id);
            entity.Property(wp => wp.UnitPriceAtTime).HasPrecision(18, 2);

            entity.HasOne(wp => wp.WorkOrder)
                  .WithMany(w => w.WorkOrderParts)
                  .HasForeignKey(wp => wp.WorkOrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(wp => wp.SparePart)
                  .WithMany(s => s.WorkOrderParts)
                  .HasForeignKey(wp => wp.SparePartId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
