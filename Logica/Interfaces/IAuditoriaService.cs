using System.Threading.Tasks;

namespace Mecano.Logica.Interfaces
{
    public interface IAuditoriaService
    {
        Task RegistrarAsync(string tipoAccion, string entidadAfectada, string? entidadId, string detalles, string? usuarioId = null, string? usuarioNombre = null);
    }
}
