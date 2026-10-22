using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Mecano.Components.Pages.Mecanico;

/// <summary>
/// Agenda personal del mecánico (/mecanico/citas): solo sus citas, con filtros y
/// paginación como el listado de administración pero sin selector de mecánico
/// (el id siempre sale del claim; el servicio lo fuerza) y con la proyección
/// restringida <see cref="CitaListaMecanicoDTO"/>: cliente solo por nombre,
/// colegas solo por nombre y sin precios.
/// </summary>
public partial class MisCitas : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private List<CitaListaMecanicoDTO> citas = new();
    private bool cargando = true;
    private string? mensajeError;
    private int mecanicoId;

    // ~~~ FILTROS (sin MecanicoId: lo fuerza el servicio con el claim) ~~~
    private DateTime? desde;
    private DateTime? hasta;
    private EstadoCita? filtroEstado;
    private string terminoBusqueda = string.Empty;

    // ~~~ PAGINACIÓN ~~~
    private int paginaActual = 1;
    private int tamanioPagina = 20;
    private int totalRegistros;
    private int totalPaginas;

    private static readonly int[] TamaniosPagina = { 10, 20, 50, 100 };
    private static readonly EstadoCita[] EstadosCita = Enum.GetValues<EstadoCita>();

    protected override async Task OnInitializedAsync()
    {
        if (AuthState is not null)
        {
            var state = await AuthState;
            mecanicoId = int.TryParse(
                state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
        }

        await CargarAsync();
    }

    // ~~~ CARGA ~~~
    private FiltroCitasDTO ConstruirFiltro() => new()
    {
        Desde = desde,
        Hasta = hasta,
        Estado = filtroEstado,
        Termino = string.IsNullOrWhiteSpace(terminoBusqueda) ? null : terminoBusqueda
    };

    private async Task CargarAsync()
    {
        cargando = true;
        mensajeError = null;
        try
        {
            if (mecanicoId == 0)
            {
                citas = new();
                totalRegistros = 0;
                totalPaginas = 1;
                mensajeError = "No se pudo identificar tu cuenta. Cierra sesión y vuelve a entrar.";
                return;
            }

            var filtro = ConstruirFiltro();

            totalRegistros = await CitaService.ContarParaMecanicoAsync(mecanicoId, filtro);
            totalPaginas = totalRegistros == 0
                ? 1
                : (int)Math.Ceiling(totalRegistros / (double)tamanioPagina);

            // Si la página quedó fuera de rango tras un filtro, retrocedemos.
            if (paginaActual > totalPaginas)
                paginaActual = totalPaginas;

            citas = await CitaService.BuscarParaMecanicoAsync(mecanicoId, filtro, paginaActual, tamanioPagina);
        }
        catch (Exception)
        {
            citas = new();
            totalRegistros = 0;
            totalPaginas = 1;
            mensajeError = "No se pudieron cargar tus citas. Inténtalo de nuevo.";
        }
        finally
        {
            cargando = false;
        }
    }

    /// <summary>Recarga tras una transición de estado (Iniciar/Finalizar) desde una fila.</summary>
    private Task RecargarTrasTransicion() => CargarAsync();

    // ~~~ FILTROS / PAGINACIÓN ~~~
    private async Task AplicarFiltros()
    {
        paginaActual = 1;
        await CargarAsync();
    }

    private async Task LimpiarFiltros()
    {
        desde = null;
        hasta = null;
        filtroEstado = null;
        terminoBusqueda = string.Empty;
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

    private bool HayFiltros() =>
        desde is not null
        || hasta is not null
        || filtroEstado is not null
        || !string.IsNullOrWhiteSpace(terminoBusqueda);

    private string TextoVacio() =>
        HayFiltros()
            ? "No encontramos citas tuyas que coincidan con los filtros."
            : "Todavía no tienes citas asignadas.";
}
