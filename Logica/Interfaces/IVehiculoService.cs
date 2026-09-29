using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;

namespace Mecano.Logica.Interfaces;

public interface IVehiculoService
{
    Task<List<Vehiculo>> ObtenerPorClienteAsync(int clienteId);
    Task<Vehiculo> RegistrarAsync(int clienteId, string placa, string marca, string modelo, int anio);
}
