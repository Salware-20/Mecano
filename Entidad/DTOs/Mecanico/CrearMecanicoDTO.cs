using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs.Mecanico
{
    /// <summary>
    /// DTO de escritura para crear un mecánico.
    /// StringLength está deliberadamente acotado a los límites del esquema
    /// (Nombre varchar(150), Email varchar(200), Cedula/Telefono varchar(20)).
    /// La normalización estricta (email a minúsculas, trim) ocurre en el servicio.
    /// </summary>
    public class CrearMecanicoDTO
    {
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

        /// <summary>
        /// int? para que [Required] dispare cuando el valor es null.
        /// El servicio usa dto.EspecialidadId!.Value una vez superada la validación.
        /// </summary>
        [Required(ErrorMessage = "La especialidad es obligatoria")]
        public int? EspecialidadId { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(200, MinimumLength = 6,
            ErrorMessage = "La contraseña debe tener entre 6 y 200 caracteres")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// [Compare] cubre el caso "ambas llenas pero distintas".
        /// El caso "Password llena, ConfirmarPassword vacía" lo valida el servicio.
        /// </summary>
        [Required(ErrorMessage = "Confirme la contraseña")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
