using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.DTOs.Mecanico;
using Mecano.Entidad.Excepciones;
using Mecano.Entidad.Utilidades;
using Mecano.Logica.Interfaces;

namespace Mecano.Logica.Servicios;

public class CitaService : ICitaService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<CitaService> _logger;

    public CitaService(IDbContextFactory<MySQLDBContext> factory, ILogger<CitaService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<Cita> AgendarCitaAsync(AgendarCitaDTO dto)
    {
        using var context = await _factory.CreateDbContextAsync();

        var mecanicosIds = dto.MecanicosIds?.Distinct().ToList() ?? new List<int>();
        if (mecanicosIds.Count == 0)
        {
            throw new InvalidOperationException("Seleccione al menos un mecánico.");
        }

        var mecanicos = await context.Mecanico
            .Where(m => mecanicosIds.Contains(m.MecanicoId))
            .ToDictionaryAsync(m => m.MecanicoId);

        // Todos los mecánicos asignados deben existir y estar activos (defensa en
        // profundidad: la UI ya solo ofrece activos).
        foreach (var id in mecanicosIds)
        {
            if (!mecanicos.TryGetValue(id, out var mec) || !mec.Activo)
            {
                throw new InactiveMechanicException($"Mecánico con ID {id} no existe o está inactivo.");
            }
        }

        // Una cita solo admite mecánicos de una misma especialidad (defensa en profundidad:
        // la UI agrupa por especialidad, pero el servicio es la última barrera ante
        // llamadas directas o datos manipulados).
        if (mecanicos.Values.Select(m => m.EspecialidadId).Distinct().Count() > 1)
        {
            throw new InvalidOperationException("Todos los mecánicos de una cita deben pertenecer a la misma especialidad.");
        }

        var servicio = await context.Servicio.FindAsync(dto.ServicioId);
        if (servicio == null)
        {
            throw new InvalidOperationException($"Servicio con ID {dto.ServicioId} no existe.");
        }

        // Un servicio desactivado no se puede agendar (defensa en profundidad: la UI
        // solo ofrece servicios activos, pero la desactivación puede ocurrir en paralelo).
        if (!servicio.Activo)
        {
            throw new InactiveServiceException("El servicio seleccionado está desactivado. Elige un servicio activo para agendar la cita.");
        }

        // Reglas de agenda (defensa en profundidad: la UI ya las restringe, pero el
        // servicio es la última barrera ante llamadas directas o datos manipulados).
        if (dto.Fecha.Date < DateTime.Today)
        {
            throw new InvalidOperationException("No se pueden agendar citas en fechas pasadas.");
        }

        if (HorarioAtencion.EsDiaNoLaboral(dto.Fecha))
        {
            throw new InvalidOperationException("El taller no atiende los domingos.");
        }

        var apertura = HorarioAtencion.Apertura;
        var cierre = HorarioAtencion.Cierre;

        if (dto.HoraInicio < apertura || dto.HoraInicio >= cierre)
        {
            throw new InvalidOperationException("El horario de atención es de 8:00 a. m. a 6:00 p. m.");
        }

        if (dto.Fecha.Date == DateTime.Today && dto.HoraInicio <= TimeOnly.FromDateTime(DateTime.Now))
        {
            throw new InvalidOperationException("No se pueden agendar citas en horarios que ya pasaron.");
        }

        var horaFin = dto.HoraInicio.AddMinutes(servicio.DuracionMinutos);

        if (horaFin > cierre)
        {
            throw new InvalidOperationException("La cita terminaría después del horario de atención (6:00 p. m.).");
        }

        // El vehículo ya no puede crearse al vuelo al agendar: debe existir y pertenecer
        // al cliente (la UI lo exige). Se valida aquí como defensa en profundidad.
        if (dto.VehiculoId is null)
        {
            throw new InvalidOperationException("Seleccione un vehículo para la cita.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            int vehiculoId = dto.VehiculoId.Value;

            // La cita ocupa el tramo para TODOS los mecánicos asignados: si alguno
            // está ocupado, se rechaza la cita completa indicando cuál conflicto.
            foreach (var id in mecanicosIds)
            {
                bool overlapExists = await context.Cita
                    .AnyAsync(c => c.CitaMecanicos.Any(cm => cm.MecanicoId == id)
                                && c.Fecha.Date == dto.Fecha.Date
                                && c.Estado != EstadoCita.Cancelado
                                && c.Estado != EstadoCita.Finalizado
                                && c.HoraInicio < horaFin
                                && dto.HoraInicio < c.HoraFin);

                if (overlapExists)
                {
                    throw new AppointmentOverlapException(
                        $"El mecánico {mecanicos[id].Nombre} ya tiene una cita asignada en ese horario.");
                }
            }

            var cita = new Cita
            {
                ClienteId = dto.ClienteId,
                VehiculoId = vehiculoId,
                ServicioId = dto.ServicioId,
                Fecha = dto.Fecha,
                HoraInicio = dto.HoraInicio,
                HoraFin = horaFin,
                Estado = EstadoCita.Pendiente,
                FechaCreacion = DateTime.Now,
                CitaMecanicos = mecanicosIds
                    .Select(id => new CitaMecanico { MecanicoId = id })
                    .ToList()
            };

            context.Cita.Add(cita);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return await context.Cita
                .Include(c => c.Cliente)
                .Include(c => c.Vehiculo)
                .Include(c => c.Servicio)
                .Include(c => c.CitaMecanicos)
                .FirstAsync(c => c.CitaId == cita.CitaId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error agendando la cita.");
            throw;
        }
    }

    public async Task CancelarCitaAsync(int citaId)
    {
        using var context = await _factory.CreateDbContextAsync();
        var cita = await context.Cita.FindAsync(citaId);
        
        if (cita == null)
        {
            throw new InvalidOperationException($"La cita con ID {citaId} no existe.");
        }

        // Un trabajo finalizado no se puede deshacer cancelándolo desde el listado.
        if (cita.Estado == EstadoCita.Finalizado)
        {
            throw new InvalidOperationException("Una cita finalizada no se puede cancelar.");
        }

        cita.Estado = EstadoCita.Cancelado;
        await context.SaveChangesAsync();
    }

    public async Task<List<Cita>> ObtenerPorRangoAsync(DateTime inicio, DateTime fin)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Cita
            .Include(c => c.Cliente)
            .Include(c => c.Vehiculo)
            .Include(c => c.Servicio)
            .Include(c => c.CitaMecanicos)
                .ThenInclude(cm => cm.Mecanico)
            .Where(c => c.Fecha >= inicio.Date && c.Fecha <= fin.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    // ~~~ READ (proyección compartida) ~~~
    // Misma proyección para el listado paginado y el panel de citas del cliente:
    // evita materializar el grafo y mantiene un único contrato de lectura.
    private static readonly Expression<Func<Cita, CitaDTO>> ProyeccionListado = c => new CitaDTO
    {
        Id = c.CitaId,
        Fecha = c.Fecha,
        HoraInicio = c.HoraInicio,
        HoraFin = c.HoraFin,
        Estado = c.Estado,
        ClienteId = c.ClienteId,
        ClienteNombre = c.Cliente != null ? c.Cliente.NombreCompleto : null,
        ClienteActivo = c.Cliente != null && c.Cliente.Activo,
        VehiculoId = c.VehiculoId,
        VehiculoPlaca = c.Vehiculo != null ? c.Vehiculo.Placa : null,
        ServicioId = c.ServicioId,
        ServicioNombre = c.Servicio != null ? c.Servicio.Nombre : null,
        ServicioActivo = c.Servicio != null && c.Servicio.Activo,
        Mecanicos = c.CitaMecanicos
            .Select(cm => new MecanicoDTO
            {
                Id                 = cm.MecanicoId,
                Nombre             = cm.Mecanico.Nombre,
                Email              = cm.Mecanico.Email,
                Cedula             = cm.Mecanico.Cedula,
                TipoIdentificacion = cm.Mecanico.TipoIdentificacion,
                Telefono           = cm.Mecanico.Telefono,
                EspecialidadId     = cm.Mecanico.EspecialidadId,
                EspecialidadNombre = cm.Mecanico.Especialidad != null ? cm.Mecanico.Especialidad.Nombre : null,
                Activo             = cm.Mecanico.Activo
            })
            .ToList()
    };

    // ~~~ READ (por cliente) ~~~
    public async Task<List<CitaDTO>> ObtenerPorClienteAsync(int clienteId)
    {
        using var context = await _factory.CreateDbContextAsync();

        // Proyección directa: no usamos Include para no materializar el grafo completo.
        return await context.Cita
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.HoraInicio)
            .Select(ProyeccionListado)
            .ToListAsync();
    }

    public async Task<List<CitaDTO>> ObtenerPorVehiculoAsync(int vehiculoId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Cita
            .AsNoTracking()
            .Where(c => c.VehiculoId == vehiculoId)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.HoraInicio)
            .Select(ProyeccionListado)
            .ToListAsync();
    }

    // ~~~ READ (listado paginado) ~~~
    public async Task<List<CitaDTO>> BuscarPaginadoAsync(FiltroCitasDTO filtro, int pagina, int tamanioPagina)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await AplicarFiltro(context.Cita.AsNoTracking(), filtro)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.HoraInicio)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .Select(ProyeccionListado)
            .ToListAsync();
    }

    public async Task<int> ContarAsync(FiltroCitasDTO filtro)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await AplicarFiltro(context.Cita.AsNoTracking(), filtro).CountAsync();
    }

    // ~~~ READ (detalle) ~~~
    public async Task<CitaDetalleDTO?> ObtenerDetallePorIdAsync(int citaId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Cita
            .AsNoTracking()
            .Where(c => c.CitaId == citaId)
            .Select(c => new CitaDetalleDTO
            {
                Id = c.CitaId,
                Fecha = c.Fecha,
                HoraInicio = c.HoraInicio,
                HoraFin = c.HoraFin,
                Estado = c.Estado,
                FechaCreacion = c.FechaCreacion,
                NotasAdmin = c.NotasAdmin,

                ClienteId = c.ClienteId,
                ClienteNombre = c.Cliente != null ? c.Cliente.NombreCompleto : null,
                ClienteActivo = c.Cliente != null && c.Cliente.Activo,
                ClienteCedula = c.Cliente != null ? c.Cliente.CedulaIdentidad : null,
                ClienteTipoIdentificacion = c.Cliente != null ? c.Cliente.TipoIdentificacion : default,
                ClienteTelefono = c.Cliente != null ? c.Cliente.Telefono : null,
                ClienteCorreo = c.Cliente != null ? c.Cliente.Correo : null,

                VehiculoId = c.VehiculoId,
                VehiculoPlaca = c.Vehiculo != null ? c.Vehiculo.Placa : null,
                VehiculoMarca = c.Vehiculo != null ? c.Vehiculo.Marca : null,
                VehiculoModelo = c.Vehiculo != null ? c.Vehiculo.Modelo : null,
                VehiculoAnio = c.Vehiculo != null ? c.Vehiculo.Anio : 0,

                ServicioId = c.ServicioId,
                ServicioNombre = c.Servicio != null ? c.Servicio.Nombre : null,
                ServicioDuracionMinutos = c.Servicio != null ? c.Servicio.DuracionMinutos : 0,
                ServicioPrecio = c.Servicio != null ? c.Servicio.Precio : 0m,
                ServicioActivo = c.Servicio != null && c.Servicio.Activo,

                Mecanicos = c.CitaMecanicos
                    .Select(cm => new MecanicoDTO
                    {
                        Id                 = cm.MecanicoId,
                        Nombre             = cm.Mecanico.Nombre,
                        Email              = cm.Mecanico.Email,
                        Cedula             = cm.Mecanico.Cedula,
                        TipoIdentificacion = cm.Mecanico.TipoIdentificacion,
                        Telefono           = cm.Mecanico.Telefono,
                        EspecialidadId     = cm.Mecanico.EspecialidadId,
                        EspecialidadNombre = cm.Mecanico.Especialidad != null ? cm.Mecanico.Especialidad.Nombre : null,
                        Activo             = cm.Mecanico.Activo
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    // ~~~ UPDATE (datos no de agenda: vehículo, servicio y notas) ~~~
    // La fecha, la hora y el mecánico no se tocan aquí: eso es la reprogramación (fase posterior).
    public async Task<bool> ActualizarCitaAsync(EditarCitaDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var cita = await context.Cita
            .Include(c => c.CitaMecanicos)
            .FirstOrDefaultAsync(c => c.CitaId == dto.Id);
        if (cita is null)
            return false;

        if (!cita.EstaActiva())
            throw new InvalidOperationException("No se puede editar una cita cancelada o finalizada.");

        var servicio = await context.Servicio.FindAsync(dto.ServicioId);
        if (servicio is null)
            throw new InvalidOperationException($"Servicio con ID {dto.ServicioId} no existe.");

        // Se permite conservar el servicio/vehículo actual aunque esté inactivo, pero no
        // cambiar a uno inactivo (defensa en profundidad: la UI ya ofrece solo activos).
        if (!servicio.Activo && servicio.ServicioId != cita.ServicioId)
            throw new InvalidOperationException("El servicio seleccionado no está activo.");

        var vehiculo = await context.Vehiculo.FindAsync(dto.VehiculoId);
        if (vehiculo is null || vehiculo.ClienteId != cita.ClienteId)
            throw new InvalidOperationException("El vehículo seleccionado no pertenece al cliente de la cita.");

        if (!vehiculo.Activo && vehiculo.VehiculoId != cita.VehiculoId)
            throw new InvalidOperationException("El vehículo seleccionado no está activo.");

        // Cambiar el servicio cambia la duración: se recalcula el fin y se revalida la agenda.
        // La cita ocupa el tramo para todos sus mecánicos: basta con que uno esté ocupado.
        var mecanicosIds = cita.CitaMecanicos.Select(cm => cm.MecanicoId).ToList();
        var fecha = cita.Fecha;
        var horaInicio = cita.HoraInicio;
        var horaFin = horaInicio.AddMinutes(servicio.DuracionMinutos);
        var cierre = HorarioAtencion.Cierre;
        if (horaFin > cierre)
            throw new InvalidOperationException("La cita terminaría después del horario de atención (6:00 p. m.).");

        bool overlapExists = await context.Cita.AnyAsync(c =>
            c.CitaId != dto.Id
            && c.CitaMecanicos.Any(cm => mecanicosIds.Contains(cm.MecanicoId))
            && c.Fecha.Date == fecha.Date
            && c.Estado != EstadoCita.Cancelado
            && c.Estado != EstadoCita.Finalizado
            && c.HoraInicio < horaFin
            && horaInicio < c.HoraFin);

        if (overlapExists)
            throw new AppointmentOverlapException("El mecánico ya tiene otra cita que se solapa con el nuevo horario.");

        cita.VehiculoId = dto.VehiculoId;
        cita.ServicioId = dto.ServicioId;
        cita.HoraFin = horaFin;
        cita.NotasAdmin = string.IsNullOrWhiteSpace(dto.NotasAdmin) ? null : dto.NotasAdmin.Trim();

        await context.SaveChangesAsync();
        _logger.LogInformation("Cita {Id} actualizada (vehículo {VehiculoId}, servicio {ServicioId}).",
            cita.CitaId, dto.VehiculoId, dto.ServicioId);
        return true;
    }

    // ~~~ UPDATE (reprogramación: fecha, hora y mecánico) ~~~
    // El vehículo, el servicio y las notas no se tocan aquí: eso es la edición (fase anterior).
    public async Task<bool> ReprogramarCitaAsync(ReprogramarCitaDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var cita = await context.Cita
            .Include(c => c.CitaMecanicos)
            .FirstOrDefaultAsync(c => c.CitaId == dto.Id);
        if (cita is null)
            return false;

        if (!cita.EstaActiva())
            throw new InvalidOperationException("No se puede reprogramar una cita cancelada o finalizada.");

        var mecanicosIds = dto.MecanicosIds?.Distinct().ToList() ?? new List<int>();
        if (mecanicosIds.Count == 0)
            throw new InvalidOperationException("Seleccione al menos un mecánico.");

        var mecanicos = await context.Mecanico
            .Where(m => mecanicosIds.Contains(m.MecanicoId))
            .ToDictionaryAsync(m => m.MecanicoId);

        foreach (var id in mecanicosIds)
        {
            if (!mecanicos.TryGetValue(id, out var mec) || !mec.Activo)
                throw new InactiveMechanicException($"Mecánico con ID {id} no existe o está inactivo.");
        }

        // La reprogramación también debe respetar la especialidad compartida.
        if (mecanicos.Values.Select(m => m.EspecialidadId).Distinct().Count() > 1)
            throw new InvalidOperationException("Todos los mecánicos de una cita deben pertenecer a la misma especialidad.");

        var servicio = await context.Servicio.FindAsync(cita.ServicioId);
        if (servicio is null)
            throw new InvalidOperationException($"Servicio con ID {cita.ServicioId} no existe.");

        if (dto.Fecha.Date < DateTime.Today)
            throw new InvalidOperationException("No se pueden reprogramar citas a fechas pasadas.");

        if (dto.Fecha.DayOfWeek == DayOfWeek.Sunday)
            throw new InvalidOperationException("El taller no atiende los domingos.");

        var apertura = new TimeOnly(8, 0);
        var cierre = new TimeOnly(18, 0);

        if (dto.HoraInicio < apertura || dto.HoraInicio >= cierre)
            throw new InvalidOperationException("El horario de atención es de 8:00 a. m. a 6:00 p. m.");

        if (dto.Fecha.Date == DateTime.Today && dto.HoraInicio <= TimeOnly.FromDateTime(DateTime.Now))
            throw new InvalidOperationException("No se pueden reprogramar citas a horarios que ya pasaron.");

        var horaFin = dto.HoraInicio.AddMinutes(servicio.DuracionMinutos);
        if (horaFin > cierre)
            throw new InvalidOperationException("La cita terminaría después del horario de atención (6:00 p. m.).");

        bool hayConflicto = false;
        string nomConflicto = string.Empty;
        foreach (var id in mecanicosIds)
        {
            bool overlapExists = await context.Cita.AnyAsync(c =>
                c.CitaId != dto.Id
                && c.CitaMecanicos.Any(cm => cm.MecanicoId == id)
                && c.Fecha.Date == dto.Fecha.Date
                && c.Estado != EstadoCita.Cancelado
                && c.Estado != EstadoCita.Finalizado
                && c.HoraInicio < horaFin
                && dto.HoraInicio < c.HoraFin);

            if (overlapExists)
            {
                hayConflicto = true;
                nomConflicto = mecanicos[id].Nombre;
                break;
            }
        }

        if (hayConflicto)
            throw new AppointmentOverlapException($"El mecánico {nomConflicto} ya tiene una cita asignada en ese horario.");

        // La reprogramación reemplaza el conjunto de mecánicos asignados.
        cita.CitaMecanicos.Clear();
        foreach (var id in mecanicosIds)
        {
            cita.CitaMecanicos.Add(new CitaMecanico { CitaId = cita.CitaId, MecanicoId = id });
        }

        cita.Fecha = dto.Fecha;
        cita.HoraInicio = dto.HoraInicio;
        cita.HoraFin = horaFin;

        await context.SaveChangesAsync();
        _logger.LogInformation("Cita {Id} reprogramada a {Fecha} {Hora} con mecánicos {MecanicosIds}.",
            cita.CitaId, dto.Fecha, dto.HoraInicio, string.Join(", ", mecanicosIds));
        return true;
    }

    // ~~~ UPDATE (transición de estado) ~~~
    // El mecánico solo puede iniciar (Confirmado → EnProceso) y finalizar
    // (EnProceso → Finalizado), y solo si está asignado; confirmar y cancelar son
    // exclusivos de administración (mecanicoId == null). Los estados terminales
    // (Finalizado y Cancelado) no se tocan.
    public async Task<bool> TransicionarEstadoAsync(int citaId, EstadoCita destino, int? mecanicoId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var cita = await context.Cita
            .Include(c => c.CitaMecanicos)
            .FirstOrDefaultAsync(c => c.CitaId == citaId);

        if (cita == null) return false;

        if (cita.Estado == EstadoCita.Finalizado || cita.Estado == EstadoCita.Cancelado)
        {
            throw new InvalidOperationException(
                $"La cita ya está {TextoEstado(cita.Estado)}: su estado no se puede modificar.");
        }

        var esAdmin = mecanicoId == null;

        if (!esAdmin)
        {
            if (destino != EstadoCita.EnProceso && destino != EstadoCita.Finalizado)
            {
                throw new InvalidOperationException(
                    "El mecánico solo puede iniciar o finalizar una cita; confirmar y cancelar son funciones de administración.");
            }

            if (!cita.CitaMecanicos.Any(cm => cm.MecanicoId == mecanicoId))
            {
                throw new InvalidOperationException(
                    "Solo el mecánico asignado a la cita puede cambiar su estado.");
            }
        }

        var valida = (cita.Estado, destino) switch
        {
            (EstadoCita.Pendiente, EstadoCita.Confirmado) => esAdmin,
            (EstadoCita.Pendiente, EstadoCita.Cancelado) => esAdmin,
            (EstadoCita.Confirmado, EstadoCita.EnProceso) => true,
            (EstadoCita.Confirmado, EstadoCita.Cancelado) => esAdmin,
            (EstadoCita.EnProceso, EstadoCita.Finalizado) => true,
            (EstadoCita.EnProceso, EstadoCita.Cancelado) => esAdmin,
            _ => false
        };

        if (!valida)
        {
            throw new InvalidOperationException(
                $"No se puede pasar una cita de {TextoEstado(cita.Estado)} a {TextoEstado(destino)}.");
        }

        var origen = cita.Estado;
        cita.Estado = destino;
        await context.SaveChangesAsync();

        _logger.LogInformation("Cita {Id}: {Origen} → {Destino} ({Quien}).",
            cita.CitaId, TextoEstado(origen), TextoEstado(destino),
            esAdmin ? "administración" : $"mecánico {mecanicoId}");
        return true;
    }

    private static string TextoEstado(EstadoCita estado) => estado switch
    {
        EstadoCita.Pendiente => "Pendiente",
        EstadoCita.Confirmado => "Confirmada",
        EstadoCita.EnProceso => "En proceso",
        EstadoCita.Finalizado => "Finalizada",
        EstadoCita.Cancelado => "Cancelada",
        _ => estado.ToString()
    };

    // ~~~ VISTA DEL MECÁNICO (proyección restringida) ~~~
    // Igual que ProyeccionListado pero sin el PII de los colegas (solo nombre) y sin
    // precio: el listado del mecánico no es un listado de administración.
    private static readonly Expression<Func<Cita, CitaListaMecanicoDTO>> ProyeccionListaMecanico = c => new CitaListaMecanicoDTO
    {
        Id = c.CitaId,
        Fecha = c.Fecha,
        HoraInicio = c.HoraInicio,
        HoraFin = c.HoraFin,
        Estado = c.Estado,
        ClienteId = c.ClienteId,
        ClienteNombre = c.Cliente != null ? c.Cliente.NombreCompleto : null,
        ClienteActivo = c.Cliente != null && c.Cliente.Activo,
        VehiculoId = c.VehiculoId,
        VehiculoPlaca = c.Vehiculo != null ? c.Vehiculo.Placa : null,
        VehiculoMarca = c.Vehiculo != null ? c.Vehiculo.Marca : null,
        VehiculoModelo = c.Vehiculo != null ? c.Vehiculo.Modelo : null,
        VehiculoAnio = c.Vehiculo != null ? c.Vehiculo.Anio : 0,
        ServicioId = c.ServicioId,
        ServicioNombre = c.Servicio != null ? c.Servicio.Nombre : null,
        ServicioDuracionMinutos = c.Servicio != null ? c.Servicio.DuracionMinutos : 0,
        ServicioActivo = c.Servicio != null && c.Servicio.Activo,
        MecanicosNombres = c.CitaMecanicos
            .Select(cm => cm.Mecanico.Nombre)
            .ToList()
    };

    public async Task<List<CitaListaMecanicoDTO>> BuscarParaMecanicoAsync(
        int mecanicoId, FiltroCitasDTO filtro, int pagina, int tamanioPagina)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // El alcance es innegociable: el filtro de mecánico siempre es el del claim,
        // pase lo que pase en el resto del DTO que mande la UI.
        filtro.MecanicoId = mecanicoId;

        return await AplicarFiltro(context.Cita.AsNoTracking(), filtro)
            // Agenda del mecánico: cronológico ascendente (lo próximo primero).
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.HoraInicio)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .Select(ProyeccionListaMecanico)
            .ToListAsync();
    }

    public async Task<int> ContarParaMecanicoAsync(int mecanicoId, FiltroCitasDTO filtro)
    {
        await using var context = await _factory.CreateDbContextAsync();

        filtro.MecanicoId = mecanicoId;
        return await AplicarFiltro(context.Cita.AsNoTracking(), filtro).CountAsync();
    }

    public async Task<CitaDetalleMecanicoDTO?> ObtenerDetalleParaMecanicoAsync(int citaId, int mecanicoId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Sin precio ni NotasAdmin, y del cliente solo el nombre: datos de trabajo.
        // Además, solo citas asignadas al mecánico: teclear una URL no abre citas ajenas.
        return await context.Cita
            .AsNoTracking()
            .Where(c => c.CitaId == citaId
                && c.CitaMecanicos.Any(cm => cm.MecanicoId == mecanicoId))
            .Select(c => new CitaDetalleMecanicoDTO
            {
                Id = c.CitaId,
                Fecha = c.Fecha,
                HoraInicio = c.HoraInicio,
                HoraFin = c.HoraFin,
                Estado = c.Estado,
                FechaCreacion = c.FechaCreacion,
                ClienteId = c.ClienteId,
                ClienteNombre = c.Cliente != null ? c.Cliente.NombreCompleto : null,
                ClienteActivo = c.Cliente != null && c.Cliente.Activo,
                VehiculoId = c.VehiculoId,
                VehiculoPlaca = c.Vehiculo != null ? c.Vehiculo.Placa : null,
                VehiculoMarca = c.Vehiculo != null ? c.Vehiculo.Marca : null,
                VehiculoModelo = c.Vehiculo != null ? c.Vehiculo.Modelo : null,
                VehiculoAnio = c.Vehiculo != null ? c.Vehiculo.Anio : 0,
                VehiculoKilometraje = c.Vehiculo != null ? c.Vehiculo.Kilometraje : null,
                VehiculoNotas = c.Vehiculo != null ? c.Vehiculo.Notas : null,
                ServicioId = c.ServicioId,
                ServicioNombre = c.Servicio != null ? c.Servicio.Nombre : null,
                ServicioDuracionMinutos = c.Servicio != null ? c.Servicio.DuracionMinutos : 0,
                ServicioActivo = c.Servicio != null && c.Servicio.Activo,
                MecanicosNombres = c.CitaMecanicos
                    .Select(cm => cm.Mecanico.Nombre)
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    // ~~~ FILTRO COMPARTIDO (listado + conteo) ~~~
    // BuscarPaginadoAsync y ContarAsync comparten este filtro para que el total de la
    // paginación coincida exactamente con el conjunto paginado.
    private static IQueryable<Cita> AplicarFiltro(IQueryable<Cita> query, FiltroCitasDTO filtro)
    {
        if (filtro.Desde is DateTime desde)
            query = query.Where(c => c.Fecha >= desde.Date);

        if (filtro.Hasta is DateTime hasta)
            query = query.Where(c => c.Fecha <= hasta.Date);

        if (filtro.MecanicoId is int mecanicoId)
            query = query.Where(c => c.CitaMecanicos.Any(cm => cm.MecanicoId == mecanicoId));

        if (filtro.Estado is EstadoCita estado)
            query = query.Where(c => c.Estado == estado);

        if (string.IsNullOrWhiteSpace(filtro.Termino))
            return query;

        var termino = filtro.Termino.Trim();
        // Misma doble comparación que ClienteService: la placa se guarda normalizada
        // (ABC123) pero se busca formateada (ABC-123), así que se prueba tal cual y normalizada.
        var placa = Formatos.NormalizarPlaca(termino);

        return query.Where(c =>
            EF.Functions.Like(c.Cliente.NombreCompleto, $"%{termino}%")
            || (c.Vehiculo != null && EF.Functions.Like(c.Vehiculo.Placa, $"%{termino}%"))
            || (c.Vehiculo != null && placa.Length > 0 && EF.Functions.Like(c.Vehiculo.Placa, $"%{placa}%")));
    }
}
