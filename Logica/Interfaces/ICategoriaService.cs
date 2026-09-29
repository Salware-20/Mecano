using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;

namespace Mecano.Logica.Interfaces;

public interface ICategoriaService
{
    Task<List<Categoria>> ObtenerTodasAsync();
}
