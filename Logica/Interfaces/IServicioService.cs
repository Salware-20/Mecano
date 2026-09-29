using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;

namespace Mecano.Logica.Interfaces;

public interface IServicioService
{
    Task<List<Servicio>> ObtenerTodosActivosAsync();
    Task<Servicio?> ObtenerPorIdAsync(int servicioId);
    TimeOnly CalcularHoraFin(TimeOnly horaInicio, int duracionMinutos);
}
