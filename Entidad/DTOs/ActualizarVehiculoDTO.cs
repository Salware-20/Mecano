using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    public class ActualizarVehiculoDTO
    {
        public int Id { get; set; }

        // Ver comentario en NuevoVehiculoDTO: la regla de largo vive en el servicio.
        [Required(ErrorMessage = "La placa es obligatoria")]
        [StringLength(20)]
        public string Placa { get; set; } = string.Empty;

        [Required(ErrorMessage = "La marca es obligatoria")]
        [StringLength(100)]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "El modelo es obligatorio")]
        [StringLength(100)]
        public string Modelo { get; set; } = string.Empty;

        [Range(1900, 2100, ErrorMessage = "El año debe estar entre 1900 y 2100")]
        public int Anio { get; set; }
    }
}
