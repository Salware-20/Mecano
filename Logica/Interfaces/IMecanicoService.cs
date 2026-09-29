using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;

namespace Mecano.Logica.Interfaces;

public interface IMecanicoService
{
    Task<List<Mecanico>> ObtenerPorEspecialidadAsync(int categoriaId);
    Task<List<Mecanico>> ObtenerDisponiblesAsync(int categoriaId, DateTime fecha, TimeOnly horaInicio, TimeOnly horaFin);
}
