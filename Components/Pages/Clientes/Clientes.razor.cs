using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Cliente;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Clientes;

public partial class Clientes : ComponentBase, IDisposable
{
    private List<ClienteDTO> clientes = new();
    private bool cargando = true;
    private bool guardando;

    private string terminoBusqueda = string.Empty;
    private bool mostrarInactivos;
    private int paginaActual = 1;
    private int tamanioPagina = 20;
    private int totalRegistros;
    private int totalPaginas;

    private bool mostrarModal;
    private bool esEdicion;
    private int? idEditando;
    private string? mensajeError;
    private string? mensajeExito;

    private CrearClienteDTO modelo = new();
    private ClienteDTO? clienteAEliminar;
    private ClienteDTO? clienteADesactivar;
    private ClienteDTO? clienteAReactivar;

    // Citas futuras del cliente a desactivar (para la advertencia en el modal).
    private int citasFuturas;

    // Debounce de la búsqueda: cada pulsación reinicia el temporizador.
    private CancellationTokenSource? _debounceCts;

    private static readonly int[] TamaniosPagina = { 10, 20, 50, 100 };

    protected override async Task OnInitializedAsync()
    {
        await CargarAsync();
    }

    // ~~~ CARGA ~~~
    private async Task CargarAsync()
    {
        cargando = true;
        try
        {
            totalRegistros = await ClienteService.ContarBusquedaAsync(terminoBusqueda, mostrarInactivos);
            totalPaginas = totalRegistros == 0
                ? 1
                : (int)Math.Ceiling(totalRegistros / (double)tamanioPagina);

            // Si la página quedó fuera de rango tras un filtro o un toggle, retrocedemos.
            if (paginaActual > totalPaginas)
                paginaActual = totalPaginas;

            clientes = await ClienteService.BuscarClientesPaginadoAsync(
                terminoBusqueda, paginaActual, tamanioPagina, mostrarInactivos);
        }
        finally
        {
            cargando = false;
        }
    }

