using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Logica.Interfaces;

namespace Mecano.Logica.Servicios;

public class NotificationService : INotificationService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IDbContextFactory<MySQLDBContext> factory, ILogger<NotificationService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task NotificarCitaAgendadaAsync(Cita cita)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        // Ensure navigation properties are loaded if not already provided
        if (cita.Cliente == null || cita.Servicio == null)
        {
            cita = await context.Cita
                .Include(c => c.Cliente)
                .Include(c => c.Servicio)
                .FirstOrDefaultAsync(c => c.CitaId == cita.CitaId) ?? cita;
        }

        string destinatarioCorreo = cita.Cliente?.Correo ?? "no-email@mecano.cr";
        string asunto = $"Cita Agendada - {cita.Servicio?.Nombre}";
        string mensaje = $"Estimado {cita.Cliente?.NombreCompleto},\nSu cita para {cita.Servicio?.Nombre} ha sido agendada para el {cita.Fecha:dd/MM/yyyy} a las {cita.HoraInicio}.";

        _logger.LogInformation($"Notificación simulada enviada a: {destinatarioCorreo}");

        var log = new NotificacionLog
        {
            CitaId = cita.CitaId,
            DestinatarioCorreo = destinatarioCorreo,
            Asunto = asunto,
            MensajeBody = mensaje,
            FechaEnvio = DateTime.Now,
            EnviadoExitosamente = true
        };

        context.NotificacionLog.Add(log);
        await context.SaveChangesAsync();
    }
}
