using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Utilidades;

namespace Mecano.Entidad.DTOs.Mecanico
{
    /// <summary>
    /// DTO de escritura para actualizar un mecánico existente.
    /// En creación se usa <see cref="CrearMecanicoDTO"/>. Este DTO se usa en
    /// edición: el correo de acceso no se modifica; se muestra de solo lectura.
    /// Password y ConfirmarPassword son opcionales: si se dejan vacíos, el servicio
    /// conserva el hash actual sin modificarlo.
    /// </summary>
    public class ActualizarMecanicoDTO
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres")]
        [RegularExpression(Patrones.Nombre, ErrorMessage = Patrones.MensajeNombre)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Correo de acceso (solo lectura en UI). Se compone como alias@mecano.cr.
        /// </summary>
        [StringLength(200, ErrorMessage = "El correo no puede superar 200 caracteres")]
        public string? Email { get; set; }

        private string _aliasCorreo = string.Empty;

        /// <summary>
        /// Alias del correo corporativo. Solo se usa en creación a través del formulario.
        /// </summary>
        [StringLength(CorreoCorporativo.LargoMaximoAlias, ErrorMessage = "El alias no puede superar 64 caracteres.")]
        [RegularExpression(Patrones.AliasCorreo, ErrorMessage = Patrones.MensajeAliasCorreo)]
        public string AliasCorreo
        {
            get => _aliasCorreo;
            set => _aliasCorreo = CorreoCorporativo.NormalizarAlias(value);
        }

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(20, ErrorMessage = "La cédula no puede superar 20 caracteres")]
        public string? Cedula { get; set; }

        public Mecano.Entidad.Constantes.TipoIdentificacion TipoIdentificacion { get; set; } = Mecano.Entidad.Constantes.TipoIdentificacion.Nacional;

        [Required(ErrorMessage = Patrones.MensajeTelefonoRequerido)]
        [Phone(ErrorMessage = Patrones.MensajeTelefonoFormato)]
        [StringLength(20, ErrorMessage = "El teléfono no puede superar 20 caracteres")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "La especialidad es obligatoria")]
        public int? EspecialidadId { get; set; }

        /// <summary>
        /// Opcional en actualización. Vacío = no cambiar el hash almacenado.
        /// Si se provee, debe tener al menos 6 caracteres y ConfirmarPassword
        /// debe coincidir (validado por [Compare] en el DTO y por el servicio
        /// cuando ConfirmarPassword está vacío).
        /// </summary>
        [StringLength(200, MinimumLength = 6,
            ErrorMessage = "La contraseña debe tener entre 6 y 200 caracteres")]
        public string? Password { get; set; }

        /// <summary>
        /// [Compare] cubre el caso "ambas llenas pero distintas".
        /// El caso "Password llena, ConfirmarPassword vacía o nula" lo valida el servicio
        /// con InvalidOperationException, ya que [Compare] no dispara cuando Password es null.
        /// </summary>
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmarPassword { get; set; }
    }
}
