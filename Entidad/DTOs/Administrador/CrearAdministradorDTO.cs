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

        private string _aliasCorreo = string.Empty;

        [Required(ErrorMessage = "El alias de correo es obligatorio.")]
        [StringLength(CorreoCorporativo.LargoMaximoAlias, ErrorMessage = "El alias no puede superar 64 caracteres.")]
        [RegularExpression(Patrones.AliasCorreo, ErrorMessage = Patrones.MensajeAliasCorreo)]
        public string AliasCorreo
        {
            get => _aliasCorreo;
            set => _aliasCorreo = CorreoCorporativo.NormalizarAlias(value);
        }

        public TipoIdentificacion TipoIdentificacion { get; set; } = TipoIdentificacion.Nacional;

        [StringLength(20)]
        public string? Cedula { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

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
