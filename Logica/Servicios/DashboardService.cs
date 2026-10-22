using System;
using System.Linq;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Dashboard;
using Mecano.Entidad.Utilidades;
using Mecano.Logica.Interfaces;

namespace Mecano.Logica.Servicios;

/// <summary>
/// Lectura agregada del día para el panel de control. Compone los servicios ya
/// probados (citas, mecánicos, clientes y vehículos) en un único DTO de salida,
/// agregando solo los conteos que faltaban en sus interfaces.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ICitaService _citaService;
    private readonly IMecanicoService _mecanicoService;
    private readonly IClienteService _clienteService;
    private readonly IVehiculoService _vehiculoService;

    public DashboardService(
        ICitaService citaService,
        IMecanicoService mecanicoService,
        IClienteService clienteService,
        IVehiculoService vehiculoService)
    {
        _citaService = citaService;
        _mecanicoService = mecanicoService;
        _clienteService = clienteService;
        _vehiculoService = vehiculoService;
    }

    public async Task<ResumenDiarioDTO> ObtenerResumenDiarioAsync(DateTime fecha)
    {
        var dia = fecha.Date;

        // Una sola lectura del día: el grafo ya trae cliente, vehículo y mecánicos.
        var citas = await _citaService.ObtenerPorRangoAsync(dia, dia);

        var mecanicosActivos = await _mecanicoService.ContarActivosAsync();
        var clientesActivos = await _clienteService.ContarBusquedaAsync(
            null, FiltroActivo.Activos, incluirIdentificacion: false);
        var vehiculosActivos = await _vehiculoService.ContarActivosAsync();

        // Citas que efectivamente usan taller: las canceladas liberan capacidad.
        var citasEfectivas = citas.Where(c => c.Estado != EstadoCita.Cancelado).ToList();

        var enAgenda = citasEfectivas
            .SelectMany(c => c.CitaMecanicos)
            .Select(cm => cm.MecanicoId)
            .Distinct()
            .Count();

        // Ocupación por minutos-mecánico: una cita con dos mecánicos ocupa a dos
        // personas. Las finalizadas ya usaron su tiempo, así que también cuentan.
        var minutosOcupados = citasEfectivas
            .Sum(c => (int)(c.HoraFin - c.HoraInicio).TotalMinutes * Math.Max(1, c.CitaMecanicos.Count));

        // Los domingos el taller cierra: no hay capacidad que ocupar.
        var cerrado = HorarioAtencion.EsDiaNoLaboral(dia);
        var minutosDisponibles = cerrado ? 0 : mecanicosActivos * HorarioAtencion.MinutosPorDia;

        return new ResumenDiarioDTO
        {
            Fecha = dia,
            CitasHoy = new CitasHoyDTO
            {
                Total = citas.Count,
                Conteos = Enum.GetValues<EstadoCita>()
                    .Select(estado => new EstadoConteoDTO
                    {
                        Estado = estado,
                        Conteo = citas.Count(c => c.Estado == estado)
                    })
                    .ToList(),
                Citas = citas
                    .OrderBy(c => c.HoraInicio)
                    .ThenBy(c => c.CitaId)
                    .Select(c => new CitaResumenDTO
                    {
                        Id = c.CitaId,
                        HoraInicio = c.HoraInicio,
                        HoraFin = c.HoraFin,
                        Estado = c.Estado,
                        ClienteNombre = c.Cliente.NombreCompleto,
                        VehiculoDescripcion = $"{c.Vehiculo.Marca} {c.Vehiculo.Modelo}".Trim(),
                        Placa = c.Vehiculo.Placa,
                        ServicioNombre = c.Servicio.Nombre
                    })
                    .ToList()
            },
            TallerPersonal = new TallerPersonalDTO
            {
                MecanicosActivos = mecanicosActivos,
                MecanicosConCitasHoy = enAgenda,
                ClientesActivos = clientesActivos,
                VehiculosActivos = vehiculosActivos
            },
            Ocupacion = new OcupacionDiariaDTO
            {
                MinutosOcupados = minutosOcupados,
                MinutosDisponibles = minutosDisponibles,
                MecanicosEnAgenda = enAgenda,
                Cerrado = cerrado
            }
        };
    }

    public async Task<ResumenPeriodoDTO> ObtenerResumenPeriodoAsync(DateTime inicio, DateTime fin)
    {
        var desde = inicio.Date;
        var hasta = fin.Date;

        var citas = await _citaService.ObtenerPorRangoAsync(desde, hasta);

        // Canceladas: el donut las muestra (es distribución de estados), pero no
        // consumen carga mecánica, igual que en la ocupación del día.
        var efectivas = citas.Where(c => c.Estado != EstadoCita.Cancelado).ToList();

        // Mecánicos activos: los de 0 citas también se listan (se ve quién está ocioso).
        var mecanicos = await _mecanicoService.ObtenerTodosAsync(FiltroActivo.Activos);

        var carga = mecanicos
            .Select(m =>
            {
                var suyas = efectivas
                    .Where(c => c.CitaMecanicos.Any(cm => cm.MecanicoId == m.Id))
                    .ToList();
                return new CargaMecanicoDTO
                {
                    MecanicoId = m.Id,
                    Nombre = m.Nombre,
                    Citas = suyas.Count,
                    Minutos = suyas.Sum(c => (int)(c.HoraFin - c.HoraInicio).TotalMinutes)
                };
            })
            .OrderByDescending(c => c.Minutos)
            .ThenBy(c => c.Nombre)
            .ToList();

        return new ResumenPeriodoDTO
        {
            Inicio = desde,
            Fin = hasta,
            TotalCitas = citas.Count,
            Conteos = Enum.GetValues<EstadoCita>()
                .Select(estado => new EstadoConteoDTO
                {
                    Estado = estado,
                    Conteo = citas.Count(c => c.Estado == estado)
                })
                .ToList(),
            CargaMecanicos = carga
        };
    }

    public async Task<ResumenMecanicoDTO> ObtenerResumenMecanicoAsync(int mecanicoId)
    {
        var hoy = DateTime.Today;

        // Semana laborable en curso: lunes a sábado (los domingos el taller cierra).
        var lunes = hoy.AddDays(hoy.DayOfWeek == DayOfWeek.Sunday ? -6 : -(int)hoy.DayOfWeek + 1);
        var sabado = lunes.AddDays(5);

        // Una sola lectura de la semana; el filtro por mecánico se aplica en memoria
        // sobre el grafo que ya trae cliente, vehículo y mecánicos.
        var citasSemana = (await _citaService.ObtenerPorRangoAsync(lunes, sabado))
            .Where(c => c.CitaMecanicos.Any(cm => cm.MecanicoId == mecanicoId))
            .ToList();

        var suyasHoy = citasSemana.Where(c => c.Fecha.Date == hoy).ToList();

        // El mecánico no ve su propia ocupación contada por terceros: los minutos son
        // solo su tiempo, sin multiplicar por cuántos colegas compartan la cita.
        var minutosOcupados = suyasHoy
            .Where(c => c.Estado != EstadoCita.Cancelado)
            .Sum(c => (int)(c.HoraFin - c.HoraInicio).TotalMinutes);

        var cerrado = HorarioAtencion.EsDiaNoLaboral(hoy);

        var mecanico = await _mecanicoService.ObtenerDetallePorIdAsync(mecanicoId);

        return new ResumenMecanicoDTO
        {
            MecanicoId = mecanicoId,
            MecanicoNombre = mecanico?.Nombre,
            EspecialidadNombre = mecanico?.EspecialidadNombre,
            Fecha = hoy,
            Cerrado = cerrado,
            CitasHoyTotal = suyasHoy.Count,
            CitasHoyPorEstado = Enum.GetValues<EstadoCita>()
                .Select(estado => new EstadoConteoDTO
                {
                    Estado = estado,
                    Conteo = suyasHoy.Count(c => c.Estado == estado)
                })
                .ToList(),
            MinutosOcupados = minutosOcupados,
            MinutosDisponibles = cerrado ? 0 : HorarioAtencion.MinutosPorDia,
            Semana = Enumerable.Range(0, 6) // lunes..sábado
                .Select(i => lunes.AddDays(i))
                .Select(dia =>
                {
                    var delDia = citasSemana.Where(c => c.Fecha.Date == dia).ToList();
                    return new CargaDiaMecanicoDTO
                    {
                        Fecha = dia,
                        TotalCitas = delDia.Count,
                        MinutosOcupados = delDia
                            .Where(c => c.Estado != EstadoCita.Cancelado)
                            .Sum(c => (int)(c.HoraFin - c.HoraInicio).TotalMinutes),
                        Cerrado = HorarioAtencion.EsDiaNoLaboral(dia)
                    };
                })
                .ToList()
        };
    }
}
