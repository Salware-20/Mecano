using Mecano.Entidad.DTOs;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Vehiculos;

public partial class VehiculoDetalle : ComponentBase
{
    [Parameter]
    public int Id { get; set; }

    private VehiculoDetalleDTO? vehiculo;
    private List<CitaDTO> citas = new();
    private bool cargando = true;

    protected override async Task OnInitializedAsync()
    {
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        cargando = true;
        try
        {
            vehiculo = await VehiculoService.ObtenerDetallePorIdAsync(Id);

            if (vehiculo is null)
            {
                citas = new();
                return;
            }

            citas = await CitaService.ObtenerPorVehiculoAsync(Id);
        }
        finally
        {
            cargando = false;
        }
    }
}
