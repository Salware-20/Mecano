using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.DTOs;

namespace Mecano.Logica.Interfaces;

public interface ICalendarQueryService
{
    Task<List<CalendarEventDTO>> ObtenerEventosAsync(DateTime inicio, DateTime fin);
}
