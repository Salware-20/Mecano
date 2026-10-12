using Mecano.Entidad.DTOs.Comentario;

namespace Mecano.Logica.Interfaces;

public interface IComentarioService
{
    Task<List<ComentarioDTO>> ObtenerPorClienteAsync(int clienteId);
    Task<int> CrearAsync(CrearComentarioDTO dto);
    Task<bool> EliminarAsync(int comentarioId);
}
