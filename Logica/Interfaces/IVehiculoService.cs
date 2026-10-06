using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;

namespace Mecano.Logica.Interfaces;

public interface IVehiculoService
{
    /// <summary>Vehículos activos de un cliente (los desactivados no se listan).</summary>
    Task<List<Vehiculo>> ObtenerPorClienteAsync(int clienteId, bool incluirInactivos = false);
    Task<int> CrearAsync(NuevoVehiculoDTO dto);
    Task<bool> ActualizarAsync(ActualizarVehiculoDTO dto);
    Task<bool> EliminarAsync(int id);
    Task<bool> TransferirAsync(int vehiculoId, int nuevoClienteId);
}
