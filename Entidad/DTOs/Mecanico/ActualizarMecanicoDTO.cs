using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs.Mecanico
{
    /// <summary>
    /// DTO de escritura para actualizar un mecánico existente.
    /// Password y ConfirmarPassword son opcionales: si se dejan vacíos, el servicio
    /// conserva el hash actual sin modificarlo.
    /// </summary>
    public class ActualizarMecanicoDTO
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [StringLength(200, ErrorMessage = "El correo no puede superar 200 caracteres")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "La cédula no puede superar 20 caracteres")]
        public string? Cedula { get; set; }

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
