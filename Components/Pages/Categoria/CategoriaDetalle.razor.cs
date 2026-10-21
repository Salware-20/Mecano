using Mecano.Entidad.Constantes;
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

    /// <summary>Id de la categoría ya cargada; evita recargar en cada re-render.</summary>
    private int? idCargado;

    // Filtros en memoria: la lista completa de la categoría ya viene cargada.
    private string terminoBusqueda = string.Empty;
    private FiltroActivo filtro = FiltroActivo.Activos;

    // Modal crear/editar
    private bool mostrarModal;
    private bool esEdicion;
    private int? idEditando;
    private string? mensajeError;
    private string? mensajeExito;
    private CrearServicioDTO modelo = new();

    // Confirmaciones de estado
    private ServicioDTO? servicioADesactivar;
    private ServicioDTO? servicioAReactivar;

    private bool HayModalAbierto =>
        mostrarModal || servicioADesactivar is not null || servicioAReactivar is not null;

    protected override async Task OnParametersSetAsync()
    {
        if (idCargado == Id) return;
        idCargado = Id;
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        cargando = true;
        try
        {
            categoria = await CategoriaService.ObtenerPorIdAsync(Id);
            servicios = await ServicioService.ObtenerPorCategoriaAsync(Id, soloActivos: false);
        }
        finally
        {
            cargando = false;
        }
    }

    // ~~~ Filtros (búsqueda + estado) ~~~

    private List<ServicioDTO> ServiciosVisibles
    {
        get
        {
            IEnumerable<ServicioDTO> q = filtro switch
            {
                FiltroActivo.Inactivos => servicios.Where(s => !s.Activo),
                FiltroActivo.Todos => servicios,
                _ => servicios.Where(s => s.Activo)
            };

            var termino = terminoBusqueda.Trim();
            if (termino.Length > 0)
            {
                q = q.Where(s =>
                    s.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                    (s.Descripcion?.Contains(termino, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return q.OrderBy(s => s.Nombre).ToList();
        }
    }

    private void OnBuscarCambio(ChangeEventArgs e) =>
        terminoBusqueda = e.Value?.ToString() ?? string.Empty;

    private void LimpiarBusqueda() => terminoBusqueda = string.Empty;

    private void CambiarFiltro(ChangeEventArgs e)
    {
        if (Enum.TryParse<FiltroActivo>(e.Value?.ToString(), out var valor))
        {
            filtro = valor;
        }
    }

    private string TextoVacio()
    {
        if (terminoBusqueda.Trim().Length > 0)
        {
            return "No se encontraron servicios que coincidan con la búsqueda.";
        }

        return filtro switch
        {
            FiltroActivo.Inactivos => "No hay servicios desactivados en esta categoría.",
            FiltroActivo.Todos => "Esta categoría aún no tiene servicios registrados.",
            _ => "No hay servicios activos. Cambia el filtro a «Inactivos» o «Todos»."
        };
    }

    // ~~~ Crear / editar (modal) ~~~

    private void AbrirCrear()
    {
        esEdicion = false;
        idEditando = null;
        modelo = new CrearServicioDTO();
        mensajeError = null;
        mensajeExito = null;
        mostrarModal = true;
    }

    private async Task AbrirEditar(ServicioDTO servicio)
    {
        // Se recarga la fila para editar contra datos autoritativos (y poder editar inactivos).
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
        mensajeExito = null;
        mostrarModal = true;
    }

    private void CerrarModal()
    {
        mostrarModal = false;
        mensajeError = null;
        idEditando = null;
    }

    // Actualiza los hints mientras el usuario escribe: el
    // @bind-Value de InputNumber solo engancha onchange (se dispara al salir del campo),
    // así que estos handlers refrescan el modelo en cada tecla. e.Value es el texto crudo
    // del <input> (null si queda vacío); si no parsea, se conserva el último valor válido
    // para no reescribir lo que el usuario está tecleando.
    private void OnDuracionInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var minutos))
            modelo.DuracionMinutos = minutos;
    }

    private void OnPrecioInput(ChangeEventArgs e)
    {
        var texto = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(texto))
            return;

        if (decimal.TryParse(texto.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var precio))
        {
            if (precio >= 0)
                modelo.Precio = precio;
        }
    }

    private async Task Guardar()
    {
        guardando = true;
        mensajeError = null;
        try
        {
            // La página está acotada a una categoría: el servicio siempre pertenece a ella.
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

    // ~~~ Desactivar / reactivar ~~~

    private void ConfirmarDesactivar(ServicioDTO servicio)
    {
        servicioADesactivar = servicio;
        mensajeError = null;
        mensajeExito = null;
    }

    private void CancelarDesactivar()
    {
        servicioADesactivar = null;
        mensajeError = null;
    }

    private async Task DesactivarConfirmado()
    {
        if (servicioADesactivar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            await ServicioService.EliminarAsync(servicioADesactivar.Id);
            mensajeExito = $"Servicio “{servicioADesactivar.Nombre}” desactivado.";
            servicioADesactivar = null;
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

    private void ConfirmarReactivar(ServicioDTO servicio)
    {
        servicioAReactivar = servicio;
        mensajeError = null;
        mensajeExito = null;
    }

    private void CancelarReactivar()
    {
        servicioAReactivar = null;
        mensajeError = null;
    }

    private async Task ReactivarConfirmado()
    {
        if (servicioAReactivar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            if (await ServicioService.ReactivarAsync(servicioAReactivar.Id))
            {
                mensajeExito = $"Servicio “{servicioAReactivar.Nombre}” reactivado.";
                servicioAReactivar = null;
                await CargarAsync();
            }
            else
            {
                mensajeError = "El servicio ya no está disponible.";
            }
        }
        finally
        {
            guardando = false;
        }
    }
}
