namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>Recursos del taller: plantilla activa y registros de clientes/vehículos.</summary>
    public class TallerPersonalDTO
    {
        public int MecanicosActivos { get; set; }

        /// <summary>Mecánicos con al menos una cita no cancelada hoy («trabajando hoy»).</summary>
        public int MecanicosConCitasHoy { get; set; }

        public int ClientesActivos { get; set; }
        public int VehiculosActivos { get; set; }
    }
}
