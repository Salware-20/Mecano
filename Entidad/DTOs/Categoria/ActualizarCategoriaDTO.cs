using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs.Categoria
{
    public class ActualizarCategoriaDTO
    {
        // TODO: remove (no-op on value types)
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 3)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }
    }
}
