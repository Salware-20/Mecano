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

    /// <summary>Historial completo de citas de un vehículo (pasadas, actuales y programadas), más recientes primero.</summary>
    Task<List<CitaDTO>> ObtenerPorVehiculoAsync(int vehiculoId);

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

    // --- UPDATE (reprogramación: fecha, hora y mecánico) ---
    /// <summary>
    /// Reprograma una cita (fecha, hora y mecánico). Conserva el servicio y recalcula <c>HoraFin</c>
    /// con su duración; revalida la ventana de atención y el solape por mecánico. Devuelve false si la cita no existe.
    /// </summary>
    Task<bool> ReprogramarCitaAsync(ReprogramarCitaDTO dto);

    // --- UPDATE (transición de estado) ---
    /// <summary>
    /// Cambia el estado de una cita según las reglas del negocio: Confirmar y Cancelar son
    /// exclusivos de administración (el mecánico debe pasar <c>null</c> en <paramref name="mecanicoId"/>);
    /// Iniciar (Confirmado → EnProceso) y Finalizar (EnProceso → Finalizado) los puede hacer
    /// el administrador o el mecánico asignado a la cita (pasa su id). Los estados terminales
    /// (Finalizado y Cancelado) no se pueden tocar. Lanza <see cref="InvalidOperationException"/>
    /// con mensaje en español si la transición no está permitida; devuelve false si la cita no existe.
    /// </summary>
    Task<bool> TransicionarEstadoAsync(int citaId, EstadoCita destino, int? mecanicoId);

    // --- READ (vista del mecánico) ---
    /// <summary>
    /// Listado paginado de las citas de un mecánico, con la proyección restringida
    /// <see cref="CitaListaMecanicoDTO"/>: cliente solo por nombre y colegas solo por nombre,
    /// sin PII ni precios. El filtro se interpreta siempre dentro de las citas del mecánico.
    /// </summary>
    Task<List<CitaListaMecanicoDTO>> BuscarParaMecanicoAsync(int mecanicoId, FiltroCitasDTO filtro, int pagina, int tamanioPagina);

    /// <summary>Conteo para la paginación del listado del mecánico; mismo filtro que la consulta.</summary>
    Task<int> ContarParaMecanicoAsync(int mecanicoId, FiltroCitasDTO filtro);

    /// <summary>
    /// Detalle de una cita con la proyección restringida <see cref="CitaDetalleMecanicoDTO"/>
    /// (sin precio, sin NotasAdmin y sin PII del cliente). El mecánico solo puede ver
    /// citas asignadas a él: devuelve null si la cita no existe o si no está asignada
    /// al <paramref name="mecanicoId"/> recibido (una URL a mano no abre citas ajenas).
    /// </summary>
    Task<CitaDetalleMecanicoDTO?> ObtenerDetalleParaMecanicoAsync(int citaId, int mecanicoId);
}
