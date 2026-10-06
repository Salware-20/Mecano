using Mecano.Entidad.Constantes;

namespace Mecano.Entidad.DTOs.Cliente
{
    /// <summary>
    /// Proyección de lectura de Cliente. TipoIdentificacion es de solo lectura: el servicio
    /// la asigna en CrearAsync. La validación y el selector de tipo llegan en la Fase 2.
    /// </summary>
    public class ClienteDTO
    {
        public int Id { get; set; }
        public string CedulaIdentidad { get; set; } = string.Empty;
        public TipoIdentificacion TipoIdentificacion { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public int CantidadVehiculos { get; set; }
        public int CantidadCitas { get; set; }
    }
}
