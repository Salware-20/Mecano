using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mecano.Entidad.Clases
{
    /// <summary>
    /// Represents a service offered by the shop (e.g., oil change, alignment).
    /// </summary>
    public class Servicio
    {
        public int ServicioId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        /// <summary>Estimated duration of the service in minutes.</summary>
        public int DuracionMinutos { get; set; }

        public bool Activo { get; set; } = true;

        // Foreign key to Categoria (optional)
        public int? CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        // Navigation properties
        public ICollection<Cita> Citas { get; } = new List<Cita>();
    }
}
