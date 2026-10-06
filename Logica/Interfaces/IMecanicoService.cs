using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Mecanico;

namespace Mecano.Logica.Interfaces;

public interface IMecanicoService
{
    // --- Métodos existentes — consumidos por AgendarCita. No modificar. ---

    Task<List<Mecanico>> ObtenerPorEspecialidadAsync(int categoriaId);
    Task<List<Mecanico>> ObtenerDisponiblesAsync(int categoriaId, DateTime fecha, TimeOnly horaInicio, TimeOnly horaFin);

    // --- CRUD administrativo ---

    /// <summary>
    /// Devuelve la lista de mecánicos. Por defecto solo los activos.
    /// Pasar soloActivos = false para obtener todos (activos e inactivos).
    /// </summary>
    Task<List<MecanicoDTO>> ObtenerTodosAsync(bool soloActivos = true);

    /// <summary>
    /// Devuelve el detalle de un mecánico por su id, sin filtrar por Activo,
    /// para que la UI pueda mostrar el estado actual y ofrecer reactivar si corresponde.
    /// Devuelve null si no existe.
    /// </summary>
    Task<MecanicoDTO?> ObtenerDetallePorIdAsync(int id);

    /// <summary>
    /// Crea un mecánico. Lanza InvalidOperationException si el email ya existe
    /// o la especialidad no existe.
    /// </summary>
    /// <returns>Id del mecánico recién creado.</returns>
    Task<int> CrearAsync(CrearMecanicoDTO dto);

    /// <summary>
    /// Actualiza los datos de un mecánico. Devuelve false si no existe.
    /// Lanza InvalidOperationException si el email ya está en uso por otro usuario
    /// o la especialidad no existe.
    /// Si dto.Password es nulo o vacío, el hash almacenado no se modifica.
    /// </summary>
    Task<bool> ActualizarAsync(ActualizarMecanicoDTO dto);

    /// <summary>
    /// Establece Activo = false. Devuelve false si el mecánico no existe.
    /// Las citas existentes conservan su MecanicoId (no se tocan).
    /// </summary>
    Task<bool> DesactivarAsync(int id);

    /// <summary>
    /// Establece Activo = true. Devuelve false si el mecánico no existe.
    /// </summary>
    Task<bool> ReactivarAsync(int id);

    /// <summary>
    /// Comprueba si el email ya está registrado en la tabla de Administradores
    /// o en la de Mecánicos. Si se pasa excluirId, se excluye ese mecánico de
    /// la comprobación (para permitir guardar el mismo email en una actualización).
    /// </summary>
    Task<bool> ExisteEmailAsync(string email, int? excluirId = null);
}
