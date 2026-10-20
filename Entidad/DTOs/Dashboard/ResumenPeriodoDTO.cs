using System;
using System.Collections.Generic;

namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>
    /// Lectura agregada de un período (semana o mes en curso) que alimenta los gráficos
    /// del panel de control: distribución de citas por estado y carga por mecánico.
    /// </summary>
    public class ResumenPeriodoDTO
    {
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }

        /// <summary>Total de citas del período (incluye canceladas; el donut las muestra).</summary>
        public int TotalCitas { get; set; }

        /// <summary>Un elemento por cada EstadoCita (orden del enum), aunque quede en 0.</summary>
        public List<EstadoConteoDTO> Conteos { get; set; } = new();

        /// <summary>
        /// Mecánicos activos ordenados por minutos descendentes; los de 0 también aparecen.
        /// Solo citas no canceladas (coherente con la ocupación del día).
        /// </summary>
        public List<CargaMecanicoDTO> CargaMecanicos { get; set; } = new();
    }
}
