using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;

namespace Mecano.Logica.Interfaces;

public interface IClienteService
{
    Task<List<Cliente>> BuscarClientesAsync(string termino, int pagina = 1, int tamanioPagina = 10);
    Task<Cliente?> ObtenerPorIdAsync(int clienteId);
    Task<Cliente> RegistrarAsync(string cedulaIdentidad, string nombreCompleto, string? telefono, string? correo);
}
