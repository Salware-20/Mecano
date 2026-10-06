using Mecano.Entidad.DTOs.Categoria;
using Mecano.Entidad.DTOs.Mecanico;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Mecanicos;

// NOTA sobre namespaces: este archivo importa Mecano.Entidad.DTOs.Mecanico (una
// hoja cuyos miembros son MecanicoDTO/CrearMecanicoDTO/ActualizarMecanicoDTO) y
// Mecano.Entidad.DTOs.Categoria, pero NUNCA Mecano.Entidad.Clases ni el namespace
// padre Mecano.Entidad.DTOs. Por eso el nombre simple "Mecanico" no resuelve a
// nada ambiguo aquí y NO se necesita el alias MecanicoClase (CS0118). Si en el
// futuro este archivo necesita la entidad Mecanico (clase), agregar:
//     using MecanicoClase = Mecano.Entidad.Clases.Mecanico;
// y usar MecanicoClase en lugar de un using plano. Ver PROJECT_STATE.md §11.
public partial class Mecanicos : ComponentBase
{
    private List<MecanicoDTO> mecanicos = new();
    private List<CategoriaDTO> especialidades = new();
    private bool cargando = true;
    private bool guardando;
    private bool mostrarInactivos;

    private bool mostrarModal;
    private bool esEdicion;
    private string? mensajeError;      // Se renderiza dentro del modal abierto.
    private string? mensajeExito;      // Banner superior descartable (operaciones fuera de modal).
    private string? mensajeErrorPagina; // Banner superior de error (carga de datos / atajos).

    private ActualizarMecanicoDTO modelo = new();
    private MecanicoDTO? mecanicoADesactivar;

