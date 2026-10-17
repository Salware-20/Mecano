using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Utilidades;

namespace Mecano.Entidad.DTOs.Cliente
{
    /// <summary>
    /// DTO de escritura para actualizar un cliente. Las reglas de validación son
    /// espejo de <see cref="CrearClienteDTO"/> para que crear y editar apliquen las
    /// mismas regulaciones (nombre, teléfono obligatorio y correo con extensión).
    /// </summary>
    public class ActualizarClienteDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [StringLength(20, MinimumLength = 1)]
        public string CedulaIdentidad { get; set; } = string.Empty;

        public Mecano.Entidad.Constantes.TipoIdentificacion TipoIdentificacion { get; set; } = Mecano.Entidad.Constantes.TipoIdentificacion.Nacional;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, MinimumLength = 3)]
        [RegularExpression(Patrones.Nombre, ErrorMessage = Patrones.MensajeNombre)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = Patrones.MensajeTelefonoRequerido)]
        [Phone(ErrorMessage = Patrones.MensajeTelefonoFormato)]
        [StringLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
        [RegularExpression(Patrones.Correo, ErrorMessage = Patrones.MensajeCorreo)]
        [StringLength(200)]
        public string? Correo { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }
    }
}
