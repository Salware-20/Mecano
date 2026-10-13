using Mecano.Entidad.Clases;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Filtros del listado de citas (/citas). Todos los campos son opcionales: un filtro
    /// vacío equivale a "todas las citas".
    /// </summary>
    public sealed class FiltroCitasDTO
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? MecanicoId { get; set; }
        public EstadoCita? Estado { get; set; }

        /// <summary>Texto libre: nombre del cliente o placa del vehículo.</summary>
        public string? Termino { get; set; }
    }
}
