using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Categoria;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mecano.Logica.Interfaces;

public interface ICategoriaService
{
    Task<List<CategoriaDTO>> ObtenerTodasAsync();
    Task<CategoriaDTO> ObtenerPorIdAsync(int id);

    // Nuevas para el CRUD:
    Task<int> CrearAsync(CrearCategoriaDTO dto);
    Task<bool> ActualizarAsync(ActualizarCategoriaDTO dto);
    Task<bool> EliminarAsync(int id);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
}
