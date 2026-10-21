using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Write model para agendar una cita. Se pueden asignar uno o más mecánicos
    /// (siempre de la misma especialidad/servicio) a la misma cita.
    /// </summary>
    public sealed class AgendarCitaDTO
    {
        public int ClienteId { get; set; }
        public int? VehiculoId { get; set; }
        public int ServicioId { get; set; }

        [MinLength(1, ErrorMessage = "Seleccione al menos un mecánico.")]
        public List<int> MecanicosIds { get; set; } = new();

        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
    }
}
