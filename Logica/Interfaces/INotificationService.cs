using System.Threading.Tasks;
using Mecano.Entidad.Clases;

namespace Mecano.Logica.Interfaces;

public interface INotificationService
{
    Task NotificarCitaAgendadaAsync(Cita cita);
}
