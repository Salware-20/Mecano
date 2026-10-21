namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Proyección de lectura completa de un vehículo, para la pantalla de detalle (/vehiculos/{id}).
    /// Incluye los datos del dueño para que la vista no tenga que cargar el grafo de entidades.
    /// </summary>
    public class VehiculoDetalleDTO
    {
        public int Id { get; set; }

        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Anio { get; set; }

        /// <summary>Kilometraje (odómetro) del vehículo; nulo si no se ha registrado.</summary>
        public int? Kilometraje { get; set; }

        public string? Color { get; set; }
        public string? Notas { get; set; }
        public bool Activo { get; set; }

        // Dueño (nullable: un vehículo puede haber quedado sin cliente asignado)
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
    }
}
