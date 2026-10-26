using Mecano.Entidad.DTOs.Administrador;
using Mecano.Entidad.DTOs.Perfil;

namespace Mecano.Logica.Interfaces
{
    /// <summary>
    /// Servicio de administradores: perfil del administrador autenticado
    /// (correo de notificaciones y contraseña) y gestión de staff por parte
    /// del Admin Global. El correo corporativo se compone como
    /// <c>alias@mecano.cr</c>; el correo de notificaciones es personal y solo
    /// se escribe desde /perfil.
    /// </summary>
    public interface IAdministradorService
    {
        /// <summary>
        /// Devuelve el perfil del administrador indicado, o null si no existe.
        /// </summary>
        Task<PerfilAdminDTO?> ObtenerPerfilAsync(int adminId);

        /// <summary>
        /// Actualiza los datos editables del perfil (correo de notificaciones).
        /// Devuelve false si el administrador no existe.
        /// Lanza <see cref="InvalidOperationException"/> si el correo de notificaciones
        /// ya está en uso por otro administrador.
        /// </summary>
        Task<bool> ActualizarPerfilAsync(int adminId, ActualizarPerfilAdminDTO dto);

        /// <summary>
        /// Cambia la contraseña del administrador tras validar la actual.
        /// Devuelve false si el administrador no existe.
        /// Lanza <see cref="InvalidOperationException"/> si la contraseña actual no
        /// coincide o si la nueva es igual a la actual.
        /// </summary>
        Task<bool> CambiarPasswordAsync(int adminId, CambiarPasswordDTO dto);

        // Gestión de otros administradores (solo Global Admin)

        /// <summary>
        /// Lista los administradores para la pantalla de gestión. No expone el
        /// correo de notificaciones, que es personal.
        /// </summary>
        Task<List<AdministradorListaDTO>> ListarAsync(bool incluirInactivos = true);

        /// <summary>Devuelve un administrador por id, sin su correo de notificaciones.</summary>
        Task<AdministradorListaDTO?> ObtenerPorIdAsync(int adminId);

        /// <summary>
        /// Crea un administrador staff. El correo de acceso se compone como
        /// <c>{AliasCorreo}@mecano.cr</c>; el correo de notificaciones queda vacío
        /// y solo el propio administrador puede configurarlo desde /perfil.
        /// </summary>
        Task<int> CrearAsync(CrearAdministradorDTO dto, int creadoPorId);

        /// <summary>
        /// Actualiza los datos editables de un administrador staff. No modifica
        /// el correo de acceso ni el correo de notificaciones.
        /// </summary>
        Task<bool> ActualizarAsync(ActualizarAdministradorDTO dto);
        Task<bool> DesactivarAsync(int adminId, int solicitanteId);
        Task<bool> ReactivarAsync(int adminId);
        Task<bool> ExisteEmailAsync(string email, int? excluirId = null);

        /// <summary>
        /// Elimina un administrador si no cuenta con auditorías registradas.
        /// Lanza <see cref="InvalidOperationException"/> si tiene registros de auditoría asociados.
        /// </summary>
        Task DeleteAsync(int id);
    }
}
