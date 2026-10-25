using System.Security.Claims;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs.Administrador;
using Mecano.Entidad.Utilidades;
using Mecano.Logica.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Mecano.Components.Pages.Administradores;

public partial class Administradores : ComponentBase
{
    [Inject] private IAdministradorService AdministradorService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<AdministradorListaDTO> administradores = new();
    private List<AdministradorListaDTO> AdministradoresFiltrados = new();
    private bool cargando = true;
    private bool guardando;
    private string? mensajeExito;
    private string? mensajeErrorPagina;
    private string? mensajeError;

    private string terminoBusqueda = string.Empty;
    private string filtroEstado = "todos";

    private bool mostrarModal;
    private bool esEdicion;
    private int? adminEditandoId;
    private string? adminEmailActual;

    private CrearAdministradorDTO crearModelo = new();
    private ActualizarAdministradorDTO modelo = new();

    private AdministradorListaDTO? adminADesactivar;
    private AdministradorListaDTO? adminAReactivar;

    private int adminActualId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                adminActualId = id;
            }
        }

        await CargarAdministradores();
    }

    private async Task CargarAdministradores()
    {
        cargando = true;
        try
        {
            administradores = await AdministradorService.ListarAsync(true);
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            mensajeErrorPagina = ex.Message;
        }
        finally
        {
            cargando = false;
        }
    }

    private void OnBuscarCambio(ChangeEventArgs e)
    {
        terminoBusqueda = e.Value?.ToString() ?? string.Empty;
        AplicarFiltros();
    }

    private void LimpiarBusqueda()
    {
        terminoBusqueda = string.Empty;
        AplicarFiltros();
    }

    private void CambiarFiltro(ChangeEventArgs e)
    {
        filtroEstado = e.Value?.ToString() ?? "todos";
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        IEnumerable<AdministradorListaDTO> query = administradores;

        if (filtroEstado == "activos")
            query = query.Where(a => a.Activo);
        else if (filtroEstado == "inactivos")
            query = query.Where(a => !a.Activo);

        if (!string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            var term = terminoBusqueda.Trim().ToLower();
            query = query.Where(a =>
                (a.Nombre?.ToLower().Contains(term) ?? false) ||
                (a.Email?.ToLower().Contains(term) ?? false) ||
                (a.Cedula?.ToLower().Contains(term) ?? false));
        }

        AdministradoresFiltrados = query.ToList();
    }

    private void AbrirCrear()
    {
        esEdicion = false;
        adminEditandoId = null;
        adminEmailActual = null;
        crearModelo = new CrearAdministradorDTO();
        modelo = new ActualizarAdministradorDTO();
        mensajeError = null;
        mostrarModal = true;
    }

    private void AbrirEditar(AdministradorListaDTO admin)
    {
        esEdicion = true;
        adminEditandoId = admin.AdministradorId;
        adminEmailActual = admin.Email;
        crearModelo = new CrearAdministradorDTO();
        modelo = new ActualizarAdministradorDTO
        {
            AdministradorId = admin.AdministradorId,
            Nombre = admin.Nombre,
            TipoIdentificacion = admin.TipoIdentificacion,
            Cedula = admin.Cedula,
            Telefono = admin.Telefono,
            CorreoNotificaciones = admin.CorreoNotificaciones
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
        mensajeExito = null;
        mensajeErrorPagina = null;

        try
        {
            if (!esEdicion)
            {
                var id = await AdministradorService.CrearAsync(crearModelo, adminActualId);
                mensajeExito = "Administrador creado correctamente.";
            }
            else if (adminEditandoId.HasValue)
            {
                modelo.AdministradorId = adminEditandoId.Value;
                await AdministradorService.ActualizarAsync(modelo);
                mensajeExito = "Administrador actualizado correctamente.";
            }

            mostrarModal = false;
            await CargarAdministradores();
        }
        catch (Exception ex)
        {
            mensajeError = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }

    private void ConfirmarDesactivar(AdministradorListaDTO admin)
    {
        if (admin.EsAdminGlobal || admin.AdministradorId == adminActualId) return;
        adminADesactivar = admin;
    }

    private void CancelarDesactivar()
    {
        adminADesactivar = null;
    }

    private async Task DesactivarConfirmado()
    {
        if (adminADesactivar is null) return;
        guardando = true;
        mensajeExito = null;
        mensajeErrorPagina = null;

        try
        {
            await AdministradorService.DesactivarAsync(adminADesactivar.AdministradorId, adminActualId);
            mensajeExito = "Administrador desactivado correctamente.";
            adminADesactivar = null;
            await CargarAdministradores();
        }
        catch (Exception ex)
        {
            mensajeErrorPagina = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }

    private void ConfirmarReactivar(AdministradorListaDTO admin)
    {
        if (admin.EsAdminGlobal) return;
        adminAReactivar = admin;
    }

    private void CancelarReactivar()
    {
        adminAReactivar = null;
    }

    private async Task ReactivarConfirmado()
    {
        if (adminAReactivar is null) return;
        guardando = true;
        mensajeExito = null;
        mensajeErrorPagina = null;

        try
        {
            await AdministradorService.ReactivarAsync(adminAReactivar.AdministradorId);
            mensajeExito = "Administrador reactivado correctamente.";
            adminAReactivar = null;
            await CargarAdministradores();
        }
        catch (Exception ex)
        {
            mensajeErrorPagina = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }
}
