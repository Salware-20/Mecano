using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Constantes;

namespace Mecano.Entidad.DTOs.Comentario
{
    public class CrearComentarioDTO
    {
        [Range(1, int.MaxValue)]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "El comentario no puede estar vacío")]
        [StringLength(1000, MinimumLength = 1)]
        public string Texto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El autor es obligatorio")]
        [StringLength(150, MinimumLength = 1)]
        public string AutorNombre { get; set; } = string.Empty;

        public TipoAutorComentario AutorTipo { get; set; }
    }
}
