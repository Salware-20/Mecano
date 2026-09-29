using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.Clases
{
    public enum Especialidad { General, Llantas, Aceite, Motor }

    /// <summary>
    /// Represents a mechanic employed at the shop.
    /// Mechanics have login accounts with read-only access to their assigned appointments.
    /// </summary>
    public class Mecanico
    {
        public int MecanicoId { get; set; }

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

        public Especialidad Especialidad { get; set; } = Especialidad.General;

        public bool Activo { get; set; } = true;

        // Navigation properties
        public ICollection<Cita> Citas { get; } = new List<Cita>();
    }
}
