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

    // --- READ (listado y detalle) ---
    /// <summary>Listado paginado de citas filtrado por <see cref="FiltroCitasDTO"/>.</summary>
    Task<List<CitaDTO>> BuscarPaginadoAsync(FiltroCitasDTO filtro, int pagina, int tamanioPagina);

    /// <summary>Conteo para la paginación; debe recibir el mismo filtro que la consulta paginada.</summary>
    Task<int> ContarAsync(FiltroCitasDTO filtro);

    /// <summary>Detalle completo de una cita, o null si no existe.</summary>
    Task<CitaDetalleDTO?> ObtenerDetallePorIdAsync(int citaId);

    // --- UPDATE (datos no de agenda) ---
    /// <summary>
    /// Edita el vehículo, el servicio y las notas de una cita. Recalcula <c>HoraFin</c> a partir de
    /// la duración del nuevo servicio y revalida horario y solape por mecánico. Devuelve false si la cita no existe.
    /// </summary>
    Task<bool> ActualizarCitaAsync(EditarCitaDTO dto);
}
