using System;
using System.Collections.Generic;
using Mecano.Entidad.DTOs.Dashboard;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Resumen operativo del propio mecánico para su panel (/mecanico): sus citas de
    /// hoy por estado, sus minutos ocupados sobre su capacidad personal
    /// (los <c>MinutosPorDia</c> del horario de atención) y su semana laborable
    /// (lunes a sábado). No expone datos de otros mecánicos ni del taller en conjunto:
    /// es la lectura individual equivalente a <see cref="ResumenDiarioDTO"/> del admin.
    /// </summary>
    public class ResumenMecanicoDTO
    {
        public int MecanicoId { get; set; }
        public string? MecanicoNombre { get; set; }
        public string? EspecialidadNombre { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>Conteo de las citas del mecánico hoy, un elemento por estado (los de 0 también).</summary>
        public List<EstadoConteoDTO> CitasHoyPorEstado { get; set; } = new();

        /// <summary>Total de citas del mecánico hoy (incluye canceladas).</summary>
        public int CitasHoyTotal { get; set; }

        /// <summary>Minutos ocupados hoy por este mecánico (cada cita cuenta su duración).</summary>
        public int MinutosOcupados { get; set; }

        /// <summary>Capacidad personal del día: los minutos del horario de atención; 0 si es día cerrado.</summary>
        public int MinutosDisponibles { get; set; }

        /// <summary>Porcentaje entero de ocupación personal de hoy.</summary>
        public int PorcentajeOcupacion =>
            MinutosDisponibles > 0 ? (int)Math.Round(100d * MinutosOcupados / MinutosDisponibles) : 0;

        /// <summary>true si hoy es día no laborable (domingo).</summary>
        public bool Cerrado { get; set; }

        /// <summary>Cargas de la semana en curso (lunes a sábado), un elemento por día.</summary>
        public List<CargaDiaMecanicoDTO> Semana { get; set; } = new();
    }

    /// <summary>Carga de un día de la semana para el mecánico autenticado.</summary>
    public class CargaDiaMecanicoDTO
    {
        public DateTime Fecha { get; set; }

        /// <summary>Citas del mecánico ese día (incluye canceladas; el panel las filtra).</summary>
        public int TotalCitas { get; set; }

        /// <summary>Minutos ocupados ese día por el mecánico.</summary>
        public int MinutosOcupados { get; set; }

        /// <summary>true si el día es cerrado (domingo).</summary>
        public bool Cerrado { get; set; }
    }
}
