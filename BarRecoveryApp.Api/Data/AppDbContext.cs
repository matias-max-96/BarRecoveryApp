using BarRecoveryApp.Api.Entities;
using Microsoft.EntityFrameworkCore;

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
        }
    }
}
