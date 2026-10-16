using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Cliente;
using Mecano.Entidad.DTOs.Comentario;
using Mecano.Entidad.Utilidades;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Security.Claims;
using System.Threading;

namespace Mecano.Components.Pages.Clientes;

public partial class ClienteDetalle : ComponentBase, IDisposable
{
    [Parameter]
    public int Id { get; set; }

    private ClienteDTO? cliente;
    private List<Vehiculo> vehiculos = new();
    private List<CitaDTO> citas = new();

    private bool cargando = true;
    private bool guardando;

    // ~~~ COMENTARIOS ~~~
    private List<ComentarioDTO> comentarios = new();
    private bool cargandoComentarios;
    private bool errorCargaComentarios;
    private bool mostrarModalComentario;
    private CrearComentarioDTO modeloComentario = new();
    private ComentarioDTO? comentarioAEliminar;

    // Usuario logueado: es el autor de los comentarios que publique
    private string autorNombreActual = string.Empty;
    private TipoAutorComentario autorTipoActual = TipoAutorComentario.Administrador;

    private bool mostrarModal;
    private string? mensajeError;
    private string? mensajeExito;

    private CrearClienteDTO modelo = new();
    private ClienteDTO? clienteAEliminar;
    private ClienteDTO? clienteADesactivar;
    private ClienteDTO? clienteAReactivar;

    // ~~~ VEHÍCULOS: formulario (crear/editar) y borrado ~~~
    private bool mostrarModalVehiculo;
    private Vehiculo? vehiculoEditando;   // null = modo crear; no null = modo editar
    private NuevoVehiculoDTO modeloVehiculo = new();
    private Vehiculo? vehiculoAEliminar;

    // ~~~ VEHÍCULOS: transferencia ~~~
    private Vehiculo? vehiculoATransferir;
    private string terminoDestino = string.Empty;
    private List<ClienteDTO> destinos = new();
    private bool buscandoDestino;
    private ClienteDTO? clienteDestino;
    private CancellationTokenSource? _debounceDestinoCts;

    // ~~~ ESTADO: advertencias ~~~
    private int citasFuturas;

    protected override async Task OnParametersSetAsync()
    {
        await CargarAsync();
    }

    /// <summary>El autor se resuelve una sola vez, basado en la sesión actual.</summary>
    protected override async Task OnInitializedAsync()
    {
        await CargarAutorAsync();
    }

    // ~~~ CARGA AUTOR (usuario logueado) ~~~
    private async Task CargarAutorAsync()
    {
        var auth = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        autorNombreActual = auth.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        autorTipoActual = auth.User.FindFirst(ClaimTypes.Role)?.Value == Roles.Administrador
            ? TipoAutorComentario.Administrador
            : TipoAutorComentario.Mecanico;
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
                comentarios = new();
                return;
            }

            // ObtenerPorClienteAsync muestra todos los vehículos (incluidos inactivos)
            // cuando el cliente está inactivo, para permitir transferencia/visualización
            // de la flota completa. Clientes activos: solo vehículos activos.
            var incluirInactivos = cliente is not null && !cliente.Activo;
            var tareaVehiculos = VehiculoService.ObtenerPorClienteAsync(Id, incluirInactivos);
            var tareaCitas = CitaService.ObtenerPorClienteAsync(Id);

            await Task.WhenAll(tareaVehiculos, tareaCitas);

            vehiculos = tareaVehiculos.Result;
            citas = tareaCitas.Result;

            await CargarComentariosAsync();
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
            TipoIdentificacion = cliente.TipoIdentificacion,
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
                TipoIdentificacion = modelo.TipoIdentificacion,
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

    // ~~~ VEHÍCULOS: MODAL FORMULARIO (crear/editar) ~~~
    private void AbrirCrearVehiculo()
    {
        if (cliente is null) return;

        modeloVehiculo = new NuevoVehiculoDTO { ClienteId = Id };
        vehiculoEditando = null;
        mensajeError = null;
        mostrarModalVehiculo = true;
    }

    private void AbrirEditarVehiculo(Vehiculo veh)
    {
        // Se puebla desde la fila de la lista: todos los campos editables ya están
        // cargados, no hace falta volver a leer la base (YAGNI).
        modeloVehiculo = new NuevoVehiculoDTO
        {
            ClienteId = veh.ClienteId ?? Id,
            Placa = veh.Placa, // forma de almacenamiento "ABC123": el input va sin guiones
            Marca = veh.Marca,
            Modelo = veh.Modelo,
            Anio = veh.Anio,
            Kilometraje = veh.Kilometraje,
            Color = veh.Color,
            Notas = veh.Notas
        };
        vehiculoEditando = veh;
        mensajeError = null;
        mostrarModalVehiculo = true;
    }

