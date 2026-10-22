using System;
using System.Threading.Tasks;
using Mecano.Entidad.DTOs;
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

    /// <summary>
    /// Resumen operativo del propio mecánico para su panel (/mecanico): sus citas de hoy
    /// por estado, su ocupación personal (minutos del horario de atención) y su semana
    /// (lunes a sábado). No toca datos de otros mecánicos ni del taller en conjunto.
    /// </summary>
    Task<ResumenMecanicoDTO> ObtenerResumenMecanicoAsync(int mecanicoId);
}
