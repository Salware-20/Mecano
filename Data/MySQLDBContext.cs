using Mecano.Entidad.Clases;
using Microsoft.EntityFrameworkCore;

namespace Mecano.Data
{
    public class MySQLDBContext : DbContext
    {
        public MySQLDBContext(DbContextOptions<MySQLDBContext>options): base(options) { }
        public DbSet<Administrador> administradors { get; set; }
        public DbSet<Categoria> categorias { get; set; }
        public DbSet<Cita> cita { get; set; }
        public DbSet<Cliente> cliente { get; set; }
        public DbSet<Mecanico> mecanico { get; set; }
        public DbSet<NotificacionLog> notificacionLog { get; set; }
        public DbSet<Servicio> servicio { get; set; }
        public DbSet<Vehiculo> vehiculo { get; set; }

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

            // Mecanico - Cita: One mechanic has many appointments
            modelBuilder.Entity<Mecanico>()
                .HasMany(m => m.Citas)
                .WithOne(ci => ci.Mecanico)
                .HasForeignKey(ci => ci.MecanicoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Servicio - Cita: One service has many appointments
            modelBuilder.Entity<Servicio>()
                .HasMany(s => s.Citas)
                .WithOne(ci => ci.Servicio)
                .HasForeignKey(ci => ci.ServicioId)
                .OnDelete(DeleteBehavior.SetNull);

            // Categoria - Servicio: One category has many services
            modelBuilder.Entity<Categoria>()
                .HasMany(c => c.servicios)
                .WithOne(s => s.Categoria)
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cita - NotificacionLog: One appointment has many notification logs
            modelBuilder.Entity<Cita>()
                .HasMany(c => c.Notificaciones)
                .WithOne(nl => nl.Cita)
                .HasForeignKey(nl => nl.CitaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
