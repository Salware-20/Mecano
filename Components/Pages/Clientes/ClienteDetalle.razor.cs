using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Cliente;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Clientes;

public partial class ClienteDetalle : ComponentBase
{
    [Parameter]
    public int Id { get; set; }

    private ClienteDTO? cliente;
    private List<Vehiculo> vehiculos = new();
    private List<CitaDTO> citas = new();

    private bool cargando = true;
    private bool guardando;

    private bool mostrarModal;
    private string? mensajeError;
    private string? mensajeExito;

    private CrearClienteDTO modelo = new();
    private ClienteDTO? clienteAEliminar;
    private ClienteDTO? clienteADesactivar;

    protected override async Task OnParametersSetAsync()
    {
        await CargarAsync();
    }

    // ~~~ CARGA ~~~
    private async Task CargarAsync()
    {
        cargando = true;
        try
        {
            cliente = await ClienteService.ObtenerDetallePorIdAsync(Id);

            if (cliente is null)
            {
                vehiculos = new();
                citas = new();
                return;
            }

            // Las dos consultas son independientes: las disparamos en paralelo.
            var tareaVehiculos = VehiculoService.ObtenerPorClienteAsync(Id);
            var tareaCitas = CitaService.ObtenerPorClienteAsync(Id);

            await Task.WhenAll(tareaVehiculos, tareaCitas);

            vehiculos = tareaVehiculos.Result;
            citas = tareaCitas.Result;
        }
        finally
        {
            cargando = false;
        }
    }

    // ~~~ MODAL EDITAR ~~~
    private void AbrirEditar()
    {
        if (cliente is null) return;

        modelo = new CrearClienteDTO
        {
            CedulaIdentidad = cliente.CedulaIdentidad,
            NombreCompleto = cliente.NombreCompleto,
            Telefono = cliente.Telefono,
            Correo = cliente.Correo,
            Direccion = cliente.Direccion
        };
        mensajeError = null;
        mostrarModal = true;
    }

    private void CerrarModal()
    {
        mostrarModal = false;
        mensajeError = null;
    }

    private async Task Guardar()
    {
        guardando = true;
        mensajeError = null;
        try
        {
            await ClienteService.ActualizarAsync(new ActualizarClienteDTO
            {
                Id = Id,
                CedulaIdentidad = modelo.CedulaIdentidad,
                NombreCompleto = modelo.NombreCompleto,
                Telefono = modelo.Telefono,
                Correo = modelo.Correo,
                Direccion = modelo.Direccion
            });

            mensajeExito = $"Cliente “{modelo.NombreCompleto}” actualizado correctamente.";
            CerrarModal();
            await CargarAsync();
        }
        catch (InvalidOperationException ex)
        {
            mensajeError = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }

    // ~~~ ESTADO ~~~
    private void ConfirmarDesactivar()
    {
        if (cliente is null) return;
        clienteADesactivar = cliente;
        mensajeError = null;
    }

    private void CancelarDesactivar()
    {
        clienteADesactivar = null;
        mensajeError = null;
    }

    private async Task DesactivarConfirmado()
    {
        if (clienteADesactivar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            await ClienteService.DesactivarAsync(Id);
            mensajeExito = $"Cliente “{clienteADesactivar.NombreCompleto}” desactivado.";
            clienteADesactivar = null;
            await CargarAsync();
        }
        catch (InvalidOperationException ex)
        {
            mensajeError = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }

    private async Task Reactivar()
    {
        guardando = true;
        try
        {
            if (await ClienteService.ReactivarAsync(Id))
            {
                mensajeExito = "Cliente reactivado.";
                await CargarAsync();
            }
        }
        finally
        {
            guardando = false;
        }
    }

    // ~~~ ELIMINAR DEFINITIVO ~~~
    private void ConfirmarEliminar()
    {
        if (cliente is null) return;
        clienteAEliminar = cliente;
        mensajeError = null;
    }

    private void CancelarEliminar()
    {
        clienteAEliminar = null;
        mensajeError = null;
    }

    private async Task EliminarConfirmado()
    {
        if (clienteAEliminar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            await ClienteService.EliminarDefinitivoAsync(Id);
            mensajeExito = $"Cliente “{clienteAEliminar.NombreCompleto}” eliminado definitivamente.";
            clienteAEliminar = null;
            await CargarAsync();
        }
        catch (InvalidOperationException ex)
        {
            mensajeError = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }

    private bool SinHistoria => cliente is not null
        && cliente.CantidadVehiculos == 0
        && cliente.CantidadCitas == 0;

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
