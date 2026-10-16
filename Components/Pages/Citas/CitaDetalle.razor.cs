using System.Globalization;
using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Mecanico;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Citas;

public partial class CitaDetalle : ComponentBase
{
    // Ventana de atención del taller (los mismos límites que usa el tablero de agendado).
    private static readonly TimeOnly Apertura = new(8, 0);
    private static readonly TimeOnly Cierre = new(18, 0);
    private const int PasoMinutos = 30;

    [Parameter]
    public int Id { get; set; }

    private CitaDetalleDTO? cita;
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
            cita = await CitaService.ObtenerDetallePorIdAsync(Id);
        }
        finally
        {
            cargando = false;
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

        // Servicios de la especialidad del mecánico; el servicio actual se conserva como opción
        // aunque no pertenezca a esa categoría (datos heredados).
        servicios = cita.MecanicoEspecialidadId > 0
            ? await ServicioService.ObtenerPorCategoriaAsync(cita.MecanicoEspecialidadId)
            : new List<ServicioDTO>();

        if (servicios.All(s => s.Id != cita.ServicioId))
        {
            servicios.Insert(0, new ServicioDTO
            {
                Id = cita.ServicioId,
                Nombre = cita.ServicioNombre ?? "Servicio actual",
                DuracionMinutos = cita.ServicioDuracionMinutos,
                Precio = cita.ServicioPrecio,
                CategoriaId = cita.MecanicoEspecialidadId > 0 ? cita.MecanicoEspecialidadId : null,
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
        modeloReprogramar = new ReprogramarCitaDTO
        {
            Id = cita.Id,
            MecanicoId = cita.MecanicoId,
            Fecha = cita.Fecha.Date,
            HoraInicio = cita.HoraInicio
        };
        horaTexto = cita.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture);

        mecanicos = await MecanicoService.ObtenerTodosAsync(FiltroActivo.Activos);
        // Si el mecánico actual está inactivo, se conserva como opción para no forzar un cambio.
        if (mecanicos.All(m => m.Id != cita.MecanicoId))
        {
            mecanicos.Insert(0, new MecanicoDTO
            {
                Id = cita.MecanicoId,
                Nombre = cita.MecanicoNombre ?? "Mecánico actual",
                EspecialidadId = cita.MecanicoEspecialidadId,
                EspecialidadNombre = cita.MecanicoEspecialidadNombre,
                Activo = false
            });
        }

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

    private bool MecanicoOcupado(TimeOnly ini, TimeOnly fin)
    {
        return citasDelDia.Any(c =>
            c.CitaId != cita?.Id
            && c.MecanicoId == modeloReprogramar.MecanicoId
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

        if (!HoraHabilitada(hora))
        {
            mensajeError = "El horario seleccionado ya no está disponible.";
            return;
        }

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
