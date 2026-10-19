using Mecano.Entidad.Constantes;
using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.Clases
{
    /// <summary>
    /// Represents an administrator of the system.
    /// Administrators are the ONLY users who can create, reschedule, or cancel appointments.
    /// </summary>
    public class Administrador
    {
        public int AdministradorId { get; set; }

        /// <summary>Costa Rican national ID stored as a string (optional, for records).</summary>
        [MaxLength(20)]
        public string? Cedula { get; set; }

        public TipoIdentificacion TipoIdentificacion { get; set; } = TipoIdentificacion.Nacional;

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Correo secundario para recibir notificaciones (opcional). Es la única vía de
        /// aviso cuando el administrador está fuera, así que se mantiene único entre
        /// administradores: un mismo buzón no puede pertenecer a dos cuentas.
        /// </summary>
        [MaxLength(200)]
        public string? CorreoNotificaciones { get; set; }

        [Required]
        public string HashPassword { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;
        public bool EsAdminGlobal { get; set; }
    }
}
