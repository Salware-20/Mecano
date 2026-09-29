using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.Clases
{
    /// <summary>
    /// Audit log for notifications sent (or attempted) for an appointment.
    /// </summary>
    public class NotificacionLog
    {
        public int NotificacionLogId { get; set; }

        // Foreign key to Cita
        public int CitaId { get; set; }
        public Cita? Cita { get; set; }

        [Required]
        [MaxLength(200)]
        public string DestinatarioCorreo { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Asunto { get; set; } = string.Empty;

        [Required]
        public string MensajeBody { get; set; } = string.Empty;

        public DateTime FechaEnvio { get; set; } = DateTime.Now;

        /// <summary>True if the notification was delivered successfully.</summary>
        public bool EnviadoExitosamente { get; set; }
    }
}
