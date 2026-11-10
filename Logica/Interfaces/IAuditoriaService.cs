using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Data;
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
            string? usuarioNombre = null,
            string? usuarioRol = null);

        /// <summary>
        /// Agrega la entrada de auditoría al <paramref name="context"/> recibido sin llamar a
        /// <c>SaveChanges</c>: la operación de negocio y su auditoría se confirman juntas o
        /// ninguna. Completa la identidad desde los claims cuando el llamador no la envía.
        /// </summary>
        Task AgregarEnContextoAsync(
            MySQLDBContext context,
            string tipoAccion,
            string entidadAfectada,
            string? entidadId,
            string detalles,
            string? usuarioId = null,
            string? usuarioNombre = null,
            string? usuarioRol = null);

        /// <summary>Devuelve una página de logs ordenados por fecha descendente y el total para paginación.</summary>
        Task<(List<AuditoriaLogDTO> Items, int Total)> ObtenerPaginadoAsync(
            FiltroAuditoriaDTO filtro,
            int pagina,
            int tamanioPagina);

        /// <summary>Devuelve el CorreoNotificaciones del Admin Global, o null si no está configurado.</summary>
        Task<string?> ObtenerCorreoOwnerAsync();
    }
}
