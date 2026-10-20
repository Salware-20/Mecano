using System;

namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>
    /// Ocupación programada del día: minutos-mecánico reservados sobre la
    /// capacidad total (mecánicos activos × minutos del horario de atención).
    /// </summary>
    public class OcupacionDiariaDTO
    {
        /// <summary>Minutos ocupados contados por mecánico (una cita con 2 mecánicos cuenta doble).</summary>
        public int MinutosOcupados { get; set; }

        /// <summary>Capacidad del día; 0 si el taller cierra o no hay mecánicos activos.</summary>
        public int MinutosDisponibles { get; set; }

        /// <summary>Mecánicos distintos con al menos una cita no cancelada hoy.</summary>
        public int MecanicosEnAgenda { get; set; }

        /// <summary>true si la fecha cae en un día no laborable (domingo).</summary>
        public bool Cerrado { get; set; }

        /// <summary>
        /// Porcentaje entero redondeado. Puede superar 100 si mecánicos inactivos
        /// todavía tienen citas hoy; el panel lo muestra con su alerta.
        /// </summary>
        public int Porcentaje =>
            MinutosDisponibles > 0 ? (int)Math.Round(100d * MinutosOcupados / MinutosDisponibles) : 0;
    }
}
