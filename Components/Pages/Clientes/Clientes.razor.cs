using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Cliente;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Mecano.Components.Pages.Clientes;

public partial class Clientes : ComponentBase, IDisposable
{
    private List<ClienteDTO> clientes = new();
    private bool cargando = true;
    private bool guardando;

    private string terminoBusqueda = string.Empty;
    private FiltroActivo filtro = FiltroActivo.Activos;

    // El Mecánico no puede ver ni buscar clientes por cédula (solo nombre y placa).
    private bool incluirIdentificacion;

    // ~~~ PANEL EXPANDIBLE (vehículos + citas) ~~~
    // Solo una fila expandida a la vez; los datos se cargan al abrir y quedan en caché
    // por cliente hasta que una operación de estado los invalide.
    private int? filaExpandidaId;
    private readonly Dictionary<int, List<Vehiculo>> vehiculosPorCliente = new();
    private readonly Dictionary<int, List<CitaDTO>> citasPorCliente = new();
    private readonly HashSet<int> detalleCargando = new();
    private readonly HashSet<int> detalleError = new();
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
        // La cédula se oculta y no se busca para roles distintos de Administrador.
        var auth = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        incluirIdentificacion = auth.User.FindFirst(ClaimTypes.Role)?.Value == Roles.Administrador;

        await CargarAsync();
    }

    // ~~~ CARGA ~~~
    private async Task CargarAsync()
    {
        // La lista visible cambió (búsqueda, filtro, página o mutación): colapsamos el detalle.
        filaExpandidaId = null;

        cargando = true;
        try
        {
            totalRegistros = await ClienteService.ContarBusquedaAsync(terminoBusqueda, filtro, incluirIdentificacion);
            totalPaginas = totalRegistros == 0
                ? 1
                : (int)Math.Ceiling(totalRegistros / (double)tamanioPagina);

            // Si la página quedó fuera de rango tras un filtro o un toggle, retrocedemos.
            if (paginaActual > totalPaginas)
                paginaActual = totalPaginas;

            clientes = await ClienteService.BuscarClientesPaginadoAsync(
                terminoBusqueda, paginaActual, tamanioPagina, filtro, incluirIdentificacion);
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

    private async Task CambiarFiltro(ChangeEventArgs e)
    {
        if (Enum.TryParse<FiltroActivo>(e.Value?.ToString(), out var valor))
        {
            filtro = valor;
            paginaActual = 1;
            await CargarAsync();
        }
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
            TipoIdentificacion = detalle.TipoIdentificacion,
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
                    TipoIdentificacion = modelo.TipoIdentificacion,
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
            InvalidarDetalle(clienteADesactivar.Id);
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
                InvalidarDetalle(clienteAReactivar.Id);
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
            InvalidarDetalle(clienteAEliminar.Id);
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

    // ~~~ PANEL EXPANDIBLE: CARGA Y ACCIONES ~~~
    private bool EstaExpandido(int clienteId) => filaExpandidaId == clienteId;

    private static string IdDetalle(int clienteId) => $"detalle-{clienteId}";

    private async Task ToggleFila(ClienteDTO cliente)
    {
        if (filaExpandidaId == cliente.Id)
        {
            filaExpandidaId = null;
            return;
        }

        filaExpandidaId = cliente.Id;

        if (vehiculosPorCliente.ContainsKey(cliente.Id))
            return;

        await CargarDetalleAsync(cliente);
    }

    private async Task CargarDetalleAsync(ClienteDTO cliente)
    {
        detalleError.Remove(cliente.Id);
        detalleCargando.Add(cliente.Id);

        // Fuerza el render del spinner antes de la consulta (Blazor solo repinta al
        // completar el handler async, así que sin esto el panel aparecería de golpe).
        StateHasChanged();

        try
        {
            // Mismo criterio que ClienteDetalle: un cliente inactivo conserva su flota
            // completa (incluye vehículos inactivos); uno activo muestra solo los activos.
            var incluirInactivos = !cliente.Activo;
            var tareaVehiculos = VehiculoService.ObtenerPorClienteAsync(cliente.Id, incluirInactivos);
            var tareaCitas = CitaService.ObtenerPorClienteAsync(cliente.Id);

            await Task.WhenAll(tareaVehiculos, tareaCitas);

            vehiculosPorCliente[cliente.Id] = tareaVehiculos.Result;
            citasPorCliente[cliente.Id] = tareaCitas.Result;
        }
        catch
        {
            // Sin caché ante error: el próximo intento vuelve a consultar.
            detalleError.Add(cliente.Id);
        }
        finally
        {
            detalleCargando.Remove(cliente.Id);
        }
    }

    private async Task RecargarDetalle(ClienteDTO cliente)
    {
        vehiculosPorCliente.Remove(cliente.Id);
        citasPorCliente.Remove(cliente.Id);
        filaExpandidaId = cliente.Id;
        await CargarDetalleAsync(cliente);
    }

    private List<Vehiculo> VehiculosDe(int clienteId) =>
        vehiculosPorCliente.TryGetValue(clienteId, out var vehiculos) ? vehiculos : new List<Vehiculo>();

    private List<CitaDTO> CitasDe(int clienteId) =>
        citasPorCliente.TryGetValue(clienteId, out var citas) ? citas : new List<CitaDTO>();

    /// <summary>Descarta el detalle cacheado de un cliente tras un cambio de estado que lo afecta.</summary>
    private void InvalidarDetalle(int clienteId)
    {
        vehiculosPorCliente.Remove(clienteId);
        citasPorCliente.Remove(clienteId);
        detalleError.Remove(clienteId);

        if (filaExpandidaId == clienteId)
            filaExpandidaId = null;
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

    private bool SinHistoria(ClienteDTO cliente) => cliente.CantidadVehiculos == 0 && cliente.CantidadCitas == 0;

    private string TextoVacio() =>
        !string.IsNullOrWhiteSpace(terminoBusqueda)
            ? "No se encontraron clientes que coincidan con la búsqueda."
            : filtro switch
            {
                FiltroActivo.Inactivos => "No hay clientes inactivos.",
                FiltroActivo.Todos => "No hay clientes registrados todavía.",
                _ => "No hay clientes activos."
            };

    private string TextoPlaceholderBusqueda() =>
        incluirIdentificacion ? "Buscar por nombre, cédula o placa..." : "Buscar por nombre o placa...";

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
    }
}
