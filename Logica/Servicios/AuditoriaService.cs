using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Auditoria;
using Mecano.Logica.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecano.Logica.Servicios
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly IDbContextFactory<MySQLDBContext> _factory;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(
            IDbContextFactory<MySQLDBContext> factory,
            AuthenticationStateProvider authStateProvider,
            ILogger<AuditoriaService> logger)
        {
            _factory = factory;
            _authStateProvider = authStateProvider;
            _logger = logger;
        }

        /// <summary>
        /// Registra una acción crítica en <c>AuditoriaLogs</c>. Los errores de escritura
        /// se absorben (solo se loggean): el fallo del log nunca debe bloquear la operación
        /// de negocio que lo desencadenó.
        /// </summary>
        public async Task RegistrarAsync(
            string tipoAccion,
            string entidadAfectada,
            string? entidadId,
            string detalles,
            string? usuarioId = null,
            string? usuarioNombre = null,
            string? usuarioRol = null)
        {
            try
            {
                await using var context = await _factory.CreateDbContextAsync();

                await AgregarEnContextoAsync(
                    context, tipoAccion, entidadAfectada, entidadId, detalles,
                    usuarioId, usuarioNombre, usuarioRol);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // No bloquear la operación principal si el log falla.
                _logger.LogError(ex, "Error al registrar auditoría ({TipoAccion}, {EntidadAfectada} #{EntidadId})",
                    tipoAccion, entidadAfectada, entidadId);
            }
        }

        /// <summary>
        /// Agrega la entrada de auditoría al contexto recibido sin guardar. El llamador
        /// confirma con su propio <c>SaveChanges</c>, de modo que la mutación de negocio y
        /// su rastro de auditoría comparten la misma transacción.
        /// </summary>
        public async Task AgregarEnContextoAsync(
            MySQLDBContext context,
            string tipoAccion,
            string entidadAfectada,
            string? entidadId,
            string detalles,
            string? usuarioId = null,
            string? usuarioNombre = null,
            string? usuarioRol = null)
        {
            ArgumentNullException.ThrowIfNull(context);

            (usuarioId, usuarioNombre, usuarioRol) =
                await CompletarIdentidadAsync(usuarioId, usuarioNombre, usuarioRol);

            context.AuditoriaLogs.Add(new AuditoriaLog
            {
                FechaHora       = DateTime.Now,
                TipoAccion      = tipoAccion,
                UsuarioId       = usuarioId,
                UsuarioNombre   = usuarioNombre,
                UsuarioRol      = usuarioRol,
                EntidadAfectada = entidadAfectada,
                EntidadId       = entidadId,
                Detalles        = detalles
            });
        }

        /// <summary>
        /// Completa los datos de identidad que falten con los claims del circuito.
        /// Fuera de Blazor (sin estado de autenticación) conserva lo recibido.
        /// </summary>
        private async Task<(string? Id, string? Nombre, string? Rol)> CompletarIdentidadAsync(
            string? usuarioId,
            string? usuarioNombre,
            string? usuarioRol)
        {
            if (!string.IsNullOrWhiteSpace(usuarioId)
                && !string.IsNullOrWhiteSpace(usuarioNombre)
                && !string.IsNullOrWhiteSpace(usuarioRol))
            {
                return (usuarioId, usuarioNombre, usuarioRol);
            }

            try
            {
                var state = await _authStateProvider.GetAuthenticationStateAsync();
                var user = state.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    if (string.IsNullOrWhiteSpace(usuarioId))
                        usuarioId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrWhiteSpace(usuarioNombre))
                        usuarioNombre = user.FindFirst(ClaimTypes.Name)?.Value;

                    if (string.IsNullOrWhiteSpace(usuarioRol))
                        usuarioRol = user.FindFirst(ClaimTypes.Role)?.Value;
                }
            }
            catch
            {
                // Sin circuito Blazor no hay AuthenticationState: se conserva lo recibido.
            }

            return (usuarioId, usuarioNombre, usuarioRol);
        }

        /// <summary>
        /// Devuelve una página de entradas de auditoría en orden descendente por fecha,
        /// aplicando los filtros opcionales del DTO. El total sirve para la paginación numerada.
        /// </summary>
        public async Task<(List<AuditoriaLogDTO> Items, int Total)> ObtenerPaginadoAsync(
            FiltroAuditoriaDTO filtro,
            int pagina,
            int tamanioPagina)
        {
            await using var context = await _factory.CreateDbContextAsync();

            var query = context.AuditoriaLogs.AsNoTracking().AsQueryable();

            if (filtro.Desde.HasValue)
                query = query.Where(l => l.FechaHora >= filtro.Desde.Value.Date);

            if (filtro.Hasta.HasValue)
                query = query.Where(l => l.FechaHora < filtro.Hasta.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filtro.TipoAccion))
            {
                var tipo = filtro.TipoAccion.Trim();
                query = query.Where(l => l.TipoAccion.Contains(tipo));
            }

            if (!string.IsNullOrWhiteSpace(filtro.UsuarioNombre))
            {
                var nombre = filtro.UsuarioNombre.Trim();
                query = query.Where(l =>
                    (l.UsuarioNombre != null && l.UsuarioNombre.Contains(nombre))
                    || (l.UsuarioRol != null && l.UsuarioRol.Contains(nombre)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(l => l.FechaHora)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(l => new AuditoriaLogDTO
                {
                    Id               = l.Id,
                    FechaHora        = l.FechaHora,
                    TipoAccion       = l.TipoAccion,
                    UsuarioNombre    = l.UsuarioNombre,
                    UsuarioRol       = l.UsuarioRol,
                    EntidadAfectada  = l.EntidadAfectada,
                    EntidadId        = l.EntidadId,
                    Detalles         = l.Detalles
                })
                .ToListAsync();

            return (items, total);
        }

        /// <summary>
        /// Devuelve el <c>CorreoNotificaciones</c> del Administrador Global, o <c>null</c>
        /// si no está configurado. Lo usa la página /auditoria para construir el mailto:.
        /// </summary>
        public async Task<string?> ObtenerCorreoOwnerAsync()
        {
            await using var context = await _factory.CreateDbContextAsync();

            var correo = await context.Administradors
                .AsNoTracking()
                .Where(a => a.EsAdminGlobal && !string.IsNullOrWhiteSpace(a.CorreoNotificaciones))
                .Select(a => a.CorreoNotificaciones)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(correo) ? null : correo.Trim();
        }
    }
}
