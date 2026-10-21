using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Dashboard;
using Mecano.Entidad.Utilidades;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages;

// El panel de control es la página de inicio ("/"): el NavMenu ya apuntaba
// «Dashboard» a la raíz, así que se reemplaza la Home de plantilla en su lugar.
// Visible solo para el rol Administrador (decisión de diseño de la primera fase).
public partial class Home : ComponentBase, IDisposable
{
    private ResumenDiarioDTO? resumen;
    private bool cargando = true;
    private string? mensajeError;

    // Pestaña abierta del desglose por estado: nav-tabs implementadas en Blazor
    // puro (variable + clase "active"), porque el proyecto no carga el JS de Bootstrap.
    private EstadoCita PestanaActiva { get; set; } = EstadoCita.Pendiente;

    private static readonly CultureInfo CulturaCr = new("es-CR");

    // Se usan solo dentro del bloque "resumen is not null" del markup.
    private TallerPersonalDTO TallerPersonal => resumen!.TallerPersonal;
    private OcupacionDiariaDTO Ocupacion => resumen!.Ocupacion;

    private string FechaLarga =>
        resumen!.Fecha.ToString("dddd, d 'de' MMMM 'de' yyyy", CulturaCr);

    protected override async Task OnInitializedAsync()
    {
        // El tema lo altera MainLayout (window.mecanoTheme); al cambiar, el panel
        // se re-renderiza y los charts se recrean con la paleta nueva (@key).
        Tema.Changed += OnTemaCambiado;
        await Cargar();
        await CargarPeriodoAsync();
    }

    public void Dispose() => Tema.Changed -= OnTemaCambiado;

    private void OnTemaCambiado() => _ = InvokeAsync(StateHasChanged);

    private async Task Cargar()
    {
        cargando = true;
        mensajeError = null;
        try
        {
            resumen = await DashboardService.ObtenerResumenDiarioAsync(DateTime.Today);

            // Abre la primera pestaña que sí tiene citas para no arrancar en vacío.
            PestanaActiva = resumen.CitasHoy.Conteos.Find(c => c.Conteo > 0)?.Estado
                            ?? EstadoCita.Pendiente;
        }
        catch (Exception)
        {
            resumen = null;
            mensajeError = "No se pudo cargar el resumen del día. Intenta de nuevo.";
        }
        finally
        {
            cargando = false;
        }
    }

    private void SeleccionarPestana(EstadoCita estado) => PestanaActiva = estado;

    // Escala de ocupación: sin citas/baja < 50 %, óptima 50–85 %, alta > 85 %
    // (incluye > 100 %, posible si mecánicos inactivos todavía tienen citas hoy).
    private string EtiquetaOcupacion => Ocupacion.Porcentaje switch
    {
        0 => "Sin citas",
        < 50 => "Baja",
        <= 85 => "Óptima",
        _ => "Alta"
    };

    private string ClaseBadgeOcupacion => Ocupacion.Porcentaje switch
    {
        <= 49 => "bg-secondary",
        <= 85 => "bg-success",
        _ => "bg-warning text-dark"
    };

    private string ClaseBarraOcupacion => Ocupacion.Porcentaje switch
    {
        <= 49 => "bg-secondary",
        <= 85 => "bg-success",
        _ => "bg-warning"
    };

    // --- Gráficos del período (semana o mes en curso) ---
    // Un solo estado compartido por las dos tarjetas: cambiarlo en una cambia las dos.
    private enum PeriodoGrafico { Semana, Mes }

    private PeriodoGrafico periodo = PeriodoGrafico.Mes; // decisión: mes por defecto
    private ResumenPeriodoDTO? periodoDatos;
    private bool cargandoPeriodo = true;
    private string? mensajeErrorPeriodo;

    private async Task CargarPeriodoAsync()
    {
        cargandoPeriodo = true;
        mensajeErrorPeriodo = null;
        try
        {
            var hoy = DateTime.Today;
            var lunes = hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)); // semana lun…dom
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var (inicio, fin) = periodo == PeriodoGrafico.Semana
                ? (lunes, lunes.AddDays(6))
                : (inicioMes, inicioMes.AddMonths(1).AddDays(-1));

