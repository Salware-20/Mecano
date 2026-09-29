using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Logica.Interfaces;

namespace Mecano.Logica.Servicios;

public class MecanicoService : IMecanicoService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<MecanicoService> _logger;

    public MecanicoService(IDbContextFactory<MySQLDBContext> factory, ILogger<MecanicoService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Mecanico>> ObtenerPorEspecialidadAsync(int categoriaId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Mecanico
            .Where(m => m.EspecialidadId == categoriaId && m.Activo)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Mecanico>> ObtenerDisponiblesAsync(int categoriaId, DateTime fecha, TimeOnly horaInicio, TimeOnly horaFin)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        var busyMechanicIds = await context.Cita
            .Where(c => c.Fecha.Date == fecha.Date
                && c.Estado != EstadoCita.Cancelada
                && c.Estado != EstadoCita.Finalizada
                && c.HoraInicio < horaFin
                && horaInicio < c.HoraFin)
            .Select(c => c.MecanicoId)
            .Distinct()
            .ToListAsync();

        return await context.Mecanico
            .Where(m => m.EspecialidadId == categoriaId 
                     && m.Activo 
                     && !busyMechanicIds.Contains(m.MecanicoId))
            .AsNoTracking()
            .ToListAsync();
    }
}
