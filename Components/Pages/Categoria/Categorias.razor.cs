using Mecano.Entidad.DTOs.Categoria;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Categoria
{
    public partial class Categorias : ComponentBase
    {
        private List<CategoriaDTO> categorias = new();
        private bool cargando = true;
        private bool guardando;

        // Modal crear/editar
        private bool mostrarModal;
        private bool esEdicion;
        private int? idEditando;
        private string? mensajeError;
        private string? mensajeExito;
        private CrearCategoriaDTO modelo = new();

        // Confirmación de borrado
        private CategoriaDTO? categoriaAEliminar;

        // Búsqueda en memoria: la lista completa ya viene cargada.
        private string terminoBusqueda = string.Empty;

        private bool HayModalAbierto => mostrarModal || categoriaAEliminar is not null;

        protected override async Task OnInitializedAsync() => await CargarCategorias();

        private async Task CargarCategorias()
        {
            cargando = true;
            try
            {
                categorias = await CategoriaService.ObtenerTodasAsync();
            }
            finally
            {
                cargando = false;
            }
        }

        // ~~~ Búsqueda ~~~

        private List<CategoriaDTO> CategoriasVisibles
        {
            get
            {
                var termino = terminoBusqueda.Trim();
                if (termino.Length == 0)
                {
                    return categorias.OrderBy(c => c.Nombre).ToList();
                }

                return categorias
                    .Where(c =>
                        c.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                        (c.Descripcion?.Contains(termino, StringComparison.OrdinalIgnoreCase) ?? false))
                    .OrderBy(c => c.Nombre)
                    .ToList();
            }
        }

        private void OnBuscarCambio(ChangeEventArgs e) =>
            terminoBusqueda = e.Value?.ToString() ?? string.Empty;

        private void LimpiarBusqueda() => terminoBusqueda = string.Empty;

        private string TextoVacio() =>
            terminoBusqueda.Trim().Length > 0
                ? "No se encontraron categorías que coincidan con la búsqueda."
                : "Aún no hay categorías registradas. Crea la primera para agrupar los servicios del taller.";

        // ~~~ Crear / editar ~~~

        private void AbrirCrear()
        {
            esEdicion = false;
            idEditando = null;
            modelo = new CrearCategoriaDTO();
            mensajeError = null;
            mensajeExito = null;
            mostrarModal = true;
        }

        private void AbrirEditar(CategoriaDTO categoria)
        {
            esEdicion = true;
            idEditando = categoria.Id;
            modelo = new CrearCategoriaDTO
            {
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion
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

        private async Task Guardar()
        {
            guardando = true;
            mensajeError = null;
            try
            {
                if (esEdicion && idEditando.HasValue)
                {
                    await CategoriaService.ActualizarAsync(new ActualizarCategoriaDTO
                    {
                        Id = idEditando.Value,
                        Nombre = modelo.Nombre,
                        Descripcion = modelo.Descripcion
                    });
                    mensajeExito = $"Categoría «{modelo.Nombre}» actualizada.";
                }
                else
                {
                    await CategoriaService.CrearAsync(modelo);
                    mensajeExito = $"Categoría «{modelo.Nombre}» creada.";
                }

                CerrarModal();
                await CargarCategorias();
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

        // ~~~ Eliminar ~~~

        private void ConfirmarEliminar(CategoriaDTO categoria)
        {
            categoriaAEliminar = categoria;
            mensajeError = null;
            mensajeExito = null;
        }

        private void CancelarEliminar()
        {
            categoriaAEliminar = null;
            mensajeError = null;
        }

        private async Task EliminarConfirmado()
        {
            if (categoriaAEliminar is null) return;

            guardando = true;
            mensajeError = null;
            try
            {
                var nombre = categoriaAEliminar.Nombre;
                await CategoriaService.EliminarAsync(categoriaAEliminar.Id);
                categoriaAEliminar = null;
                mensajeExito = $"Categoría «{nombre}» eliminada.";
                await CargarCategorias();
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
    }
}