    // ~~~ BÚSQUEDA / FILTROS ~~~
    private async Task OnBuscarCambio(ChangeEventArgs e)
    {
        terminoBusqueda = e.Value?.ToString() ?? string.Empty;

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(400, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        paginaActual = 1;
        await CargarAsync();
    }

    private async Task LimpiarBusqueda()
    {
        terminoBusqueda = string.Empty;
        paginaActual = 1;
        await CargarAsync();
    }

    private async Task CambiarMostrarInactivos(bool valor)
    {
        mostrarInactivos = valor;
        paginaActual = 1;
        await CargarAsync();
    }

    private async Task CambiarTamanioPagina(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var nuevoTamanio) && nuevoTamanio > 0)
        {
            tamanioPagina = nuevoTamanio;
            paginaActual = 1;
            await CargarAsync();
        }
    }

    // ~~~ PAGINACIÓN ~~~
    private async Task IrAPagina(int pagina)
    {
        if (pagina < 1 || pagina > totalPaginas || pagina == paginaActual)
            return;

        paginaActual = pagina;
        await CargarAsync();
    }

    private Task PaginaAnterior() => IrAPagina(paginaActual - 1);
    private Task PaginaSiguiente() => IrAPagina(paginaActual + 1);

    /// <summary>
    /// Ventana de páginas numeradas alrededor de la actual, con elipsis en los extremos.
    /// Ej: 1 … 4 5 6 … 20
    /// </summary>
    private IEnumerable<int> PaginasVisibles()
    {
        const int radio = 2;
        var inicio = Math.Max(1, paginaActual - radio);
        var fin = Math.Min(totalPaginas, paginaActual + radio);

        for (var p = inicio; p <= fin; p++)
            yield return p;
    }

    private bool MostrarElipsisAntes() => paginaActual - 2 > 1;
    private bool MostrarElipsisDespues() => paginaActual + 2 < totalPaginas;

    // ~~~ MODAL CREAR / EDITAR ~~~
    private void AbrirCrear()
    {
        esEdicion = false;
        idEditando = null;
        modelo = new CrearClienteDTO();
        mensajeError = null;
        mostrarModal = true;
    }

    private async Task AbrirEditar(ClienteDTO cliente)
    {
        // Recargamos la fila contra datos autoritativos antes de editar.
        var detalle = await ClienteService.ObtenerDetallePorIdAsync(cliente.Id);
        if (detalle is null)
        {
            mensajeError = "El cliente ya no está disponible.";
            return;
        }

        esEdicion = true;
        idEditando = detalle.Id;
        modelo = new CrearClienteDTO
        {
            CedulaIdentidad = detalle.CedulaIdentidad,
            NombreCompleto = detalle.NombreCompleto,
            Telefono = detalle.Telefono,
            Correo = detalle.Correo,
            Direccion = detalle.Direccion
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
            if (esEdicion && idEditando.HasValue)
            {
                await ClienteService.ActualizarAsync(new ActualizarClienteDTO
                {
                    Id = idEditando.Value,
                    CedulaIdentidad = modelo.CedulaIdentidad,
                    NombreCompleto = modelo.NombreCompleto,
                    Telefono = modelo.Telefono,
                    Correo = modelo.Correo,
                    Direccion = modelo.Direccion
                });
                mensajeExito = $"Cliente “{modelo.NombreCompleto}” actualizado correctamente.";
            }
            else
            {
                await ClienteService.CrearAsync(modelo);
                mensajeExito = $"Cliente “{modelo.NombreCompleto}” creado correctamente.";
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

    // ~~~ ESTADO: DESACTIVAR / REACTIVAR ~~~
    private async Task ConfirmarDesactivar(ClienteDTO cliente)
    {
        clienteADesactivar = cliente;
        citasFuturas = 0;
        mensajeError = null;

        // Cuenta citas futuras (Fecha >= hoy, Estado no Cancelada/Finalizada)
        // para mostrar la advertencia en el modal de confirmación.
        try
        {
            var citas = await CitaService.ObtenerPorClienteAsync(cliente.Id);
            citasFuturas = citas.Count(c =>
                c.Fecha >= DateTime.Today
                && c.Estado != EstadoCita.Cancelada
                && c.Estado != EstadoCita.Finalizada);
        }
        catch
        {
            // Si el conteo falla, el modal simplemente no muestra la advertencia.
            citasFuturas = 0;
        }
    }

    private void CancelarDesactivar()
    {
        clienteADesactivar = null;
        citasFuturas = 0;
        mensajeError = null;
    }

    private async Task DesactivarConfirmado()
    {
        if (clienteADesactivar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            await ClienteService.DesactivarAsync(clienteADesactivar.Id);
            mensajeExito = $"Cliente “{clienteADesactivar.NombreCompleto}” desactivado.";
            clienteADesactivar = null;
            citasFuturas = 0;
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

    private void ConfirmarReactivar(ClienteDTO cliente)
    {
        clienteAReactivar = cliente;
        mensajeError = null;
    }

    private void CancelarReactivar()
    {
        clienteAReactivar = null;
        mensajeError = null;
    }

    private async Task ReactivarConfirmado()
    {
        if (clienteAReactivar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            if (await ClienteService.ReactivarAsync(clienteAReactivar.Id))
            {
                mensajeExito = $"Cliente “{clienteAReactivar.NombreCompleto}” reactivado.";
                clienteAReactivar = null;
                await CargarAsync();
            }
            else
            {
                mensajeError = "No se pudo reactivar el cliente.";
            }
        }
        finally
        {
            guardando = false;
        }
    }

    // ~~~ ELIMINAR DEFINITIVO ~~~
    private void ConfirmarEliminar(ClienteDTO cliente)
    {
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
            await ClienteService.EliminarDefinitivoAsync(clienteAEliminar.Id);
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

    private bool SinHistoria(ClienteDTO cliente) => cliente.CantidadVehiculos == 0 && cliente.CantidadCitas == 0;

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
    }
}
