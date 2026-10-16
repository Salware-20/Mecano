using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Write model para editar los datos "no de agenda" de una cita: vehículo, servicio y notas.
    /// La fecha, la hora y el mecánico no se editan por aquí (eso es la reprogramación, fase posterior).
    /// </summary>
    public class EditarCitaDTO
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un vehículo.")]
        public int VehiculoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un servicio.")]
        public int ServicioId { get; set; }

        [StringLength(1000, ErrorMessage = "Las notas no pueden superar los 1000 caracteres.")]
        public string? NotasAdmin { get; set; }
    }
}
