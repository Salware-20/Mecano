using Mecano.Entidad.DTOs;

namespace Mecano.Logica.Interfaces
{
    public interface IAuthServices
    {
        Task<AuthenticatedUser?> LoginAsync(string email, string password);
        Task<bool> RegisterAdminAsync(string email, string nombre, string password);
        Task<bool> EmailExisteAsync(string email);
    }
}
