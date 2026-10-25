using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.Utilidades;

namespace Mecano.Entidad.DTOs.Administrador
{
    public class CrearAdministradorDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150)]
        [RegularExpression(Patrones.Nombre, ErrorMessage = Patrones.MensajeNombre)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(200)]
        [RegularExpression(Patrones.Correo, ErrorMessage = Patrones.MensajeCorreo)]
        public string Email { get; set; } = string.Empty;

        public TipoIdentificacion TipoIdentificacion { get; set; } = TipoIdentificacion.Nacional;

        [StringLength(20)]
        public string? Cedula { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(200)]
        [RegularExpression(Patrones.Correo, ErrorMessage = Patrones.MensajeCorreo)]
        public string? CorreoNotificaciones { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contraseña debe contener mayúsculas, minúsculas y números.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmar contraseña es obligatorio.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
