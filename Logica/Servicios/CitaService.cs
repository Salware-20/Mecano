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
        
        var mecanico = await context.Mecanico.FindAsync(dto.MecanicoId);
        if (mecanico == null || !mecanico.Activo)
        {
            throw new InactiveMechanicException($"Mecánico con ID {dto.MecanicoId} no existe o está inactivo.");
        }

        var servicio = await context.Servicio.FindAsync(dto.ServicioId);
        if (servicio == null)
        {
            throw new InvalidOperationException($"Servicio con ID {dto.ServicioId} no existe.");
        }

        // Reglas de agenda (defensa en profundidad: la UI ya las restringe, pero el
        // servicio es la última barrera ante llamadas directas o datos manipulados).
        if (dto.Fecha.Date < DateTime.Today)
        {
            throw new InvalidOperationException("No se pueden agendar citas en fechas pasadas.");
        }

        if (dto.Fecha.DayOfWeek == DayOfWeek.Sunday)
        {
            throw new InvalidOperationException("El taller no atiende los domingos.");
        }

        var apertura = new TimeOnly(8, 0);
        var cierre = new TimeOnly(18, 0);

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

            bool overlapExists = await context.Cita
                .AnyAsync(c => c.MecanicoId == dto.MecanicoId
                            && c.Fecha.Date == dto.Fecha.Date
                            && c.Estado != EstadoCita.Cancelada
                            && c.Estado != EstadoCita.Finalizada
                            && c.HoraInicio < horaFin
                            && dto.HoraInicio < c.HoraFin);

            if (overlapExists)
            {
                throw new AppointmentOverlapException("El mecánico ya tiene una cita asignada en ese horario.");
            }

            var cita = new Cita
            {
                ClienteId = dto.ClienteId,
                VehiculoId = vehiculoId,
                ServicioId = dto.ServicioId,
                MecanicoId = dto.MecanicoId,
                Fecha = dto.Fecha,
                HoraInicio = dto.HoraInicio,
                HoraFin = horaFin,
                Estado = EstadoCita.Pendiente,
                FechaCreacion = DateTime.Now
            };

            context.Cita.Add(cita);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return await context.Cita
                .Include(c => c.Cliente)
                .Include(c => c.Vehiculo)
                .Include(c => c.Servicio)
                .Include(c => c.Mecanico)
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

        cita.Estado = EstadoCita.Cancelada;
        await context.SaveChangesAsync();
    }

    public async Task<List<Cita>> ObtenerPorRangoAsync(DateTime inicio, DateTime fin)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Cita
            .Include(c => c.Cliente)
            .Include(c => c.Vehiculo)
            .Include(c => c.Servicio)
            .Include(c => c.Mecanico)
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
        VehiculoId = c.VehiculoId,
        VehiculoPlaca = c.Vehiculo != null ? c.Vehiculo.Placa : null,
        ServicioId = c.ServicioId,
        ServicioNombre = c.Servicio != null ? c.Servicio.Nombre : null,
        MecanicoId = c.MecanicoId,
        MecanicoNombre = c.Mecanico != null ? c.Mecanico.Nombre : null
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

                MecanicoId = c.MecanicoId,
                MecanicoNombre = c.Mecanico != null ? c.Mecanico.Nombre : null,
                MecanicoEspecialidadNombre = c.Mecanico != null && c.Mecanico.Especialidad != null
                    ? c.Mecanico.Especialidad.Nombre
                    : null
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
            query = query.Where(c => c.MecanicoId == mecanicoId);

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
