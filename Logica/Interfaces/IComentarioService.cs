using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs.Comentario;

namespace Mecano.Logica.Interfaces;

public interface IComentarioService
{
    Task<List<ComentarioDTO>> ObtenerPorClienteAsync(int clienteId);
    Task<int> CrearAsync(CrearComentarioDTO dto);

    /// <summary>
    /// Elimina un comentario. Solo puede hacerlo el autor (debe coincidir
    /// nombre y rol del solicitante con los guardados).
    /// </summary>
    Task<bool> EliminarAsync(int comentarioId, string solicitanteNombre, TipoAutorComentario solicitanteTipo);
}
