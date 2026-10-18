using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Write model para reprogramar una cita: fecha, hora de inicio y mecánico(s).
    /// El vehículo, el servicio y las notas se editan con <see cref="EditarCitaDTO"/>.
    /// </summary>
    public class ReprogramarCitaDTO
    {
        public int Id { get; set; }

        [MinLength(1, ErrorMessage = "Seleccione al menos un mecánico.")]
        public List<int> MecanicosIds { get; set; } = new();

        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
    }
}
