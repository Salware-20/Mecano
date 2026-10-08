using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    public sealed class RegistroAdminDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El email no es válido")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [RegularExpression(@"(?=.*[a-z])(?=.*[A-Z])(?=.*\d)",
            ErrorMessage = "La contraseña debe contener mayúsculas, minúsculas y números")]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
