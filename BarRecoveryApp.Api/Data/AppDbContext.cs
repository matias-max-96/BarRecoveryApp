using BarRecoveryApp.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BarRecoveryApp.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Plant> Plants => Set<Plant>();

        public DbSet<RecoveryWorkReport> RecoveryWorkReports => Set<RecoveryWorkReport>();

        public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();

        public DbSet<Shipment> Shipments => Set<Shipment>();

        public DbSet<BarReturnReceipt> BarReturnReceipts => Set<BarReturnReceipt>();

        public DbSet<Bar> Bars => Set<Bar>();

        // Fase 4: agregar aquí DbSet<User> cuando llegue su turno.

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Plant>(entity =>
            {
                entity.ToTable("plants");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.HasIndex(x => x.Code).IsUnique();
                // Índice clave para el pull incremental: WHERE UpdatedAtUtc > @since
                entity.HasIndex(x => x.UpdatedAtUtc);
            });

            modelBuilder.Entity<RecoveryWorkReport>(entity =>
            {
                entity.ToTable("recovery_work_reports");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.UserId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.ShiftName).HasMaxLength(50);
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.HasIndex(x => x.UpdatedAtUtc);

                entity.HasMany(x => x.Categories)
                    .WithOne()
                    .HasForeignKey(x => x.RecoveryWorkReportId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecoveryWorkReportCategory>(entity =>
            {
                entity.ToTable("recovery_work_report_categories");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.PlantId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.BarTypeId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.ExportLabel).HasMaxLength(250);

                entity.HasMany(x => x.Activities)
                    .WithOne()
                    .HasForeignKey(x => x.RecoveryWorkReportCategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Supplies)
                    .WithOne()
                    .HasForeignKey(x => x.RecoveryWorkReportCategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecoveryWorkActivity>(entity =>
            {
                entity.ToTable("recovery_work_activities");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.ActivityId).HasMaxLength(36).IsRequired();
            });

            modelBuilder.Entity<RecoveryWorkSupply>(entity =>
            {
                entity.ToTable("recovery_work_supplies");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SupplyId).HasMaxLength(36).IsRequired();
            });

            modelBuilder.Entity<QualityInspection>(entity =>
            {
                entity.ToTable("quality_inspections");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.BarId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.InspectorUserId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.HasIndex(x => x.UpdatedAtUtc);

                entity.HasMany(x => x.AttributeValues)
                    .WithOne()
                    .HasForeignKey(x => x.QualityInspectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QualityInspectionAttributeValue>(entity =>
            {
                entity.ToTable("quality_inspection_attribute_values");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.BarId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.AttributeDefinitionId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.AttributeCode).HasMaxLength(50);
                entity.Property(x => x.AttributeName).HasMaxLength(150);
                entity.Property(x => x.UnitAtInspection).HasMaxLength(20);
                entity.Property(x => x.ToleranceTextAtInspection).HasMaxLength(200);
            });

            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.ToTable("shipments");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.TransferOrder).HasMaxLength(50).IsRequired();
                entity.Property(x => x.CustomerReference).HasMaxLength(150);
                entity.Property(x => x.DispatchGuideNumber).HasMaxLength(100);
                entity.Property(x => x.ResponsibleUserId).HasMaxLength(36).IsRequired();
                entity.HasIndex(x => x.UpdatedAtUtc);

                entity.HasMany(x => x.Bars)
                    .WithOne()
                    .HasForeignKey(x => x.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShipmentBar>(entity =>
            {
                entity.ToTable("shipment_bars");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.BarId).HasMaxLength(36).IsRequired();
            });

            modelBuilder.Entity<BarReturnReceipt>(entity =>
            {
                entity.ToTable("bar_return_receipts");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.ReturnDocument).HasMaxLength(100);
                entity.Property(x => x.ResponsibleUserId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.Notes).HasMaxLength(1000);
                entity.HasIndex(x => x.UpdatedAtUtc);

                entity.HasMany(x => x.Bars)
                    .WithOne()
                    .HasForeignKey(x => x.BarReturnReceiptId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BarReturnReceiptBar>(entity =>
            {
                entity.ToTable("bar_return_receipt_bars");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.BarId).HasMaxLength(36).IsRequired();
            });

            modelBuilder.Entity<Bar>(entity =>
            {
                entity.ToTable("bars");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.BarNumber).HasMaxLength(100).IsRequired();
                entity.Property(x => x.PlantId).HasMaxLength(36).IsRequired();
                entity.Property(x => x.BarTypeId).HasMaxLength(36).IsRequired();
                entity.HasIndex(x => x.UpdatedAtUtc);
                entity.HasIndex(x => new { x.PlantId, x.BarTypeId, x.BarNumber }).IsUnique();
            });

            base.OnModelCreating(modelBuilder);

            // Todos los DateTime del sistema representan instantes UTC por
            // convención (UpdatedAtUtc, CreatedAtUtc, etc.), pero SQLite (en
            // las tablets) y la deserialización JSON no preservan el
            // DateTimeKind — llegan como "Unspecified", y Npgsql rechaza
            // guardar/comparar eso contra una columna "timestamp with time
            // zone". Este converter fuerza Kind=Utc (sin desplazar el valor,
            // solo corrige la etiqueta) en toda propiedad DateTime de
            // cualquier entidad — se aplica tanto al guardar como al
            // traducir comparaciones en queries (ej. el filtro "since").
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableUtcConverter);
                    }
                }
            }
        }
    }
}