            periodoDatos = await DashboardService.ObtenerResumenPeriodoAsync(inicio, fin);
        }
        catch (Exception)
        {
            periodoDatos = null;
            mensajeErrorPeriodo = "No se pudo cargar el período de los gráficos.";
        }
        finally
        {
            cargandoPeriodo = false;
        }
    }

    private async Task CambiarPeriodo(PeriodoGrafico nuevo)
    {
        if (periodo == nuevo) return;
        periodo = nuevo;
        await CargarPeriodoAsync();
    }

    // Actualiza el resumen del día y el período de los gráficos (botón ⟳).
    private async Task Recargar()
    {
        await Cargar();
        await CargarPeriodoAsync();
    }

    private string RangoPeriodo => periodoDatos is null
        ? string.Empty
        : $"del {periodoDatos.Inicio:dd/MM} al {periodoDatos.Fin:dd/MM}";

    private string PorcentajeDe(EstadoConteoDTO conteo) =>
        periodoDatos is null || periodoDatos.TotalCitas == 0
            ? "0"
            : (conteo.Conteo * 100.0 / periodoDatos.TotalCitas).ToString("0.#", CulturaCr);

    // ApexCharts no lee variables CSS: colores explícitos según el tema vigente,
    // tomados de los tokens --text-secondary / --primary-accent de app.css.
    private string ColorTextoGrafico => Tema.IsDark ? "#94A3B8" : "#64748B";
    private string ColorAcentoGrafico => Tema.IsDark ? "#38BDF8" : "#0284C7";

    // Orden = orden del enum EstadoCita, igual que la lista Conteos (espejo de Formatos.BadgeEstado).
    private static List<string> ColoresDonut =>
        Enum.GetValues<EstadoCita>().Select(Formatos.ColorEstado).ToList();

    // Formateadores JS para los valores en minutos. El wrapper convierte estos strings
    // en funciones reales (reviver '@eval' de blazor-apexcharts.js: evalúa el código y
    // lo inyecta como formatter). Replican en JS la misma redacción de Formatos.Duracion:
    // si cambia uno, cambiar el otro. "Corto" para las marcas del eje; "largo" para el tooltip.
    private const string JsMinutosCorto =
        "function (m) { m = Math.round(m); if (m < 60) return m + ' min'; var h = Math.floor(m / 60), r = m % 60; return r > 0 ? h + ' h ' + r + ' min' : h + ' h'; }";

    private const string JsMinutosLargo =
        "function (m) { m = Math.round(m); if (m <= 0) return '0 minutos'; var h = Math.floor(m / 60), r = m % 60; var ph = h === 1 ? 'hora' : 'horas', pr = r === 1 ? 'minuto' : 'minutos'; if (h > 0 && r > 0) return h + ' ' + ph + ' ' + r + ' ' + pr; return h > 0 ? h + ' ' + ph : r + ' ' + pr; }";

    // Cada tarjeta lleva su propia instancia de opciones (no se pueden compartir).
    private ApexChartOptions<EstadoConteoDTO> OpcionesDonut() => new()
    {
        Chart = new Chart
        {
            ForeColor = ColorTextoGrafico,
            Toolbar = new Toolbar { Show = false }
        },
        Theme = new Theme { Mode = Tema.IsDark ? Mode.Dark : Mode.Light },
        Colors = ColoresDonut,
        // La lista visible al lado hace de leyenda accesible (con el % por estado).
        Legend = new Legend { Show = false },
        PlotOptions = new PlotOptions
        {
            Pie = new PlotOptionsPie
            {
                Donut = new PlotOptionsDonut
                {
                    Labels = new DonutLabels
                    {
                        Show = true,
                        Total = new DonutLabelTotal { Show = true, Label = "citas" }
                    }
                }
            }
        },
        // El tooltip del donut debe seguir el tema: sin configurarlo, ApexCharts usa
        // su default (siempre oscuro) y quedaría distinto al de la gráfica de barras.
        Tooltip = new Tooltip
        {
            Theme = Tema.IsDark ? Mode.Dark : Mode.Light
        },
        NoData = new NoData
        {
            Text = "Sin citas en el período",
            Align = Align.Center,
            VerticalAlign = VerticalAlign.Middle,
            Style = new NoDataStyle { Color = ColorTextoGrafico }
        }
    };

    private ApexChartOptions<CargaMecanicoDTO> OpcionesBarra() => new()
    {
        Chart = new Chart
        {
            ForeColor = ColorTextoGrafico,
            Toolbar = new Toolbar { Show = false }
        },
        Theme = new Theme { Mode = Tema.IsDark ? Mode.Dark : Mode.Light },
        Colors = new List<string> { ColorAcentoGrafico },
        Legend = new Legend { Show = false },
        DataLabels = new DataLabels { Enabled = false },
        // Barras horizontales: ApexCharts dibuja en este modo el eje de VALORES abajo
        // con la config Xaxis (drawYaxisInversed usa xaxis.labels.formatter para las
        // marcas), mientras que la config Yaxis formatea los nombres de las categorías
        // (izquierda y clave del tooltip). Por eso el formatter va aquí: las marcas se
        // leen «45 min», «1 h» o «2 h 30 min» en vez de «90» o «1.5».
        Xaxis = new XAxis
        {
            Labels = new XAxisLabels { Formatter = JsMinutosCorto }
        },
        PlotOptions = new PlotOptions
        {
            Bar = new PlotOptionsBar { Horizontal = true }
        },
        Tooltip = new Tooltip
        {
            Theme = Tema.IsDark ? Mode.Dark : Mode.Light,
            // «1 hora 30 minutos», como lo diría una persona, no «1.5».
            Y = new TooltipY { Formatter = JsMinutosLargo }
        },
        NoData = new NoData
        {
            Text = "Sin citas en el período",
            Align = Align.Center,
            VerticalAlign = VerticalAlign.Middle,
            Style = new NoDataStyle { Color = ColorTextoGrafico }
        }
    };

    // Etiquetas ARIA de los contenedores de los gráficos (el detalle en texto
    // está en la lista/leyenda al lado o debajo, así el dato nunca se pierde).
    private string EtiquetaDonut => periodoDatos is null
        ? "Gráfico de distribución de citas por estado"
        : $"Gráfico de donut: {periodoDatos.TotalCitas} citas {RangoPeriodo} repartidas por estado; " +
          "el desglose está en la lista adyacente.";

    private string EtiquetaBarra => periodoDatos is null
        ? "Gráfico de carga por mecánico"
        : $"Gráfico de barras horizontales: tiempo asignado por mecánico activo {RangoPeriodo}, " +
          "en minutos presentados como horas y minutos (por ejemplo, 1 hora 30 minutos); " +
          "el detalle de citas y tiempo está bajo el gráfico.";
}
