using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.Utilidades;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mecano.Components.Pages.Vehiculos;

public partial class VehiculoDetalle : ComponentBase
{
    [Parameter]
    public int Id { get; set; }

    private VehiculoDetalleDTO? vehiculo;
    private List<CitaDTO> citas = new();
    private bool cargando = true;

    // ~~~ Edición (solo Administrador) ~~~
    private bool mostrarModalEdicion;
    private NuevoVehiculoDTO modeloVehiculo = new();
    private bool guardando;
    private string? mensajeErrorEdicion;
    private string? mensajeExito;

    /// <summary>Estados del resumen, en el orden del flujo de la cita.</summary>
    private static readonly EstadoCita[] EstadosResumen =
    {
        EstadoCita.Pendiente,
        EstadoCita.Confirmado,
        EstadoCita.EnProceso,
        EstadoCita.Finalizado,
        EstadoCita.Cancelado
    };

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

    // ~~~ EDICIÓN ~~~
    private void AbrirEdicion()
    {
        if (vehiculo is null) return;

        modeloVehiculo = new NuevoVehiculoDTO
        {
            Placa = vehiculo.Placa,
            Marca = vehiculo.Marca,
            Modelo = vehiculo.Modelo,
            Anio = vehiculo.Anio,
            Kilometraje = vehiculo.Kilometraje,
            Color = vehiculo.Color,
            Notas = vehiculo.Notas
        };
        mensajeErrorEdicion = null;
        mensajeExito = null;
        mostrarModalEdicion = true;
    }

    private void CerrarEdicion()
    {
        mostrarModalEdicion = false;
        mensajeErrorEdicion = null;
    }

    private async Task GuardarEdicion()
    {
        guardando = true;
        mensajeErrorEdicion = null;
        try
        {
            await VehiculoService.ActualizarAsync(new ActualizarVehiculoDTO
            {
                Id = Id,
                Placa = modeloVehiculo.Placa,
                Marca = modeloVehiculo.Marca,
                Modelo = modeloVehiculo.Modelo,
                Anio = modeloVehiculo.Anio,
                Kilometraje = modeloVehiculo.Kilometraje,
                Color = modeloVehiculo.Color,
                Notas = modeloVehiculo.Notas
            });

            mostrarModalEdicion = false;
            await CargarAsync();
            mensajeExito = $"Vehículo “{Formatos.Placa(vehiculo?.Placa ?? modeloVehiculo.Placa)}” actualizado correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            mensajeErrorEdicion = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }

    // ~~~ REPORTE (impresión del navegador: Guardar como PDF) ~~~
    private async Task GenerarReporte() => await JS.InvokeVoidAsync("print");

    // ~~~ RESUMEN ~~~
    private int ContarEstado(EstadoCita estado) => citas.Count(c => c.Estado == estado);
}
