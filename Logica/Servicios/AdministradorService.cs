using Mecano.Data;
using Mecano.Entidad.DTOs.Perfil;
using Mecano.Entidad.Utilidades;
using Mecano.Logica.Interfaces;
using Mecano.Logica.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecano.Logica.Servicios;

/// <summary>
/// Perfil del administrador autenticado. La verificación de contraseña reutiliza
/// <see cref="PasswordHasher"/> — fuente única de verdad del algoritmo — y admite
/// el formato legado en texto plano con el mismo criterio que
/// <c>AuthServices.VerifyPassword</c>.
/// </summary>
public class AdministradorService : IAdministradorService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<AdministradorService> _logger;

    public AdministradorService(IDbContextFactory<MySQLDBContext> factory, ILogger<AdministradorService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<PerfilAdminDTO?> ObtenerPerfilAsync(int adminId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Administradors
            .AsNoTracking()
            .Where(a => a.AdministradorId == adminId)
            .Select(a => new PerfilAdminDTO
            {
                Id                   = a.AdministradorId,
                Nombre               = a.Nombre,
                Cedula               = a.Cedula,
                TipoIdentificacion   = a.TipoIdentificacion,
                Correo               = a.Email,
                CorreoNotificaciones = a.CorreoNotificaciones
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ActualizarPerfilAsync(int adminId, ActualizarPerfilAdminDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var admin = await context.Administradors.FindAsync(adminId);
        if (admin is null) return false;

        var nombre = dto.Nombre.Trim();
        var cedula = !string.IsNullOrWhiteSpace(dto.Cedula)
            ? Formatos.NormalizarIdentificacion(dto.Cedula, dto.TipoIdentificacion)
            : null;

        var correo = string.IsNullOrWhiteSpace(dto.CorreoNotificaciones)
            ? null
            : dto.CorreoNotificaciones.Trim();

        if (correo is not null)
        {
            var correoNormalizado = correo.ToLowerInvariant();
            bool duplicado = await context.Administradors.AnyAsync(a =>
                a.AdministradorId != adminId &&
                a.CorreoNotificaciones != null &&
                a.CorreoNotificaciones.ToLower() == correoNormalizado);

            if (duplicado)
                throw new InvalidOperationException(
                    $"El correo de notificaciones '{correo}' ya está en uso por otro administrador.");
        }

        admin.Nombre               = nombre;
        admin.Cedula               = cedula;
        admin.TipoIdentificacion   = dto.TipoIdentificacion;
        admin.CorreoNotificaciones = correo;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CambiarPasswordAsync(int adminId, CambiarPasswordDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var admin = await context.Administradors.FindAsync(adminId);
        if (admin is null) return false;

        if (string.IsNullOrWhiteSpace(dto.NuevaPassword))
            throw new InvalidOperationException("La nueva contraseña es obligatoria.");

        if (!CoincidePassword(dto.PasswordActual, admin.HashPassword))
            throw new InvalidOperationException("La contraseña actual no es correcta.");

        if (dto.NuevaPassword == dto.PasswordActual)
            throw new InvalidOperationException("La nueva contraseña debe ser diferente a la actual.");

        admin.HashPassword = PasswordHasher.Hash(dto.NuevaPassword);
        await context.SaveChangesAsync();

        _logger.LogInformation("Contraseña actualizada para el administrador {AdminId}.", adminId);
        return true;
    }

    /// <summary>
    /// Verifica la contraseña contra el hash almacenado con el mismo criterio que
    /// <c>AuthServices.VerifyPassword</c>: PBKDF2 cuando el hash contiene '$';
    /// comparación directa cuando es el formato legado en texto plano.
    /// </summary>
    private static bool CoincidePassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        return hash.Contains('$') ? PasswordHasher.Verify(password, hash) : password == hash;
    }
}
