using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.Clases
{
    public enum Estado { Pendiente, EnProceso, Finalizada, Cancelada }

    /// <summary>
    /// Represents an appointment. Only Administrators can create, reschedule, or cancel appointments.
    /// </summary>
    public class Cita
    {
        public int CitaId { get; set; }

        public DateTime FechaCita { get; set; }

        /// <summary>Stored as TimeSpan in MariaDB 10.4 via TimeOnlyConverter in MyDbContext.</summary>
        public TimeOnly HoraCita { get; set; }

        [MaxLength(1000)]
        public string? NotasAdmin { get; set; }

        public Estado Estado { get; set; } = Estado.Pendiente;

        // Foreign keys and navigation properties
        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public int? VehiculoId { get; set; }
        public Vehiculo? Vehiculo { get; set; }

        public int? MecanicoId { get; set; }
        public Mecanico? Mecanico { get; set; }

        public int? ServicioId { get; set; }
        public Servicio? Servicio { get; set; }

        // Notification logs for this appointment
        public ICollection<NotificacionLog> Notificaciones { get; } = new List<NotificacionLog>();
    }
}
