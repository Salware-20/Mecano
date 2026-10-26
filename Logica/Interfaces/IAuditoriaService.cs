using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.DTOs.Auditoria;

namespace Mecano.Logica.Interfaces
{
    public interface IAuditoriaService
    {
        /// <summary>Registra una acción crítica. Los errores de BD se absorben y no bloquean la operación principal.</summary>
        Task RegistrarAsync(
            string tipoAccion,
            string entidadAfectada,
            string? entidadId,
            string detalles,
            string? usuarioId = null,
            string? usuarioNombre = null);

        /// <summary>Devuelve una página de logs ordenados por fecha descendente y el total para paginación.</summary>
        Task<(List<AuditoriaLogDTO> Items, int Total)> ObtenerPaginadoAsync(
            FiltroAuditoriaDTO filtro,
            int pagina,
            int tamanioPagina);

        /// <summary>Devuelve el CorreoNotificaciones del Admin Global, o null si no está configurado.</summary>
        Task<string?> ObtenerCorreoOwnerAsync();
    }
}
