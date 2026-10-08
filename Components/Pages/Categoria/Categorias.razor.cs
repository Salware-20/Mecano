using Mecano.Entidad.DTOs.Categoria;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Categoria
{
    public partial class Categorias : ComponentBase
    {
        private List<CategoriaDTO> categorias = new();
        private bool cargando = true;
        private bool guardando;

        private bool mostrarModal;
        private bool esEdicion;
        private string? mensajeError;

        private CrearCategoriaDTO modelo = new();
        private CategoriaDTO? categoriaAEliminar;

        protected override async Task OnInitializedAsync()
        {
            await CargarCategorias();
        }

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

        private void AbrirCrear()
        {
            esEdicion = false;
            modelo = new CrearCategoriaDTO();
            mensajeError = null;
            mostrarModal = true;
        }

        private void AbrirEditar(CategoriaDTO cat)
        {
            esEdicion = true;
            modelo = new CrearCategoriaDTO
            {
                Nombre = cat.Nombre,
                Descripcion = cat.Descripcion
            };
            // Guardamos el Id en un campo aparte para el update
            idEditando = cat.Id;
            mensajeError = null;
            mostrarModal = true;
        }

        private int? idEditando;

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
                }
                else
                {
                    await CategoriaService.CrearAsync(modelo);
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

        private void ConfirmarEliminar(CategoriaDTO cat)
        {
            categoriaAEliminar = cat;
            mensajeError = null;
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
                await CategoriaService.EliminarAsync(categoriaAEliminar.Id);
                categoriaAEliminar = null;
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
