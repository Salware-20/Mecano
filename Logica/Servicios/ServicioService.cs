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

public class ServicioService : IServicioService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<ServicioService> _logger;

    public ServicioService(IDbContextFactory<MySQLDBContext> factory, ILogger<ServicioService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Servicio>> ObtenerTodosActivosAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Servicio
            .Include(s => s.Categoria)
            .Where(s => s.Activo)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Servicio?> ObtenerPorIdAsync(int servicioId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Servicio
            .Include(s => s.Categoria)
            .FirstOrDefaultAsync(s => s.ServicioId == servicioId);
    }

    public TimeOnly CalcularHoraFin(TimeOnly horaInicio, int duracionMinutos)
    {
        return horaInicio.AddMinutes(duracionMinutos);
    }
}
