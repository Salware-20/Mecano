using Mecano.Entidad.DTOs.Administrador;
using Mecano.Entidad.DTOs.Perfil;

namespace Mecano.Logica.Interfaces
{
    /// <summary>
    /// Gestión del perfil del administrador autenticado: lectura de datos,
    /// actualización del correo de notificaciones y cambio de contraseña.
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
        Task<List<AdministradorListaDTO>> ListarAsync(bool incluirInactivos = true);
        Task<AdministradorListaDTO?> ObtenerPorIdAsync(int adminId);
        Task<int> CrearAsync(CrearAdministradorDTO dto, int creadoPorId);
        Task<bool> ActualizarAsync(ActualizarAdministradorDTO dto);
        Task<bool> DesactivarAsync(int adminId, int solicitanteId);
        Task<bool> ReactivarAsync(int adminId);
        Task<bool> ExisteEmailAsync(string email, int? excluirId = null);
    }
}
