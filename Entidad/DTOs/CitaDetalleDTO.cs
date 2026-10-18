using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Proyección de lectura completa de una cita, para la pantalla de detalle (/citas/{id}).
    /// Incluye los identificadores y los datos de las entidades relacionadas que la vista
    /// muestra, de modo que la página no tenga que cargar el grafo de entidades.
    /// </summary>
    public class CitaDetalleDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public EstadoCita Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? NotasAdmin { get; set; }

        // Cliente
        public int ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public bool ClienteActivo { get; set; }
        public string? ClienteCedula { get; set; }
        public TipoIdentificacion ClienteTipoIdentificacion { get; set; }
        public string? ClienteTelefono { get; set; }
        public string? ClienteCorreo { get; set; }

        // Vehículo
        public int VehiculoId { get; set; }
        public string? VehiculoPlaca { get; set; }
        public string? VehiculoMarca { get; set; }
        public string? VehiculoModelo { get; set; }
        public int VehiculoAnio { get; set; }

        // Servicio
        public int ServicioId { get; set; }
        public string? ServicioNombre { get; set; }
        public int ServicioDuracionMinutos { get; set; }
        public decimal ServicioPrecio { get; set; }

        // Mecánico
        public int MecanicoId { get; set; }
        public string? MecanicoNombre { get; set; }
        public bool MecanicoActivo { get; set; }
        public int MecanicoEspecialidadId { get; set; }
        public string? MecanicoEspecialidadNombre { get; set; }
    }
}
