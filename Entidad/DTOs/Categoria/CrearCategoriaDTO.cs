using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs.Categoria
{
    public class CrearCategoriaDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 3)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }
    }
}
