using Mecano.Entidad.Constantes;

namespace Mecano.Entidad.DTOs.Comentario
{
    public class ComentarioDTO
    {
        public int Id { get; set; }
        public string Texto { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string AutorNombre { get; set; } = string.Empty;
        public TipoAutorComentario AutorTipo { get; set; }
    }
}