    private string PasswordPlaceholder =>
        esEdicion ? "Dejar en blanco para conservar la actual" : string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await CargarMecanicos();
        await CargarEspecialidades();
    }

    // ~~~ CARGA ~~~
    private async Task CargarMecanicos()
    {
        cargando = true;
        try
        {
            mecanicos = await MecanicoService.ObtenerTodosAsync(soloActivos: !mostrarInactivos);
        }
        finally
        {
            cargando = false;
        }
    }

    private async Task CargarEspecialidades()
    {
        try
        {
            especialidades = await CategoriaService.ObtenerTodasAsync();
        }
        catch
        {
            // Degradación elegante (decisión Q6a): la lista de mecánicos sigue
            // siendo útil aunque falle el catálogo; Crear/Editar quedan bloqueados.
            especialidades = new();
        }

        if (especialidades.Count == 0)
            mensajeErrorPagina = "No se pudieron cargar las especialidades. Recarga la página.";
    }

    private async Task CambiarMostrarInactivos(bool valor)
    {
        mostrarInactivos = valor;
        await CargarMecanicos();
    }

    // ~~~ MODAL CREAR / EDITAR ~~~
    private void AbrirCrear()
    {
        // El botón "Nuevo mecánico" no se renderiza cuando especialidades está
        // vacía, así que aquí no hace falta cortar el flujo.
        esEdicion = false;
        modelo = new ActualizarMecanicoDTO(); // Id queda en 0 en creación.
        mensajeError = null;
        mostrarModal = true;
    }

    private async Task AbrirEditar(MecanicoDTO mecanico)
    {
        // Decisión (Q6a): si no hay especialidades cargadas NO se abre el modal;
        // se corta el flujo y se avisa con el banner de error de página. Abrir un
        // modal que no se podría guardar sería engañoso.
        if (especialidades.Count == 0)
        {
            mensajeErrorPagina = "No se pueden editar mecánicos sin especialidades cargadas. Recarga la página.";
            return;
        }

        // Recargamos la fila contra datos autoritativos antes de editar (patrón Clientes).
        var detalle = await MecanicoService.ObtenerDetallePorIdAsync(mecanico.Id);
        if (detalle is null)
        {
            mensajeErrorPagina = "El mecánico ya no está disponible.";
            await CargarMecanicos();
            return;
        }

        esEdicion = true;
        modelo = new ActualizarMecanicoDTO
        {
            Id = detalle.Id,
            Nombre = detalle.Nombre,
            Email = detalle.Email,
            Cedula = detalle.Cedula,
            Telefono = detalle.Telefono,
            EspecialidadId = detalle.EspecialidadId
        };
        mensajeError = null;
        mostrarModal = true;
    }

    private void CerrarModal()
    {
        mostrarModal = false;
        mensajeError = null;
    }

    // En edición, en blanco = conservar el hash actual: se escribe null en el
    // modelo (no "") para que StringLength(6..200) no bloquee el submit cuando el
    // admin escribe y luego borra el campo. Estos setters reemplazan a la
    // asignación directa del @bind; la validación del campo se dispara después
    // (InputBase notifica el cambio tras invocar ValueChanged) con el valor ya
    // normalizado. En creación se asigna el valor tal cual: "" lo bloquea
    // StringLength y null lo captura la guardia de Guardar.
    private void AsignarPassword(string? valor)
        => modelo.Password = esEdicion && string.IsNullOrWhiteSpace(valor) ? null : valor;

    private void AsignarConfirmarPassword(string? valor)
        => modelo.ConfirmarPassword = esEdicion && string.IsNullOrWhiteSpace(valor) ? null : valor;

    private async Task Guardar()
    {
        guardando = true;
        mensajeError = null;
        try
        {
            if (esEdicion)
            {
                if (await MecanicoService.ActualizarAsync(modelo))
                {
                    mensajeExito = $"Mecánico “{modelo.Nombre}” actualizado correctamente.";
                    CerrarModal();
                    await CargarMecanicos();
                }
                else
                {
                    mensajeError = "El mecánico ya no existe.";
                }
                return;
            }

            // Guardas ANTES del mapeo: CrearAsync nunca debe recibir una
            // contraseña vacía (el servicio no la valida en creación).
            if (string.IsNullOrWhiteSpace(modelo.Password))
            {
                mensajeError = "La contraseña es obligatoria.";
                return;
            }
            if (modelo.Password != modelo.ConfirmarPassword)
            {
                mensajeError = "Las contraseñas no coinciden.";
                return;
            }

            await MecanicoService.CrearAsync(new CrearMecanicoDTO
            {
                // modelo.Id queda en 0: CrearMecanicoDTO no tiene campo Id.
                Nombre           = modelo.Nombre,
                Email            = modelo.Email,
                Cedula           = modelo.Cedula,
                Telefono         = modelo.Telefono,
                EspecialidadId   = modelo.EspecialidadId!.Value,
                Password         = modelo.Password,
                ConfirmarPassword = modelo.ConfirmarPassword
            });
            mensajeExito = $"Mecánico “{modelo.Nombre}” creado correctamente.";
            CerrarModal();
            await CargarMecanicos();
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

    // ~~~ DESACTIVAR / REACTIVAR ~~~
    private void ConfirmarDesactivar(MecanicoDTO mecanico)
    {
        mecanicoADesactivar = mecanico;
        mensajeError = null;
    }

    private void CancelarDesactivar()
    {
        mecanicoADesactivar = null;
        mensajeError = null;
    }

    private async Task DesactivarConfirmado()
    {
        if (mecanicoADesactivar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            if (await MecanicoService.DesactivarAsync(mecanicoADesactivar.Id))
            {
                mensajeExito = $"Mecánico “{mecanicoADesactivar.Nombre}” desactivado.";
                mecanicoADesactivar = null;
                await CargarMecanicos();
            }
            else
            {
                mensajeError = "El mecánico ya no existe.";
            }
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

    // A diferencia de Clientes —donde reactivar un cliente también reactiva sus
    // vehículos y por eso existe el modal "Confirmar reactivación"—, aquí
    // ReactivarAsync solo cambia Activo a true: sin cascada, sin datos que
    // validar y con la fila ya marcada "Inactivo" a la vista. Se ejecuta directo
    // y el resultado se informa con el banner de éxito. La asimetría con
    // Clientes es deliberada, no un olvido.
    private async Task Reactivar(MecanicoDTO mecanico)
    {
        guardando = true;
        mensajeErrorPagina = null;
        try
        {
            if (await MecanicoService.ReactivarAsync(mecanico.Id))
            {
                mensajeExito = $"Mecánico “{mecanico.Nombre}” reactivado.";
                await CargarMecanicos();
            }
            else
            {
                mensajeErrorPagina = "El mecánico ya no existe.";
                await CargarMecanicos();
            }
        }
        catch (InvalidOperationException ex)
        {
            mensajeErrorPagina = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }
}
