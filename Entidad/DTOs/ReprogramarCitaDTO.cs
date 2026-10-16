using System;
using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Write model para reprogramar una cita: fecha, hora de inicio y mecánico.
    /// El vehículo, el servicio y las notas se editan con <see cref="EditarCitaDTO"/>.
    /// </summary>
    public class ReprogramarCitaDTO
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un mecánico.")]
        public int MecanicoId { get; set; }

        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
    }
}
