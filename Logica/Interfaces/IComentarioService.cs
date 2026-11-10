using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs.Comentario;

namespace Mecano.Logica.Interfaces;

public interface IComentarioService
{
    Task<List<ComentarioDTO>> ObtenerPorClienteAsync(int clienteId);
    Task<int> CrearAsync(CrearComentarioDTO dto);

    /// <summary>
    /// Edita un comentario una única vez. Solo puede hacerlo el autor (debe coincidir
    /// nombre y rol del solicitante con los guardados). Devuelve <c>false</c> si no existe
    /// y lanza <see cref="InvalidOperationException"/> si ya fue editado antes.
    /// El texto anterior se registra en la auditoría.
    /// </summary>
    Task<bool> EditarAsync(int comentarioId, string nuevoTexto, string solicitanteNombre, TipoAutorComentario solicitanteTipo);

    /// <summary>
    /// Elimina un comentario. Solo puede hacerlo el autor (debe coincidir
    /// nombre y rol del solicitante con los guardados). El texto eliminado
    /// se registra en la auditoría.
    /// </summary>
    Task<bool> EliminarAsync(int comentarioId, string solicitanteNombre, TipoAutorComentario solicitanteTipo);
}
