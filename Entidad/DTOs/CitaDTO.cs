using System.Collections.Generic;
using System.Linq;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Mecanico;

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

        /// <summary>Mecánicos asignados a la cita (misma especialidad).</summary>
        public List<MecanicoDTO> Mecanicos { get; set; } = new();

        /// <summary>Nombres de los mecánicos separados por coma, para las tablas.</summary>
        public string MecanicosNombres => string.Join(", ", Mecanicos.Select(m => m.Nombre));
    }
}
