using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs.Cliente;

namespace Mecano.Logica.Interfaces;

public interface IClienteService
{
    // --- Legado: se conserva la firma porque AgendarCita.razor la consume. ---
    Task<List<Cliente>> BuscarClientesAsync(string? termino, int pagina = 1, int tamanioPagina = 10);
    Task<Cliente?> ObtenerPorIdAsync(int clienteId);
    Task<int> RegistrarAsync(string cedulaIdentidad, string nombreCompleto, string? telefono, string? correo);

    // --- CRUD ---
    /// <summary>
    /// Lista paginada de clientes. <paramref name="incluirIdentificacion"/> debe ser <c>false</c>
    /// para los roles que no pueden ver la cédula (Mecánico): en ese caso la búsqueda NO matchea
    /// <c>CedulaIdentidad</c>, solo nombre y placa.
    /// </summary>
    Task<List<ClienteDTO>> BuscarClientesPaginadoAsync(string? termino, int pagina, int tamanioPagina, FiltroActivo filtro = FiltroActivo.Activos, bool incluirIdentificacion = true);
    Task<List<ClienteDTO>> ObtenerTodosAsync(bool soloActivos = true);
    Task<ClienteDTO?> ObtenerDetallePorIdAsync(int id);
    Task<int> CrearAsync(CrearClienteDTO dto);
    Task<bool> ActualizarAsync(ActualizarClienteDTO dto);

    // --- Estado ---
    Task<bool> DesactivarAsync(int id);
    Task<bool> ReactivarAsync(int id);
    Task<bool> EliminarDefinitivoAsync(int id);

    // --- Helpers ---
    /// <summary>Conteo para la paginación; debe recibir el mismo <paramref name="incluirIdentificacion"/> que la consulta paginada.</summary>
    Task<int> ContarBusquedaAsync(string? termino, FiltroActivo filtro = FiltroActivo.Activos, bool incluirIdentificacion = true);
    Task<bool> ExisteIdentificacionAsync(string cedulaIdentidad, int? excluirId = null, TipoIdentificacion? tipo = null);
}
