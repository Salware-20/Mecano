using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Utilidades;

namespace Mecano.Entidad.DTOs.Perfil
{
    /// <summary>
    /// Modelo de escritura para actualizar los datos editables del perfil.
    /// Se pueden cambiar el nombre, el tipo y el número de identificación y el
    /// correo de notificaciones (pensado para cuando el puesto de administrador
    /// global pasa a otra persona).
    /// El correo de inicio de sesión (Correo) no es editable: es la clave de
    /// autenticación y cambiarlo invalidaría el login del administrador.
    /// </summary>
    public sealed class ActualizarPerfilAdminDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres")]
        [RegularExpression(Patrones.Nombre, ErrorMessage = Patrones.MensajeNombre)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Identificación obligatoria. La normalización estricta por tipo
        /// (Nacional 9 dígitos, DIMEX 11-12, Pasaporte hasta 20) la aplica el
        /// servicio con <c>Formatos.NormalizarIdentificacion</c>, que lanza
        /// <see cref="InvalidOperationException"/> con el mensaje concreto.
        /// </summary>
        [Required(ErrorMessage = "La identificación es obligatoria")]
        [StringLength(20, ErrorMessage = "La identificación no puede superar 20 caracteres")]
        public string? Cedula { get; set; }

        public Mecano.Entidad.Constantes.TipoIdentificacion TipoIdentificacion { get; set; } = Mecano.Entidad.Constantes.TipoIdentificacion.Nacional;

        [Required(ErrorMessage = Patrones.MensajeTelefonoRequerido)]
        [Phone(ErrorMessage = Patrones.MensajeTelefonoFormato)]
        [StringLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres")]
        public string? Telefono { get; set; }

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