    private void CerrarModalVehiculo()
    {
        mostrarModalVehiculo = false;
        vehiculoEditando = null;
        mensajeError = null;
    }

    private async Task GuardarVehiculo()
    {
        guardando = true;
        mensajeError = null;
        try
        {
            if (vehiculoEditando is not null)
            {
                await VehiculoService.ActualizarAsync(new ActualizarVehiculoDTO
                {
                    Id = vehiculoEditando.VehiculoId,
                    Placa = modeloVehiculo.Placa,
                    Marca = modeloVehiculo.Marca,
                    Modelo = modeloVehiculo.Modelo,
                    Anio = modeloVehiculo.Anio,
                    Kilometraje = modeloVehiculo.Kilometraje,
                    Color = modeloVehiculo.Color,
                    Notas = modeloVehiculo.Notas
                });
                mensajeExito = $"Vehículo “{Formatos.Placa(modeloVehiculo.Placa)}” actualizado correctamente.";
            }
            else
            {
                await VehiculoService.CrearAsync(modeloVehiculo);
                mensajeExito = $"Vehículo “{Formatos.Placa(modeloVehiculo.Placa)}” registrado correctamente.";
            }

            CerrarModalVehiculo();
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

    // ~~~ VEHÍCULOS: ELIMINAR (soft delete) ~~~
    private void ConfirmarEliminarVehiculo()
    {
        if (vehiculoEditando is null) return;

        // No se apilan modales: el formulario se cierra y el de confirmación toma su lugar.
        mostrarModalVehiculo = false;
        vehiculoAEliminar = vehiculoEditando;
        vehiculoEditando = null;
        mensajeError = null;
    }

    private void CancelarEliminarVehiculo()
    {
        vehiculoAEliminar = null;
        mensajeError = null;
    }

    private async Task EliminarVehiculoConfirmado()
    {
        if (vehiculoAEliminar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            await VehiculoService.EliminarAsync(vehiculoAEliminar.VehiculoId);
            mensajeExito = $"Vehículo “{Formatos.Placa(vehiculoAEliminar.Placa)}” eliminado.";
            vehiculoAEliminar = null;
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

    // ~~~ VEHÍCULOS: TRANSFERIR ~~~
    private void AbrirTransferencia()
    {
        if (vehiculoEditando is null) return;

        // Cierra el formulario y abre el modal de transferencia (sin apilar modales)
        vehiculoATransferir = vehiculoEditando;
        vehiculoEditando = null;
        mostrarModalVehiculo = false;

        // Estado inicial de búsqueda
        terminoDestino = string.Empty;
        destinos.Clear();
        clienteDestino = null;
        mensajeError = null;
        _debounceDestinoCts?.Cancel();
        _debounceDestinoCts?.Dispose();
        _debounceDestinoCts = new CancellationTokenSource();
    }

    private void OnBuscarDestinoCambio(ChangeEventArgs e)
    {
        terminoDestino = e.Value?.ToString() ?? string.Empty;

        _debounceDestinoCts?.Cancel();
        _debounceDestinoCts?.Dispose();
        _debounceDestinoCts = new CancellationTokenSource();
        var token = _debounceDestinoCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await InvokeAsync(async () =>
            {
                buscandoDestino = true;
                try
                {
                    // Solo clientes activos; el dueño actual se filtra client-side
                    destinos = await ClienteService.BuscarClientesPaginadoAsync(terminoDestino, 1, 10, FiltroActivo.Activos);
                    // Excluir dueño actual
                    if (vehiculoATransferir is not null)
                    {
                        destinos = destinos.Where(c => c.Id != vehiculoATransferir.ClienteId).ToList();
                    }
                }
                finally
                {
                    buscandoDestino = false;
                }
            });
        }, token);
    }

    private void SeleccionarDestino(ClienteDTO destino)
    {
        clienteDestino = destino;
        mensajeError = null;
    }

    private void VolverABusquedaDestino()
    {
        clienteDestino = null;
        mensajeError = null;
    }

    private void CancelarTransferencia()
    {
        vehiculoATransferir = null;
        terminoDestino = string.Empty;
        destinos.Clear();
        clienteDestino = null;
        mensajeError = null;
        _debounceDestinoCts?.Cancel();
        _debounceDestinoCts?.Dispose();
        _debounceDestinoCts = null;
    }

    private async Task TransferirConfirmado()
    {
        if (vehiculoATransferir is null || clienteDestino is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            await VehiculoService.TransferirAsync(vehiculoATransferir.VehiculoId, clienteDestino.Id);
            mensajeExito = $"Vehículo {Formatos.Placa(vehiculoATransferir.Placa)} transferido a {clienteDestino.NombreCompleto}.";
            CancelarTransferencia();
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

        // Cuenta citas futuras (Fecha >= hoy, Estado no Cancelado/Finalizado)
        // Usamos la lista ya cargada en CargarAsync (citas) para evitar query extra.
        citasFuturas = citas.Count(c =>
            c.Fecha >= DateTime.Today
            && c.Estado != EstadoCita.Cancelado
            && c.Estado != EstadoCita.Finalizado);

        mensajeError = null;
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
            await ClienteService.DesactivarAsync(Id);
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

    private void ConfirmarReactivar()
    {
        if (cliente is null) return;
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
        try
        {
            if (await ClienteService.ReactivarAsync(Id))
            {
                mensajeExito = "Cliente reactivado.";
                clienteAReactivar = null;
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

    // ~~~ COMENTARIOS ~~~

    /// <summary>
    /// Si la carga falla se deja la lista vacía y la vista muestra "Sin comentarios",
    /// para que un fallo aquí no rompa el resto de la página.
    /// </summary>
    private async Task CargarComentariosAsync()
    {
        cargandoComentarios = true;
        errorCargaComentarios = false;
        try
        {
            comentarios = await ComentarioService.ObtenerPorClienteAsync(Id);
        }
        catch
        {
            errorCargaComentarios = true;
            comentarios = new();
        }
        finally
        {
            cargandoComentarios = false;
        }
    }

    private void AbrirCrearComentario()
    {
        modeloComentario = new CrearComentarioDTO { ClienteId = Id };
        mensajeError = null;
        mostrarModalComentario = true;
    }

    private void CerrarCrearComentario()
    {
        mostrarModalComentario = false;
        mensajeError = null;
    }

    private async Task GuardarComentario()
    {
        guardando = true;
        mensajeError = null;
        try
        {
            // El autor siempre es el usuario logueado: nunca se elige a mano
            modeloComentario.ClienteId = Id;
            modeloComentario.AutorNombre = autorNombreActual;
            modeloComentario.AutorTipo = autorTipoActual;

            await ComentarioService.CrearAsync(modeloComentario);

            mensajeExito = "Comentario publicado.";
            mostrarModalComentario = false;
            await CargarComentariosAsync();
        }
        catch (ArgumentException ex)
        {
            mensajeError = ex.Message;
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

    private void ConfirmarEliminarComentario(ComentarioDTO comentario)
    {
        comentarioAEliminar = comentario;
        mensajeError = null;
    }

    private void CancelarEliminarComentario()
    {
        comentarioAEliminar = null;
        mensajeError = null;
    }

    private async Task EliminarComentarioConfirmado()
    {
        if (comentarioAEliminar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            await ComentarioService.EliminarAsync(
                comentarioAEliminar.Id, autorNombreActual, autorTipoActual);

            mensajeExito = "Comentario eliminado.";
            comentarioAEliminar = null;
            await CargarComentariosAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            mensajeError = ex.Message;
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

    // El botón de eliminar solo se muestra al autor del comentario.
    private bool EsAutor(ComentarioDTO c) =>
        c.AutorTipo == autorTipoActual
        && string.Equals(c.AutorNombre?.Trim(), autorNombreActual, StringComparison.OrdinalIgnoreCase);

    private static string PrevioComentario(string texto) =>
        texto.Length <= 140 ? texto : texto[..140].TrimEnd() + "…";

    // Los modales de esta página son sin JS: Escape se maneja desde Blazor.
    private void OnKeyDownModalComentario(KeyboardEventArgs e)
    {
        if (e.Key != "Escape" || guardando) return;

        if (comentarioAEliminar is not null) CancelarEliminarComentario();
        else if (mostrarModalComentario) CerrarCrearComentario();
    }

    private bool SinHistoria => cliente is not null
        && cliente.CantidadVehiculos == 0
        && cliente.CantidadCitas == 0;

    public void Dispose()
    {
        // Solo para el CTS del debounce de búsqueda de destino de transferencia.
        // Sin este campo no habría IDisposable en la clase (evita RZ9999 si se añade @implements en el .razor).
        _debounceDestinoCts?.Cancel();
        _debounceDestinoCts?.Dispose();
    }
}