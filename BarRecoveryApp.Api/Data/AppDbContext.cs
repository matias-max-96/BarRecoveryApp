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

        // Fase 2+: agregar aquí DbSet<BarType>, DbSet<RecoveryWorkReport>, etc.
        // a medida que cada entidad entre a su fase del plan de sync.

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

            base.OnModelCreating(modelBuilder);

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