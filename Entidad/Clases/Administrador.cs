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

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Hashed password (ASP.NET Identity format). Never store plain text.</summary>
        [Required]
        public string HashPassword { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;
    }
}
