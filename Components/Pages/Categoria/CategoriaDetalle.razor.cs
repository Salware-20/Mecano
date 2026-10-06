using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Categoria;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Categoria;

public partial class CategoriaDetalle : ComponentBase
{
    [Parameter]
    public int Id { get; set; }

    private CategoriaDTO? categoria;
    private List<ServicioDTO> servicios = new();
    private bool cargando = true;
    private bool guardando;

    private bool mostrarInactivos;
    private bool mostrarModal;
    private bool esEdicion;
    private int? idEditando;
    private string? mensajeError;
    private string? mensajeExito;

    private CrearServicioDTO modelo = new();
    private ServicioDTO? servicioAEliminar;

    protected override async Task OnParametersSetAsync()
    {
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        cargando = true;
        try
        {
            categoria = await CategoriaService.ObtenerPorIdAsync(Id);
            servicios = await ServicioService.ObtenerPorCategoriaAsync(Id, soloActivos: !mostrarInactivos);
        }
        finally
        {
            cargando = false;
        }
    }

    private void AbrirCrear()
    {
        esEdicion = false;
        idEditando = null;
        modelo = new CrearServicioDTO();
        mensajeError = null;
        mostrarModal = true;
    }

    private async Task AbrirEditar(ServicioDTO servicio)
    {
        // We reload the row to edit against authoritative data (and to allow editing inactive services).
        var detalle = await ServicioService.ObtenerDetallePorIdAsync(servicio.Id);
        if (detalle is null)
        {
            mensajeError = "El servicio ya no está disponible.";
            return;
        }

        esEdicion = true;
        idEditando = detalle.Id;
        modelo = new CrearServicioDTO
        {
            Nombre = detalle.Nombre,
            Descripcion = detalle.Descripcion,
            DuracionMinutos = detalle.DuracionMinutos,
            Precio = detalle.Precio,
            CategoriaId = detalle.CategoriaId
        };
        mensajeError = null;
        mostrarModal = true;
    }

    private void CerrarModal()
    {
        mostrarModal = false;
        mensajeError = null;
        idEditando = null;
    }

    private async Task Guardar()
    {
        guardando = true;
        mensajeError = null;
        try
        {
            // The page is scoped to a single category, so the service is always assigned to it.
            modelo.CategoriaId = Id;

            if (esEdicion && idEditando.HasValue)
            {
                await ServicioService.ActualizarAsync(new ActualizarServicioDTO
                {
                    Id = idEditando.Value,
                    Nombre = modelo.Nombre,
                    Descripcion = modelo.Descripcion,
                    DuracionMinutos = modelo.DuracionMinutos,
                    Precio = modelo.Precio,
                    CategoriaId = Id
                });
                mensajeExito = $"Servicio “{modelo.Nombre}” actualizado correctamente.";
            }
            else
            {
                await ServicioService.CrearAsync(modelo);
                mensajeExito = $"Servicio “{modelo.Nombre}” creado correctamente.";
            }

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

    private void ConfirmarEliminar(ServicioDTO servicio)
    {
        servicioAEliminar = servicio;
        mensajeError = null;
    }

    private void CancelarEliminar()
    {
        servicioAEliminar = null;
        mensajeError = null;
    }

    private async Task EliminarConfirmado()
    {
        if (servicioAEliminar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            await ServicioService.EliminarAsync(servicioAEliminar.Id);
            mensajeExito = $"Servicio “{servicioAEliminar.Nombre}” desactivado.";
            servicioAEliminar = null;
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

    private async Task Reactivar(ServicioDTO servicio)
    {
        guardando = true;
        try
        {
            if (await ServicioService.ReactivarAsync(servicio.Id))
            {
                mensajeExito = $"Servicio “{servicio.Nombre}” reactivado.";
                await CargarAsync();
            }
        }
        finally
        {
            guardando = false;
        }
    }
}
