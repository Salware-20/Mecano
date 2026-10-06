using Mecano.Entidad.Clases;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Proyección de lectura de Cita. Evita exponer el grafo completo de entidades
    /// (Cliente, Vehiculo, Servicio, Mecanico) a las pantallas.
    /// </summary>
    public class CitaDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public EstadoCita Estado { get; set; }
        public string? VehiculoPlaca { get; set; }
        public string? ServicioNombre { get; set; }
        public string? MecanicoNombre { get; set; }
    }
}
