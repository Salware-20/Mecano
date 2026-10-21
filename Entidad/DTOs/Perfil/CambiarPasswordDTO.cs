using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs.Perfil
{
    /// <summary>
    /// Modelo de escritura para cambiar la contraseña del administrador autenticado.
    /// La regla de complejidad replica la de <c>RegistroAdminDTO</c>, pero con el
    /// patrón corregido: <c>RegularExpressionAttribute</c> exige coincidencia
    /// completa, así que unos lookaheads sin <c>.+$</c> nunca validarían nada.
    /// </summary>
    public sealed class CambiarPasswordDTO
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        public string PasswordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe contener mayúsculas, minúsculas y números")]
        public string NuevaPassword { get; set; } = string.Empty;

        [Compare(nameof(NuevaPassword), ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
