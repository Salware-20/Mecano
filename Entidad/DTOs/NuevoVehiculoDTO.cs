using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.DTOs
{
    public sealed class NuevoVehiculoDTO
    {
        // Dueño del vehículo. La página lo asigna desde la ruta (/clientes/{Id});
        // en el flujo de AgendarCita no se usa (CitaService toma ClienteId de la cita).
        public int ClienteId { get; set; }

        // StringLength laxo a propósito: el usuario escribe "ABC-123" (10 con guion) y la
        // regla estricta de 6 dígitos alfanuméricos vive en VehiculoService.NormalizarYValidar,
        // de modo que el error llegue a la UI por el catch (InvalidOperationException)
        // que la página ya maneja. Misma decisión que CrearClienteDTO con la cédula.
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
