using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Mecanico;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Citas;

public partial class Citas : ComponentBase
{
    private List<CitaDTO> citas = new();
    private List<MecanicoDTO> mecanicos = new();
    private bool cargando = true;

    // ~~~ FILTROS ~~~
    private DateTime? desde;
    private DateTime? hasta;
    private int? filtroMecanicoId;
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
        // El selector de mecánico incluye inactivos: las citas históricas siguen
        // apuntando a mecánicos que pueden haber sido desactivados.
        mecanicos = await MecanicoService.ObtenerTodosAsync(FiltroActivo.Todos);
        await CargarAsync();
    }

    // ~~~ CARGA ~~~
    private FiltroCitasDTO ConstruirFiltro() => new()
    {
        Desde = desde,
        Hasta = hasta,
        MecanicoId = filtroMecanicoId,
        Estado = filtroEstado,
        Termino = string.IsNullOrWhiteSpace(terminoBusqueda) ? null : terminoBusqueda
    };

    private async Task CargarAsync()
    {
        cargando = true;
        try
        {
            var filtro = ConstruirFiltro();

            totalRegistros = await CitaService.ContarAsync(filtro);
            totalPaginas = totalRegistros == 0
                ? 1
                : (int)Math.Ceiling(totalRegistros / (double)tamanioPagina);

            // Si la página quedó fuera de rango tras un filtro, retrocedemos.
            if (paginaActual > totalPaginas)
                paginaActual = totalPaginas;

            citas = await CitaService.BuscarPaginadoAsync(filtro, paginaActual, tamanioPagina);
        }
        finally
        {
            cargando = false;
        }
    }

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
        filtroMecanicoId = null;
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
        || filtroMecanicoId is not null
        || filtroEstado is not null
        || !string.IsNullOrWhiteSpace(terminoBusqueda);

    private string TextoVacio() =>
        HayFiltros()
            ? "No se encontraron citas que coincidan con los filtros."
            : "No hay citas registradas todavía.";
}
