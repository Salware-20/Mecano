using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Constantes;

namespace Mecano.Entidad.Clases
{
    /// <summary>
    /// Nota interna adjunta a la ficha de un cliente.
    /// El autor es el Administrador o Mecánico logueado que la escribió;
    /// se guarda su NOMBRE como instantánea (no se resuelve luego por Id).
    /// </summary>
    public class Comentario
    {
        public int ComentarioId { get; set; }

        // Cliente al que pertenece el comentario (la ficha donde se muestra).
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Texto { get; set; } = string.Empty;

        public DateTime FechaHora { get; set; } = DateTime.Now;

        // Nombre del usuario logueado al escribir el comentario.
        [Required]
        [MaxLength(150)]
        public string AutorNombre { get; set; } = string.Empty;

        // Rol del autor al escribir (Administrador o Mecánico).
        public TipoAutorComentario AutorTipo { get; set; }

        // FUTURO: cuando agendar citas esté completo, permitir enlazar el
        // comentario a una cita concreta del cliente.
        // public int? CitaId { get; set; }
        // public Cita? Cita { get; set; }
    }
}
