using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    public sealed class NuevoVehiculoDTO
    {
        [Required][MaxLength(20)]
        public string Placa { get; set; } = string.Empty;
        [Required][MaxLength(100)]
        public string Marca { get; set; } = string.Empty;
        [Required][MaxLength(100)]
        public string Modelo { get; set; } = string.Empty;
        [Range(1900, 2100)]
        public int Anio { get; set; }
    }
}
