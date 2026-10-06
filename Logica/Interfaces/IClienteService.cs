using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Cliente;

namespace Mecano.Logica.Interfaces;

public interface IClienteService
{
    // --- Legado: se conserva la firma porque AgendarCita.razor la consume. ---
    Task<List<Cliente>> BuscarClientesAsync(string? termino, int pagina = 1, int tamanioPagina = 10);
    Task<Cliente?> ObtenerPorIdAsync(int clienteId);
    Task<int> RegistrarAsync(string cedulaIdentidad, string nombreCompleto, string? telefono, string? correo);

    // --- CRUD ---
    Task<List<ClienteDTO>> BuscarClientesPaginadoAsync(string? termino, int pagina, int tamanioPagina, bool incluirInactivos = false);
    Task<List<ClienteDTO>> ObtenerTodosAsync(bool soloActivos = true);
    Task<ClienteDTO?> ObtenerDetallePorIdAsync(int id);
    Task<int> CrearAsync(CrearClienteDTO dto);
    Task<bool> ActualizarAsync(ActualizarClienteDTO dto);

    // --- Estado ---
    Task<bool> DesactivarAsync(int id);
    Task<bool> ReactivarAsync(int id);
    Task<bool> EliminarDefinitivoAsync(int id);

    // --- Helpers ---
    Task<int> ContarBusquedaAsync(string? termino, bool incluirInactivos = false);
    Task<bool> ExisteIdentificacionAsync(string cedulaIdentidad, int? excluirId = null);
}
