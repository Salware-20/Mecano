using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Constantes;

namespace Mecano.Entidad.Clases
{
    /// <summary>
    /// Represents a client of the auto repair shop.
    /// Clients do NOT have login accounts; they contact the shop via phone/WhatsApp.
    /// </summary>
    public class Cliente
    {
        public int ClienteId { get; set; }

        /// <summary>Costa Rican national ID (e.g. "101010101"). Unique and indexed for fast lookup.</summary>
        [Required]
        [MaxLength(20)]
        public string CedulaIdentidad { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento de identidad. Fase 1 solo usa <see cref="TipoIdentificacion.Nacional"/>;
        /// el índice único es compuesto (CedulaIdentidad + TipoIdentificacion) para que la
        /// Fase 2 coexista DIMEX y Pasaporte sin volver a tocar el esquema.
        /// </summary>
        public TipoIdentificacion TipoIdentificacion { get; set; } = TipoIdentificacion.Nacional;

        [Required]
        [MaxLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>Phone number stored as string to preserve formatting (e.g. "88880000").</summary>
        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        public string? Correo { get; set; }

        [MaxLength(300)]
        public string? Direccion { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        // Navigation properties
        public ICollection<Cita> Citas { get; } = new List<Cita>();
        public ICollection<Vehiculo> Vehiculos { get; } = new List<Vehiculo>();
    }
}
