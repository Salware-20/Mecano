using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Logica.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecano.Logica.Servicios
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly IDbContextFactory<MySQLDBContext> _factory;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(IDbContextFactory<MySQLDBContext> factory, ILogger<AuditoriaService> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task RegistrarAsync(string tipoAccion, string entidadAfectada, string? entidadId, string detalles, string? usuarioId = null, string? usuarioNombre = null)
        {
            try
            {
                await using var context = await _factory.CreateDbContextAsync();

                var log = new AuditoriaLog
                {
                    FechaHora = DateTime.Now,
                    TipoAccion = tipoAccion,
                    UsuarioId = usuarioId,
                    UsuarioNombre = usuarioNombre,
                    EntidadAfectada = entidadAfectada,
                    EntidadId = entidadId,
                    Detalles = detalles
                };

                context.Set<AuditoriaLog>().Add(log);
                await context.SaveChangesAsync();

                // Enviar correo a CorreoNotificaciones del administrador
                await EnviarCorreoAuditoriaAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría");
                // No bloquear operación principal
            }
        }

        private async Task EnviarCorreoAuditoriaAsync(AuditoriaLog log)
        {
            try
            {
                await using var context = await _factory.CreateDbContextAsync();
                var admin = await context.Administradors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Activo && !string.IsNullOrWhiteSpace(a.CorreoNotificaciones));

                if (admin == null || string.IsNullOrWhiteSpace(admin.CorreoNotificaciones))
                    return;

                var destinatario = admin.CorreoNotificaciones.Trim();
                var asunto = $"[Mecano Auditoría] {log.TipoAccion} - {log.EntidadAfectada}";
                var cuerpo = $@"<html><body>
<h2>Registro de auditoría - Mecano</h2>
<table border='1' cellpadding='5' cellspacing='0'>
<tr><td><strong>Fecha/Hora</strong></td><td>{log.FechaHora:yyyy-MM-dd HH:mm:ss}</td></tr>
<tr><td><strong>Tipo de Acción</strong></td><td>{WebUtility.HtmlEncode(log.TipoAccion)}</td></tr>
<tr><td><strong>Usuario</strong></td><td>{WebUtility.HtmlEncode(log.UsuarioNombre ?? "-")} ({WebUtility.HtmlEncode(log.UsuarioId ?? "-")})</td></tr>
<tr><td><strong>Entidad Afectada</strong></td><td>{WebUtility.HtmlEncode(log.EntidadAfectada)}</td></tr>
<tr><td><strong>ID Entidad</strong></td><td>{WebUtility.HtmlEncode(log.EntidadId ?? "-")}</td></tr>
<tr><td><strong>Detalles</strong></td><td>{WebUtility.HtmlEncode(log.Detalles ?? "-")}</td></tr>
</table>
</body></html>";

                using var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("mecano.notificaciones@gmail.com", "sxst ygfx ctjc ldwg")
                };

                var mail = new MailMessage
                {
                    From = new MailAddress("mecano.notificaciones@gmail.com", "Mecano"),
                    Subject = asunto,
                    Body = cuerpo,
                    IsBodyHtml = true
                };
                mail.To.Add(destinatario);
                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de auditoría");
            }
        }
    }
}
