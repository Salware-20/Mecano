using System;
using System.Globalization;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Dashboard;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages;

// El panel de control es la página de inicio ("/"): el NavMenu ya apuntaba
// «Dashboard» a la raíz, así que se reemplaza la Home de plantilla en su lugar.
// Visible solo para el rol Administrador (decisión de diseño de la primera fase).
public partial class Home : ComponentBase
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

    protected override async Task OnInitializedAsync() => await Cargar();

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
}
