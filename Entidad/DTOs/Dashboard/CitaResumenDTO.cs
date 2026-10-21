using System;
using Mecano.Entidad.Clases;

namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>Fila compacta de una cita del día para el listado del panel de control.</summary>
    public class CitaResumenDTO
    {
        public int Id { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public EstadoCita Estado { get; set; }
        public string? ClienteNombre { get; set; }
        /// <summary>Marca y modelo del vehículo; la placa viaja aparte sin formatear.</summary>
        public string? VehiculoDescripcion { get; set; }
        public string? Placa { get; set; }
        public string? ServicioNombre { get; set; }
    }
}
