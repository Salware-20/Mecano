using Mecano.Entidad.Clases;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Proyección de lectura de Cita. Evita exponer el grafo completo de entidades
    /// (Cliente, Vehiculo, Servicio, Mecanico) a las pantallas.
    /// Es la proyección compartida por el listado (/citas), el panel de citas del
    /// cliente y la pestaña de citas en el detalle del cliente.
    /// </summary>
    public class CitaDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public EstadoCita Estado { get; set; }

        public int ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public bool ClienteActivo { get; set; }

        public int VehiculoId { get; set; }
        public string? VehiculoPlaca { get; set; }

        public int ServicioId { get; set; }
        public string? ServicioNombre { get; set; }

        public int MecanicoId { get; set; }
        public string? MecanicoNombre { get; set; }
    }
}
