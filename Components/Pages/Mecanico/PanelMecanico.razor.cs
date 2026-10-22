using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Mecano.Entidad.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Mecano.Components.Pages.Mecanico;

/// <summary>
/// Panel del mecánico autenticado (/mecanico): sus KPIs personales — citas de hoy
/// por estado, su ocupación sobre los minutos del horario de atención, su semana
/// (lunes a sábado) y su especialidad. No hay datos del taller en conjunto ni de
/// otros mecánicos: todo sale de <c>ObtenerResumenMecanicoAsync</c> con su id de claim.
/// </summary>
public partial class PanelMecanico : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private ResumenMecanicoDTO? resumen;
    private bool cargando = true;
    private string? mensajeError;
    private int mecanicoId;

    private static readonly System.Globalization.CultureInfo CulturaCr = new("es-CR");

    /// <summary>Fecha larga del resumen ("lunes, 20 de octubre de 2026").</summary>
    private string FechaLarga =>
        resumen!.Fecha.ToString("dddd, d 'de' MMMM 'de' yyyy", CulturaCr);

    /// <summary>Total de citas de la semana (lunes a sábado).</summary>
    private int CitasSemana => resumen?.Semana.Sum(d => d.TotalCitas) ?? 0;

    /// <summary>Minutos ocupados en la semana (lunes a sábado).</summary>
    private int MinutosSemana => resumen?.Semana.Sum(d => d.MinutosOcupados) ?? 0;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState is not null)
        {
            var state = await AuthState;
            mecanicoId = int.TryParse(
                state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
        }

        await Cargar();
    }

    private async Task Cargar()
    {
        cargando = true;
        mensajeError = null;
        try
        {
            if (mecanicoId == 0)
            {
                mensajeError = "No se pudo identificar tu cuenta. Cierra sesión y vuelve a entrar.";
                return;
            }

            resumen = await DashboardService.ObtenerResumenMecanicoAsync(mecanicoId);
        }
        catch (Exception)
        {
            resumen = null;
            mensajeError = "No se pudo cargar tu panel. Inténtalo de nuevo.";
        }
        finally
        {
            cargando = false;
        }
    }

    /// <summary>
    /// Altura de la barra semanal (0–100 %) frente a los minutos personales del día.
    /// </summary>
    private int PorcentajeDia(Mecano.Entidad.DTOs.CargaDiaMecanicoDTO dia)
    {
        var disponibles = resumen?.MinutosDisponibles ?? 0;
        if (disponibles <= 0)
            return 0;

        return Math.Min(100, (int)Math.Round(100d * dia.MinutosOcupados / disponibles));
    }

    /// <summary>Etiqueta corta del día ("lun", "mar", …) en español.</summary>
    private static string DiaCorto(DateTime fecha) =>
        fecha.ToString("ddd", CulturaCr).Replace(".", "");
}
