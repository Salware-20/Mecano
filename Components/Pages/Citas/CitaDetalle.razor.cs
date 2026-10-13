using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Citas;

public partial class CitaDetalle : ComponentBase
{
    [Parameter]
    public int Id { get; set; }

    private CitaDetalleDTO? cita;
    private bool cargando = true;

    protected override async Task OnParametersSetAsync()
    {
        cargando = true;
        try
        {
            cita = await CitaService.ObtenerDetallePorIdAsync(Id);
        }
        finally
        {
            cargando = false;
        }
    }

    private static string BadgeEstado(EstadoCita estado) => estado switch
    {
        EstadoCita.Pendiente => "bg-secondary",
        EstadoCita.Confirmada => "bg-primary",
        EstadoCita.EnProceso => "bg-info text-dark",
        EstadoCita.Finalizada => "bg-success",
        EstadoCita.Cancelada => "bg-danger",
        _ => "bg-secondary"
    };
}
