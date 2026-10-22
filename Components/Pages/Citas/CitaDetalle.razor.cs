using System.Globalization;
using System.Security.Claims;
using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Mecanico;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Mecano.Components.Pages.Citas;

public partial class CitaDetalle : ComponentBase
{
    // Ventana de atención del taller (los mismos límites que usa el tablero de agendado).
    private static readonly TimeOnly Apertura = new(8, 0);
    private static readonly TimeOnly Cierre = new(18, 0);
    private const int PasoMinutos = 30;

    [Parameter]
    public int Id { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private CitaDetalleDTO? cita;

    // --- Vista del mecánico: proyección restringida de la misma cita ---
    private CitaDetalleMecanicoDTO? citaMec;
    private bool esAdmin;
    private int mecanicoId;

    private bool cargando = true;

    // --- Edición (Fase 2: vehículo, servicio y notas) ---
    private bool mostrarModalEditar;
    private bool guardando;
    private string? mensajeError;
    private string? mensajeExito;
    private EditarCitaDTO modeloEditar = new();
    private List<Vehiculo> vehiculos = new();
    private List<ServicioDTO> servicios = new();

    // --- Reprogramación (Fase 3: fecha, hora y mecánico) ---
    private bool mostrarModalReprogramar;
    private ReprogramarCitaDTO modeloReprogramar = new();
    private List<MecanicoDTO> mecanicos = new();
    private HashSet<int> selMecanicosIds = new();
    private List<Cita> citasDelDia = new();
    private string horaTexto = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        cargando = true;
        mostrarModalEditar = false;
        mostrarModalReprogramar = false;
        mensajeError = null;
        mensajeExito = null;
        try
        {
            // La bifurcación sale del claim de la sesión (por defecto, rama restringida:
            // si no hubiera estado de autenticación no se cargaría ningún DTO admin).
            esAdmin = false;
            cita = null;
            citaMec = null;

            if (AuthState is not null)
            {
                var state = await AuthState;
                esAdmin = state.User.IsInRole(Roles.Administrador);
                if (!esAdmin)
                {
                    mecanicoId = int.TryParse(
                        state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
                }
            }

            if (esAdmin)
            {
                cita = await CitaService.ObtenerDetallePorIdAsync(Id);
            }
            else
            {
                // El servicio exige que la cita esté asignada al mecánico: teclear
                // una URL a mano no abre citas ajenas.
                citaMec = mecanicoId == 0
                    ? null
                    : await CitaService.ObtenerDetalleParaMecanicoAsync(Id, mecanicoId);
            }
        }
        finally
        {
            cargando = false;
        }
    }

    /// <summary>Recarga la rama correspondiente (tras Iniciar/Finalizar en la cabecera).</summary>
    private async Task Recargar()
    {
        if (esAdmin)
        {
            cita = await CitaService.ObtenerDetallePorIdAsync(Id);
        }
        else if (mecanicoId != 0)
        {
            citaMec = await CitaService.ObtenerDetalleParaMecanicoAsync(Id, mecanicoId);
        }
    }

    private static bool EsEditable(EstadoCita estado) =>
        estado != EstadoCita.Cancelado && estado != EstadoCita.Finalizado;

    // ~~~ Edición (Fase 2) ~~~
    private async Task AbrirEditar()
    {
        if (cita is null)
            return;

        mensajeError = null;
        mensajeExito = null;
        modeloEditar = new EditarCitaDTO
        {
            Id = cita.Id,
            VehiculoId = cita.VehiculoId,
            ServicioId = cita.ServicioId,
            NotasAdmin = cita.NotasAdmin
        };

        // Vehículos activos del cliente; si el vehículo actual está inactivo, se conserva
        // como opción para no forzar un cambio que el usuario no pidió.
        vehiculos = await VehiculoService.ObtenerPorClienteAsync(cita.ClienteId);
        if (vehiculos.All(v => v.VehiculoId != cita.VehiculoId))
        {
            vehiculos.Insert(0, new Vehiculo
            {
                VehiculoId = cita.VehiculoId,
                ClienteId = cita.ClienteId,
                Placa = cita.VehiculoPlaca ?? string.Empty,
                Marca = cita.VehiculoMarca ?? string.Empty,
                Modelo = cita.VehiculoModelo ?? string.Empty,
                Anio = cita.VehiculoAnio,
                Activo = false
            });
        }

        // Servicios de la especialidad de los mecánicos asignados (compartida); el servicio
        // actual se conserva como opción aunque no pertenezca a esa categoría (datos heredados).
        var especialidadId = cita.Mecanicos.FirstOrDefault()?.EspecialidadId ?? 0;
        servicios = especialidadId > 0
            ? await ServicioService.ObtenerPorCategoriaAsync(especialidadId)
            : new List<ServicioDTO>();

        if (servicios.All(s => s.Id != cita.ServicioId))
        {
            servicios.Insert(0, new ServicioDTO
            {
                Id = cita.ServicioId,
                Nombre = cita.ServicioNombre ?? "Servicio actual",
                DuracionMinutos = cita.ServicioDuracionMinutos,
                Precio = cita.ServicioPrecio,
                CategoriaId = especialidadId > 0 ? especialidadId : null,
                Activo = false
            });
        }

        mostrarModalEditar = true;
    }

    private void CerrarEditar()
    {
        if (guardando)
            return;

        mostrarModalEditar = false;
        mensajeError = null;
    }

    private async Task GuardarEdicion()
    {
        if (cita is null)
            return;

        guardando = true;
        mensajeError = null;
        try
        {
            var actualizado = await CitaService.ActualizarCitaAsync(modeloEditar);
            if (!actualizado)
            {
                mensajeError = "La cita ya no existe.";
                return;
            }

            cita = await CitaService.ObtenerDetallePorIdAsync(Id);
            mostrarModalEditar = false;
            mensajeExito = "Cita actualizada correctamente.";
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

    // ~~~ Reprogramación (Fase 3) ~~~
    private string FechaMinima => DateTime.Today.ToString("yyyy-MM-dd");

    // Opciones de hora (paso de 30 min) con su etiqueta y si el tramo está disponible.
    private IEnumerable<(string Valor, string Etiqueta, bool Habilitada)> OpcionesHora
    {
        get
        {
            for (var t = Apertura; t < Cierre; t = t.AddMinutes(PasoMinutos))
            {
                yield return (
                    t.ToString("HH:mm", CultureInfo.InvariantCulture),
                    t.ToString("h:mm tt", CultureInfo.InvariantCulture),
                    HoraHabilitada(t));
            }
        }
    }

    private async Task AbrirReprogramar()
    {
        if (cita is null)
            return;

        mensajeError = null;
        mensajeExito = null;
        // Se parte de los mecánicos actualmente asignados que siguen activos; los inactivos
        // se excluyen porque la reprogramación debe dejar solo mecánicos activos.
        selMecanicosIds = cita.Mecanicos.Where(m => m.Activo).Select(m => m.Id).ToHashSet();
        modeloReprogramar = new ReprogramarCitaDTO
        {
            Id = cita.Id,
            MecanicosIds = selMecanicosIds.ToList(),
            Fecha = cita.Fecha.Date,
            HoraInicio = cita.HoraInicio
        };
        horaTexto = cita.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture);

        // Solo se ofrecen mecánicos activos: reprogramar sirve, entre otras cosas,
        // para reasignar una cita de un mecánico inactivo a uno disponible.
        mecanicos = await MecanicoService.ObtenerTodosAsync(FiltroActivo.Activos);

        await CargarOcupados();
        mostrarModalReprogramar = true;
    }

    private void CerrarReprogramar()
    {
        if (guardando)
            return;

        mostrarModalReprogramar = false;
        mensajeError = null;
    }

    private async Task CargarOcupados()
    {
        if (cita is null || modeloReprogramar.Fecha == default)
        {
            citasDelDia = new();
            return;
        }

        // Se traen todas las citas del día; MecanicoOcupado filtra por mecánico y excluye la propia cita.
        citasDelDia = await CitaService.ObtenerPorRangoAsync(modeloReprogramar.Fecha, modeloReprogramar.Fecha);
    }

    private void ToggleMecanicoReprogramar(int mecanicoId, ChangeEventArgs e)
    {
        if (e.Value is bool marcado && marcado)
        {
            selMecanicosIds.Add(mecanicoId);
        }
        else
        {
            selMecanicosIds.Remove(mecanicoId);
        }

        modeloReprogramar.MecanicosIds = selMecanicosIds.ToList();
    }

    // Agrupación de los mecánicos activos por especialidad, para la lista de checkboxes.
    private IEnumerable<IGrouping<int, MecanicoDTO>> MecanicosPorEspecialidad =>
        mecanicos.Where(m => m.Activo)
            .GroupBy(m => m.EspecialidadId)
            .OrderBy(g => g.First().EspecialidadNombre);

    // Especialidad (compartida) de los mecánicos elegidos; null si aún no hay ninguno.
    private int? EspecialidadSeleccionadaId =>
        mecanicos.FirstOrDefault(m => selMecanicosIds.Contains(m.Id))?.EspecialidadId;

    // Solo se combinan mecánicos de la misma especialidad: se habilita el grupo de la
    // especialidad elegida y, por robustez, cualquier grupo que ya tenga seleccionados
    // (así un estado mixto heredado siempre se puede corregir).
    private bool GrupoHabilitado(int especialidadId) =>
        !selMecanicosIds.Any()
        || EspecialidadSeleccionadaId == especialidadId
        || mecanicos.Any(m => m.EspecialidadId == especialidadId && selMecanicosIds.Contains(m.Id));

    private bool MecanicoOcupado(TimeOnly ini, TimeOnly fin)
    {
        if (!selMecanicosIds.Any())
            return false;

        return citasDelDia.Any(c =>
            c.CitaId != cita?.Id
            && c.CitaMecanicos.Any(cm => selMecanicosIds.Contains(cm.MecanicoId))
            && c.Fecha.Date == modeloReprogramar.Fecha.Date
            && c.EstaActiva()
            && c.OverlapsWith(ini, fin));
    }

    private bool HoraHabilitada(TimeOnly h)
    {
        if (modeloReprogramar.Fecha.Date == DateTime.Today && h.ToTimeSpan() <= DateTime.Now.TimeOfDay)
            return false;

        var fin = ServicioService.CalcularHoraFin(h, cita?.ServicioDuracionMinutos ?? PasoMinutos);
        if (fin > Cierre)
            return false;

        return !MecanicoOcupado(h, fin);
    }

    private async Task GuardarReprogramacion()
    {
        if (cita is null)
            return;

        if (!TimeOnly.TryParse(horaTexto, CultureInfo.InvariantCulture, out var hora))
        {
            mensajeError = "Seleccione una hora de inicio.";
            return;
        }

        if (!selMecanicosIds.Any())
        {
            mensajeError = "Seleccione al menos un mecánico.";
            return;
        }

        if (!HoraHabilitada(hora))
        {
            mensajeError = "El horario seleccionado ya no está disponible.";
            return;
        }

        modeloReprogramar.MecanicosIds = selMecanicosIds.ToList();
        modeloReprogramar.HoraInicio = hora;

        guardando = true;
        mensajeError = null;
        try
        {
            var actualizado = await CitaService.ReprogramarCitaAsync(modeloReprogramar);
            if (!actualizado)
            {
                mensajeError = "La cita ya no existe.";
                return;
            }

            cita = await CitaService.ObtenerDetallePorIdAsync(Id);
            mostrarModalReprogramar = false;
            mensajeExito = "Cita reprogramada correctamente.";
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
}
