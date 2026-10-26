using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs.Categoria;
using Mecano.Entidad.DTOs.Mecanico;
using Mecano.Entidad.Utilidades;
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
    // La lista completa (activos + inactivos) se carga una sola vez; el filtro de
    // estado y la búsqueda se aplican en memoria (patrón de /categorias).
    private List<MecanicoDTO> mecanicos = new();
    private List<CategoriaDTO> especialidades = new();
    private bool cargando = true;
    private bool guardando;

    private string terminoBusqueda = string.Empty;
    private FiltroActivo filtro = FiltroActivo.Activos;

    private bool mostrarModal;
    private bool esEdicion;
    private string? mensajeError;       // Se renderiza dentro del modal abierto.
    private string? mensajeExito;       // Banner superior descartable (operaciones fuera de modal).
    private string? mensajeErrorPagina; // Banner superior de error (carga de datos / atajos).

    private ActualizarMecanicoDTO modelo = new();

    // Confirmaciones de estado
    [Inject] private Mecano.Logica.Interfaces.IAuditoriaService AuditoriaService { get; set; } = null!;
    [Inject] private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private MecanicoDTO? mecanicoADesactivar;
    private MecanicoDTO? mecanicoAReactivar;
    private string motivoDesactivacion = string.Empty;

    private string PasswordPlaceholder =>
        esEdicion ? "Conservar la actual" : string.Empty;

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
            mecanicos = await MecanicoService.ObtenerTodosAsync(FiltroActivo.Todos);
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

    // ~~~ FILTROS (en memoria) ~~~

    private List<MecanicoDTO> MecanicosVisibles
    {
        get
        {
            IEnumerable<MecanicoDTO> q = filtro switch
            {
                FiltroActivo.Inactivos => mecanicos.Where(m => !m.Activo),
                FiltroActivo.Todos => mecanicos,
                _ => mecanicos.Where(m => m.Activo)
            };

            var termino = terminoBusqueda.Trim();
            if (termino.Length > 0)
            {
                q = q.Where(m =>
                    Contiene(m.Nombre, termino) ||
                    Contiene(m.Cedula, termino) ||
                    Contiene(Formatos.FormatearIdentificacion(m.Cedula, m.TipoIdentificacion), termino) ||
                    Contiene(m.Email, termino) ||
                    Contiene(m.EspecialidadNombre, termino));
            }

            return q.ToList();
        }
    }

    private static bool Contiene(string? valor, string termino) =>
        !string.IsNullOrEmpty(valor) && valor.Contains(termino, StringComparison.OrdinalIgnoreCase);

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
            return "No se encontraron mecánicos que coincidan con la búsqueda.";
        }

        return filtro switch
        {
            FiltroActivo.Inactivos => "No hay mecánicos inactivos.",
            FiltroActivo.Todos => "No hay mecánicos registrados todavía.",
            _ => "No hay mecánicos activos. Cambia el filtro a «Inactivos» o «Todos»."
        };
    }

    // ~~~ MODAL CREAR / EDITAR ~~~
    private void AbrirCrear()
    {
        // El botón "Nuevo mecánico" se deshabilita cuando especialidades está
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
            Id                 = detalle.Id,
            Nombre             = detalle.Nombre,
            Email              = detalle.Email,
            Cedula             = detalle.Cedula,
            TipoIdentificacion = detalle.TipoIdentificacion,
            Telefono           = detalle.Telefono,
            EspecialidadId     = detalle.EspecialidadId
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
                Nombre             = modelo.Nombre,
                AliasCorreo        = modelo.AliasCorreo,
                Cedula             = modelo.Cedula,
                TipoIdentificacion = modelo.TipoIdentificacion,
                Telefono           = modelo.Telefono,
                EspecialidadId     = modelo.EspecialidadId!.Value,
                Password           = modelo.Password,
                ConfirmarPassword  = modelo.ConfirmarPassword
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
        motivoDesactivacion = string.Empty;
        mensajeError = null;
        mensajeExito = null;
    }

    private void CancelarDesactivar()
    {
        mecanicoADesactivar = null;
        motivoDesactivacion = string.Empty;
        mensajeError = null;
    }

    private async Task DesactivarConfirmado()
    {
        if (mecanicoADesactivar is null) return;

        if (string.IsNullOrWhiteSpace(motivoDesactivacion))
        {
            mensajeError = "Debe indicar el motivo de desactivación.";
            return;
        }

        guardando = true;
        mensajeError = null;
        try
        {
            if (await MecanicoService.DesactivarAsync(mecanicoADesactivar.Id, motivoDesactivacion))
            {
                mensajeExito = $"Mecánico “{mecanicoADesactivar.Nombre}” desactivado.";
                try
                {
                    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                    var user = authState.User;
                    var userId = user.Identity?.Name ?? "Administrador";
                    var userName = user.FindFirst("name")?.Value ?? user.Identity?.Name ?? userId;
                    await AuditoriaService.RegistrarAsync("DESACTIVAR", "Mecanico", mecanicoADesactivar.Id.ToString(), $"Motivo: {motivoDesactivacion.Trim()}", userId, userName);
                }
                catch { }
                mecanicoADesactivar = null;
                motivoDesactivacion = string.Empty;
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

    private void ConfirmarReactivar(MecanicoDTO mecanico)
    {
        mecanicoAReactivar = mecanico;
        mensajeError = null;
        mensajeExito = null;
    }

    private void CancelarReactivar()
    {
        mecanicoAReactivar = null;
        mensajeError = null;
    }

    private async Task ReactivarConfirmado()
    {
        if (mecanicoAReactivar is null) return;

        guardando = true;
        mensajeError = null;
        try
        {
            if (await MecanicoService.ReactivarAsync(mecanicoAReactivar.Id))
            {
                mensajeExito = $"Mecánico “{mecanicoAReactivar.Nombre}” reactivado.";
                try
                {
                    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                    var user = authState.User;
                    var userId = user.Identity?.Name ?? "Administrador";
                    var userName = user.FindFirst("name")?.Value ?? user.Identity?.Name ?? userId;
                    await AuditoriaService.RegistrarAsync("REACTIVAR", "Mecanico", mecanicoAReactivar.Id.ToString(), "Reactivación de mecánico", userId, userName);
                }
                catch { }
                mecanicoAReactivar = null;
                await CargarMecanicos();
            }
            else
            {
                mensajeError = "El mecánico ya no está disponible.";
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
}
