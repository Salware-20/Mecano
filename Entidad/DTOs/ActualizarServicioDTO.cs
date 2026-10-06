using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    public class ActualizarServicioDTO
    {
        // TODO: remove (no-op on value types)
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, MinimumLength = 3)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Range(5, 480, ErrorMessage = "La duración debe estar entre 5 y 480 minutos")]
        public int DuracionMinutos { get; set; }

        [Range(0, 999999, ErrorMessage = "El precio no puede ser negativo")]
        public decimal Precio { get; set; }

        public int? CategoriaId { get; set; }
    }
}
