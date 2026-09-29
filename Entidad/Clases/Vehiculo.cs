using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.Clases
{
    /// <summary>
    /// Represents a vehicle registered to a client.
    /// License plate (Placa) has a unique index for fast admin lookup.
    /// </summary>
    public class Vehiculo
    {
        public int VehiculoId { get; set; }

        // Foreign key to Cliente
        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        /// <summary>License plate — unique, indexed for fast admin search.</summary>
        [Required]
        [MaxLength(20)]
        public string Placa { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Marca { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Modelo { get; set; } = string.Empty;

        public int Anio { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(500)]
        public string? Notas { get; set; }

        // Navigation properties
        public ICollection<Cita> Citas { get; } = new List<Cita>();
    }
}
