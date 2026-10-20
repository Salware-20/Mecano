using System.Collections.Generic;

namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>Desglose de las citas de hoy: total, conteo por estado y listado ordenado.</summary>
    public class CitasHoyDTO
    {
        public int Total { get; set; }

        /// <summary>
        /// Un elemento por cada estado, en orden del enum (los que quedan en 0
        /// también se listan, para que las pestañas del panel siempre estén completas).
        /// </summary>
        public List<EstadoConteoDTO> Conteos { get; set; } = new();

        /// <summary>
        /// Citas ordenadas por hora de inicio. Incluye las canceladas: el panel
        /// las filtra por pestaña y el total las cuenta.
        /// </summary>
        public List<CitaResumenDTO> Citas { get; set; } = new();
    }
}
