using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;

namespace Mecano.Logica.Interfaces;

public interface ICitaService
{
    Task<Cita> AgendarCitaAsync(AgendarCitaDTO dto);
    Task CancelarCitaAsync(int citaId);
    Task<List<Cita>> ObtenerPorRangoAsync(DateTime inicio, DateTime fin);
    Task<List<CitaDTO>> ObtenerPorClienteAsync(int clienteId);
}
