using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Utilidades;

namespace Mecano.Entidad.DTOs.Perfil
{
    /// <summary>
    /// Modelo de escritura para actualizar los datos editables del perfil.
    /// El correo de inicio de sesión (Correo) no es editable: es la clave de
    /// autenticación y cambiarlo invalidaría el login del administrador.
    /// </summary>
    public sealed class ActualizarPerfilAdminDTO
    {
        /// <summary>
        /// Correo secundario de notificaciones. Opcional: vacío lo borra.
        /// La unicidad entre administradores la valida el servicio, no el DTO
        /// (requiere consultar la base de datos).
        /// </summary>
        /// <remarks>
        /// No se usa <c>EmailAddressAttribute</c> a propósito: considera inválida la
        /// cadena vacía y bloquearía el borrado del correo (el campo es opcional).
        /// <see cref="Patrones.Correo"/> ya es más estricto que ese atributo.
        /// </remarks>
        [StringLength(200, ErrorMessage = "El correo no puede superar 200 caracteres")]
        [RegularExpression(Patrones.Correo, ErrorMessage = Patrones.MensajeCorreo)]
        public string? CorreoNotificaciones { get; set; }
    }
}
