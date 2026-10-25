using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Administrador;
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

    public async Task<List<AdministradorListaDTO>> ListarAsync(bool incluirInactivos = true)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var query = context.Administradors.AsNoTracking();
        if (!incluirInactivos)
            query = query.Where(a => a.Activo);

        return await query
            .OrderByDescending(a => a.EsAdminGlobal)
            .ThenBy(a => a.Nombre)
            .Select(a => new AdministradorListaDTO
            {
                AdministradorId = a.AdministradorId,
                Nombre = a.Nombre,
                Email = a.Email,
                Cedula = a.Cedula,
                TipoIdentificacion = a.TipoIdentificacion,
                Telefono = a.Telefono,
                Activo = a.Activo,
                EsAdminGlobal = a.EsAdminGlobal,
                CorreoNotificaciones = a.CorreoNotificaciones
            })
            .ToListAsync();
    }

    public async Task<AdministradorListaDTO?> ObtenerPorIdAsync(int adminId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Administradors
            .AsNoTracking()
            .Where(a => a.AdministradorId == adminId)
            .Select(a => new AdministradorListaDTO
            {
                AdministradorId = a.AdministradorId,
                Nombre = a.Nombre,
                Email = a.Email,
                Cedula = a.Cedula,
                TipoIdentificacion = a.TipoIdentificacion,
                Telefono = a.Telefono,
                Activo = a.Activo,
                EsAdminGlobal = a.EsAdminGlobal,
                CorreoNotificaciones = a.CorreoNotificaciones
            })
            .FirstOrDefaultAsync();
    }

    public async Task<int> CrearAsync(CrearAdministradorDTO dto, int creadoPorId)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new InvalidOperationException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new InvalidOperationException("El correo es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            throw new InvalidOperationException("La contraseña debe tener al menos 6 caracteres.");
        if (dto.Password != dto.ConfirmarPassword)
            throw new InvalidOperationException("Las contraseñas no coinciden.");

        await using var context = await _factory.CreateDbContextAsync();

        var email = dto.Email.Trim().ToLowerInvariant();
        bool existeEmail = await context.Administradors.AnyAsync(a => a.Email.ToLower() == email)
            || await context.Mecanico.AnyAsync(m => m.Email.ToLower() == email);
        if (existeEmail)
            throw new InvalidOperationException("Ya existe un usuario con ese correo electrónico.");

        string? cedula = !string.IsNullOrWhiteSpace(dto.Cedula)
            ? Formatos.NormalizarIdentificacion(dto.Cedula, dto.TipoIdentificacion)
            : null;

        string? telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        string? correoNotificaciones = string.IsNullOrWhiteSpace(dto.CorreoNotificaciones)
            ? null
            : dto.CorreoNotificaciones.Trim().ToLowerInvariant();

        if (correoNotificaciones is not null)
        {
            bool duplicadoNotif = await context.Administradors.AnyAsync(a =>
                a.CorreoNotificaciones != null && a.CorreoNotificaciones.ToLower() == correoNotificaciones);
            if (duplicadoNotif)
                throw new InvalidOperationException("El correo de notificaciones ya está en uso por otro administrador.");
        }

        var admin = new Administrador
        {
            Nombre = dto.Nombre.Trim(),
            Email = dto.Email.Trim(),
            Cedula = cedula,
            TipoIdentificacion = dto.TipoIdentificacion,
            Telefono = telefono,
            CorreoNotificaciones = correoNotificaciones,
            HashPassword = PasswordHasher.Hash(dto.Password),
            Activo = true,
            EsAdminGlobal = false
        };

        context.Administradors.Add(admin);
        await context.SaveChangesAsync();

        _logger.LogInformation("Administrador {Email} creado por {CreadorId}.", admin.Email, creadoPorId);
        return admin.AdministradorId;
    }

    public async Task<bool> ActualizarAsync(ActualizarAdministradorDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var admin = await context.Administradors.FindAsync(dto.AdministradorId);
        if (admin is null) return false;

        var nombre = dto.Nombre.Trim();
        string? cedula = !string.IsNullOrWhiteSpace(dto.Cedula)
            ? Formatos.NormalizarIdentificacion(dto.Cedula, dto.TipoIdentificacion)
            : null;

        string? telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        string? correoNotificaciones = string.IsNullOrWhiteSpace(dto.CorreoNotificaciones)
            ? null
            : dto.CorreoNotificaciones.Trim().ToLowerInvariant();

        if (correoNotificaciones is not null)
        {
            bool duplicadoNotif = await context.Administradors.AnyAsync(a =>
                a.AdministradorId != admin.AdministradorId &&
                a.CorreoNotificaciones != null &&
                a.CorreoNotificaciones.ToLower() == correoNotificaciones);
            if (duplicadoNotif)
                throw new InvalidOperationException("El correo de notificaciones ya está en uso por otro administrador.");
        }

        admin.Nombre = nombre;
        admin.Cedula = cedula;
        admin.TipoIdentificacion = dto.TipoIdentificacion;
        admin.Telefono = telefono;
        admin.CorreoNotificaciones = correoNotificaciones;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DesactivarAsync(int adminId, int solicitanteId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        if (adminId == solicitanteId)
            throw new InvalidOperationException("No puede desactivar su propia cuenta.");

        var admin = await context.Administradors.FindAsync(adminId);
        if (admin is null) return false;
        if (admin.EsAdminGlobal)
            throw new InvalidOperationException("No se puede desactivar al administrador global.");

        admin.Activo = false;
        await context.SaveChangesAsync();
        _logger.LogInformation("Administrador {AdminId} desactivado por {SolicitanteId}.", adminId, solicitanteId);
        return true;
    }

    public async Task<bool> ReactivarAsync(int adminId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var admin = await context.Administradors.FindAsync(adminId);
        if (admin is null) return false;

        admin.Activo = true;
        await context.SaveChangesAsync();
        _logger.LogInformation("Administrador {AdminId} reactivado.", adminId);
        return true;
    }

    public async Task<bool> ExisteEmailAsync(string email, int? excluirId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var cleanEmail = email.Trim().ToLowerInvariant();

        bool existe = await context.Administradors.AnyAsync(a =>
            a.Email.ToLower() == cleanEmail && (!excluirId.HasValue || a.AdministradorId != excluirId.Value))
            || await context.Mecanico.AnyAsync(m => m.Email.ToLower() == cleanEmail);

        return existe;
    }
}
