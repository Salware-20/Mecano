using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Logica.Interfaces;

namespace Mecano.Logica.Servicios;

public class CalendarQueryService : ICalendarQueryService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<CalendarQueryService> _logger;

    public CalendarQueryService(IDbContextFactory<MySQLDBContext> factory, ILogger<CalendarQueryService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<CalendarEventDTO>> ObtenerEventosAsync(DateTime inicio, DateTime fin)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        var citas = await context.Cita
            .Include(c => c.Cliente)
            .Include(c => c.Servicio)
            .Where(c => c.Fecha >= inicio.Date 
                     && c.Fecha <= fin.Date 
                     && c.Estado != EstadoCita.Cancelada)
            .AsNoTracking()
            .ToListAsync();

        return citas.Select(c => new CalendarEventDTO
        {
            Id = c.CitaId,
            Title = $"{c.Servicio.Nombre} - {c.Cliente.NombreCompleto}",
            Start = c.Fecha.Date + c.HoraInicio.ToTimeSpan(),
            End = c.Fecha.Date + c.HoraFin.ToTimeSpan(),
            Color = ObtenerColor(c.Estado),
            AllDay = false
        }).ToList();
    }

    private string ObtenerColor(EstadoCita estado)
    {
        return estado switch
        {
            EstadoCita.Pendiente => "#ffc107",
            EstadoCita.Confirmada => "#28a745",
            EstadoCita.EnProceso => "#17a2b8",
            EstadoCita.Finalizada => "#6c757d",
            _ => "#dc3545" // Include Cancelada as fallback even if it's filtered
        };
    }
}
