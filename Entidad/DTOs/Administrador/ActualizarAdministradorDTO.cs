using System.ComponentModel.DataAnnotations;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.Utilidades;

namespace Mecano.Entidad.DTOs.Administrador
{
    public class ActualizarAdministradorDTO
    {
        [Required]
        public int AdministradorId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150)]
        [RegularExpression(Patrones.Nombre, ErrorMessage = Patrones.MensajeNombre)]
        public string Nombre { get; set; } = string.Empty;

        public TipoIdentificacion TipoIdentificacion { get; set; } = TipoIdentificacion.Nacional;

        [StringLength(20)]
        public string? Cedula { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }
    }
}
