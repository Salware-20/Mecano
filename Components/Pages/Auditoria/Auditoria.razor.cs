using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mecano.Entidad.DTOs.Auditoria;
using Mecano.Logica.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Auditoria;

public partial class Auditoria : ComponentBase
{
    [Inject]
    private IAuditoriaService AuditoriaService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private readonly FiltroAuditoriaDTO filtro = new();
    private List<AuditoriaLogDTO> logs = new();

    private bool cargando = true;
    private string? mensajeExito;
    private string? mensajeError;
    private string? correoOwner;
    private bool tieneCorreoOwner => !string.IsNullOrWhiteSpace(correoOwner);

    // Paginación
    private int paginaActual = 1;
    private int tamanioPagina = 25;
    private int totalRegistros;
    private int totalPaginas => Math.Max(1, (int)Math.Ceiling((double)totalRegistros / tamanioPagina));
    private static readonly int[] TamaniosPagina = [25, 50, 100];

    protected override async Task OnInitializedAsync()
    {
        correoOwner = await AuditoriaService.ObtenerCorreoOwnerAsync();
        await CargarLogsAsync();
    }

    private async Task CargarLogsAsync()
    {
        cargando = true;
        mensajeError = null;

        try
        {
            var resultado = await AuditoriaService.ObtenerPaginadoAsync(filtro, paginaActual, tamanioPagina);
            logs = resultado.Items;
            totalRegistros = resultado.Total;

            if (paginaActual > totalPaginas)
            {
                paginaActual = totalPaginas;
                var reintento = await AuditoriaService.ObtenerPaginadoAsync(filtro, paginaActual, tamanioPagina);
                logs = reintento.Items;
                totalRegistros = reintento.Total;
            }
        }
        catch (Exception ex)
        {
            mensajeError = $"Error al cargar la auditoría: {ex.Message}";
        }
        finally
        {
            cargando = false;
        }
    }

    private async Task AplicarFiltros()
    {
        paginaActual = 1;
        await CargarLogsAsync();
    }

    private async Task LimpiarFiltros()
    {
        filtro.Desde = null;
        filtro.Hasta = null;
        filtro.TipoAccion = null;
        filtro.UsuarioNombre = null;
        paginaActual = 1;
        await CargarLogsAsync();
    }

    private void OnDesdeCambiado(ChangeEventArgs e)
    {
        if (DateTime.TryParse(e.Value?.ToString(), out var f))
            filtro.Desde = f;
        else
            filtro.Desde = null;
    }

    private void OnHastaCambiado(ChangeEventArgs e)
    {
        if (DateTime.TryParse(e.Value?.ToString(), out var f))
            filtro.Hasta = f;
        else
            filtro.Hasta = null;
    }

    private void OnTipoAccionCambiado(ChangeEventArgs e)
    {
        filtro.TipoAccion = string.IsNullOrWhiteSpace(e.Value?.ToString()) ? null : e.Value.ToString();
    }

    // ~~~ EXPORT / NOTIFICAR AL OWNER VIA MAILTO ~~~
    private async Task NotificarOwnerAsync()
    {
        if (string.IsNullOrWhiteSpace(correoOwner))
        {
            mensajeError = "El Admin Global no tiene correo de notificaciones configurado.";
            return;
        }

        try
        {
            // Se toma una muestra representativa de hasta 50 registros del filtro actual
            // para evitar desbordar los límites de caracteres soportados por clientes de correo.
            var muestraResult = await AuditoriaService.ObtenerPaginadoAsync(filtro, 1, 50);
            var muestra = muestraResult.Items;

            var fechaHoy = DateTime.Now.ToString("dd/MM/yyyy");
            var subject = $"[Mecano] Reporte de Movimientos Críticos - {fechaHoy}";

            var sb = new StringBuilder();
            sb.AppendLine("Reporte de Movimientos Críticos — Mecano");
            sb.AppendLine($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Filtros aplicados: {DescribirFiltros()}");
            sb.AppendLine($"Total eventos coincidentes: {muestraResult.Total} (mostrando {muestra.Count} más recientes)");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine();

            foreach (var item in muestra)
            {
                sb.AppendLine($"[{item.FechaHora:dd/MM/yyyy HH:mm:ss}] {item.TipoAccion} | {item.EntidadAfectada} #{item.EntidadId ?? "-"} | Usuario: {item.UsuarioNombre ?? "-"}");
                sb.AppendLine($"Detalles: {item.Detalles ?? "-"}");
                sb.AppendLine(new string('-', 60));
            }

            var subjectEscaped = Uri.EscapeDataString(subject);
            var bodyEscaped = Uri.EscapeDataString(sb.ToString());

            var mailtoUri = $"mailto:{correoOwner}?subject={subjectEscaped}&body={bodyEscaped}";
            NavigationManager.NavigateTo(mailtoUri);

            mensajeExito = "Se ha abierto el cliente de correo con el reporte formateado.";
        }
        catch (Exception ex)
        {
            mensajeError = $"Error al preparar el reporte: {ex.Message}";
        }
    }

    private string DescribirFiltros()
    {
        var partes = new List<string>();
        if (filtro.Desde.HasValue) partes.Add($"Desde: {filtro.Desde.Value:dd/MM/yyyy}");
        if (filtro.Hasta.HasValue) partes.Add($"Hasta: {filtro.Hasta.Value:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(filtro.TipoAccion)) partes.Add($"Acción: {filtro.TipoAccion}");
        return partes.Count > 0 ? string.Join(", ", partes) : "Ninguno (todos los registros)";
    }

    // ~~~ PAGINACIÓN ~~~
    private async Task IrAPagina(int pagina)
    {
        if (pagina < 1 || pagina > totalPaginas || pagina == paginaActual) return;
        paginaActual = pagina;
        await CargarLogsAsync();
    }

    private Task PaginaAnterior() => IrAPagina(paginaActual - 1);
    private Task PaginaSiguiente() => IrAPagina(paginaActual + 1);

    private async Task CambiarTamanioPagina(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var nuevo) && TamaniosPagina.Contains(nuevo))
        {
            tamanioPagina = nuevo;
            paginaActual = 1;
            await CargarLogsAsync();
        }
    }

    private IEnumerable<int> PaginasVisibles()
    {
        const int ventana = 2;
        var inicio = Math.Max(1, paginaActual - ventana);
        var fin = Math.Min(totalPaginas, paginaActual + ventana);
        for (var p = inicio; p <= fin; p++) yield return p;
    }

    private bool MostrarElipsisAntes() => paginaActual - 2 > 1;
    private bool MostrarElipsisDespues() => paginaActual + 2 < totalPaginas;
}
