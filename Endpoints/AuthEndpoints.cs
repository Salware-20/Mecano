using System.Security.Claims;
using Mecano.Entidad.DTOs;
using Mecano.Logica.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Mecano.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth");

            group.MapPost("/login", async (
                [FromBody] LoginDTO request,
                IAuthServices authService,
                HttpContext http,
                ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("Mecano.Endpoints.AuthEndpoints");

                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    logger.LogWarning("Intento de inicio de sesión con campos vacíos.");
                    return Results.Unauthorized();
                }

                var user = await authService.LoginAsync(request.Email, request.Password);
                if (user is null)
                {
                    // Seguridad: Logueamos el intento fallido sin incluir la contraseña ni revelar detalles al cliente
                    logger.LogWarning("Intento fallido de inicio de sesión para el email: {Email}", request.Email);
                    return Results.Unauthorized();
                }

                // Construcción de los Claims requeridos
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.id.ToString()),
                    new(ClaimTypes.Name, user.nombre),
                    new(ClaimTypes.Email, user.email),
                    new(ClaimTypes.Role, user.rol)
                };

                // Incluir claim EsAdminGlobal SOLO si es Global Admin
                if (user.esAdminGlobal)
                {
                    claims.Add(new Claim("EsAdminGlobal", "true"));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await http.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    authProperties);

                logger.LogInformation("Usuario autenticado exitosamente: {Email} (Rol: {Rol})", user.email, user.rol);
                return Results.Ok(new { message = "Autenticación exitosa", user = new { user.id, user.nombre, user.email, user.rol } });
            });

            group.MapPost("/logout", async (HttpContext http) =>
            {
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Ok();
            }).RequireAuthorization();

            return app;
        }
    }
}
