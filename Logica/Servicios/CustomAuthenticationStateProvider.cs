using System.Security.Claims;
using Mecano.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mecano.Logica.Servicios
{
    // Heredamos de RevalidatingServerAuthenticationStateProvider en lugar de AuthenticationStateProvider
    // para evitar el antipatrón de mantener sesiones fantasma en Blazor Server cuando el usuario es desactivado.
    public class CustomAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CustomAuthenticationStateProvider(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
            : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Revalidamos la sesión en background cada 10 minutos
        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(10);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var user = authenticationState.User;

            // Si no está autenticado, retornamos true porque es un estado válido (usuario anónimo).
            // La redirección ocurrirá mediante el componente AuthorizeRouteView en la UI, no aquí.
            if (user.Identity is null || !user.Identity.IsAuthenticated)
            {
                return true;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return false;
            }

            // Usamos IServiceScopeFactory para evitar inyectar un DbContext/DbFactory que viva
            // todo el ciclo de vida del circuito Blazor, previniendo fugas de memoria.
            using var scope = _scopeFactory.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MySQLDBContext>>();
            using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            // Buscamos en Administradores primero
            var admin = await db.Administradors
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AdministradorId == userId, cancellationToken);
            
            if (admin is not null) 
            {
                return admin.Activo;
            }

            // Si no es admin, buscamos en Mecanicos
            var mecanico = await db.Mecanico
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MecanicoId == userId, cancellationToken);
            
            if (mecanico is not null) 
            {
                return mecanico.Activo;
            }

            // Retorna false SOLO si fue eliminado o desactivado
            return false;
        }
    }
}
