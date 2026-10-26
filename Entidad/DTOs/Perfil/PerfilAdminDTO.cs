using Mecano.Entidad.Constantes;

namespace Mecano.Entidad.DTOs.Perfil
{
    /// <summary>
    /// Modelo de lectura del perfil del administrador autenticado.
    /// </summary>
    public sealed class PerfilAdminDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public TipoIdentificacion TipoIdentificacion { get; set; } = TipoIdentificacion.Nacional;
        public string? Telefono { get; set; }

        /// <summary>Correo de inicio de sesión (clave de autenticación, no editable).</summary>
        public string Correo { get; set; } = string.Empty;

        /// <summary>Correo secundario de notificaciones (editable, opcional).</summary>
        public string? CorreoNotificaciones { get; set; }
    }
}
