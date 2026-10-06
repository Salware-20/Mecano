using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.Excepciones;
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

        var horaFin = dto.HoraInicio.AddMinutes(servicio.DuracionMinutos);

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            int vehiculoId = dto.VehiculoId ?? 0;
            
            if (dto.NuevoVehiculo != null && dto.VehiculoId == null)
            {
                var vehiculo = new Vehiculo
                {
                    ClienteId = dto.ClienteId,
                    Placa = dto.NuevoVehiculo.Placa,
                    Marca = dto.NuevoVehiculo.Marca,
                    Modelo = dto.NuevoVehiculo.Modelo,
                    Anio = dto.NuevoVehiculo.Anio
                };
                context.Vehiculo.Add(vehiculo);
                await context.SaveChangesAsync();
                vehiculoId = vehiculo.VehiculoId;
            }

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
            .Select(c => new CitaDTO
            {
                Id = c.CitaId,
                Fecha = c.Fecha,
                HoraInicio = c.HoraInicio,
                HoraFin = c.HoraFin,
                Estado = c.Estado,
                VehiculoPlaca = c.Vehiculo != null ? c.Vehiculo.Placa : null,
                ServicioNombre = c.Servicio != null ? c.Servicio.Nombre : null,
                MecanicoNombre = c.Mecanico != null ? c.Mecanico.Nombre : null
            })
            .ToListAsync();
    }
}
