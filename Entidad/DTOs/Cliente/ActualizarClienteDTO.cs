using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs.Cliente
{
    public class ActualizarClienteDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [StringLength(20, MinimumLength = 1)]
        public string CedulaIdentidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, MinimumLength = 3)]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
        [StringLength(200)]
        public string? Correo { get; set; }

        [StringLength(300)]
        public string? Direccion { get; set; }
    }
}
