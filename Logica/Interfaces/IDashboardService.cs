using System;
using System.Threading.Tasks;
using Mecano.Entidad.DTOs.Dashboard;

namespace Mecano.Logica.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Resumen operativo del día (citas por estado, recursos del taller y ocupación)
    /// para los KPIs del panel de control.
    /// </summary>
    Task<ResumenDiarioDTO> ObtenerResumenDiarioAsync(DateTime fecha);

    /// <summary>
    /// Citas de un período (semana o mes en curso) agrupadas por estado y carga por
    /// mecánico activo, para los gráficos del panel de control.
    /// </summary>
    Task<ResumenPeriodoDTO> ObtenerResumenPeriodoAsync(DateTime inicio, DateTime fin);
}
