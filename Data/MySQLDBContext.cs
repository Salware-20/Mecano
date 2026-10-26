using Mecano.Entidad.Clases;
using Microsoft.EntityFrameworkCore;

namespace Mecano.Data
{
    public class MySQLDBContext : DbContext
    {
        public MySQLDBContext(DbContextOptions<MySQLDBContext> options) : base(options) { }

        public DbSet<Administrador> Administradors { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Cita> Cita { get; set; }
        public DbSet<CitaMecanico> CitaMecanico { get; set; }
        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Comentario> Comentario { get; set; }
        public DbSet<Mecanico> Mecanico { get; set; }
        public DbSet<NotificacionLog> NotificacionLog { get; set; }
        public DbSet<Servicio> Servicio { get; set; }
        public DbSet<Vehiculo> Vehiculo { get; set; }
        public DbSet<AuditoriaLog> AuditoriaLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cliente - Cita: One client has many appointments
            modelBuilder.Entity<Cliente>()
                .HasMany(c => c.Citas)
                .WithOne(ci => ci.Cliente)
                .HasForeignKey(ci => ci.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cliente - Vehiculo: One client has many vehicles
            modelBuilder.Entity<Cliente>()
                .HasMany(c => c.Vehiculos)
                .WithOne(v => v.Cliente)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Vehiculo - Citas
            modelBuilder.Entity<Vehiculo>()
                .HasMany(v => v.Citas)
                .WithOne(c => c.Vehiculo)
                .HasForeignKey(c => c.VehiculoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mecanico - Cita (N:M a través de CitaMecanico): una cita puede tener
            // varios mecánicos de la misma especialidad. Al borrar la cita se borran
            // sus asignaciones; el mecánico no se puede borrar si todavía está asignado.
            modelBuilder.Entity<CitaMecanico>()
                .HasKey(cm => new { cm.CitaId, cm.MecanicoId });

            modelBuilder.Entity<CitaMecanico>()
                .HasOne(cm => cm.Cita)
                .WithMany(c => c.CitaMecanicos)
                .HasForeignKey(cm => cm.CitaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CitaMecanico>()
                .HasOne(cm => cm.Mecanico)
                .WithMany(m => m.CitaMecanicos)
                .HasForeignKey(cm => cm.MecanicoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mecanico - Especialidad (Categoria)
            modelBuilder.Entity<Mecanico>()
                .HasOne(m => m.Especialidad)
                .WithMany(c => c.Mecanicos)
                .HasForeignKey(m => m.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict);

            // Servicio - Cita
            modelBuilder.Entity<Servicio>()
                .HasMany(s => s.Citas)
                .WithOne(ci => ci.Servicio)
                .HasForeignKey(ci => ci.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Categoria - Servicio
            modelBuilder.Entity<Categoria>()
                .HasMany(c => c.servicios)
                .WithOne(s => s.Categoria)
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cita - NotificacionLog
            modelBuilder.Entity<Cita>()
                .HasMany(c => c.Notificaciones)
                .WithOne(nl => nl.Cita)
                .HasForeignKey(nl => nl.CitaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Auditoría: consultas por fecha y por entidad afectada.
            modelBuilder.Entity<AuditoriaLog>()
                .HasIndex(l => l.FechaHora);

            modelBuilder.Entity<AuditoriaLog>()
                .HasIndex(l => new { l.EntidadAfectada, l.EntidadId });

            // Cliente - Comentario: un cliente tiene muchos comentarios
            modelBuilder.Entity<Cliente>()
                .HasMany(c => c.Comentarios)
                .WithOne(cm => cm.Cliente)
                .HasForeignKey(cm => cm.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comentario>()
                .HasIndex(cm => cm.ClienteId);

            // Unique index on Cliente: la identidad solo es única dentro de su tipo de documento.
            // Fase 1 solo usa Nacional, pero el índice compuesto evita tener que migrar en Fase 2.
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => new { c.CedulaIdentidad, c.TipoIdentificacion })
                .IsUnique();

            // Unique index on Vehiculo.Placa
            modelBuilder.Entity<Vehiculo>()
                .HasIndex(v => v.Placa)
                .IsUnique();
        }
    }
}
