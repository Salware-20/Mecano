using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Logica.Interfaces;
using Mecano.Logica.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecano.Logica.Servicios
{
    public class AuthServices : IAuthServices
    {
        private readonly IDbContextFactory<MySQLDBContext> _DB;
        private readonly ILogger<AuthServices> _logger;

        public AuthServices(IDbContextFactory<MySQLDBContext> dB, ILogger<AuthServices> logger)
        {
            _DB = dB;
            _logger = logger;
        }

        /// <summary>
        /// Genera un hash seguro para la contraseña proporcionada.
        /// Delega en <see cref="PasswordHasher.Hash"/> — fuente única de verdad para el algoritmo.
        /// Se mantiene public static para que DbSeeder pueda llamarlo sin inyectar el servicio.
        /// </summary>
        public static string HashPassword(string password)
            => PasswordHasher.Hash(password);

        /// <summary>
        /// Verifica la contraseña contra el hash almacenado.
        /// Maneja el caso legado de contraseñas en texto plano (sin '$') con un log de advertencia,
        /// y delega la verificación PBKDF2 en <see cref="PasswordHasher.Verify"/>.
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            ArgumentException.ThrowIfNullOrWhiteSpace(hash);

            // Compatibilidad: si en la base de datos se insertó la contraseña en texto plano (sin '$')
            if (!hash.Contains('$'))
            {
                if (password == hash)
                {
                    _logger.LogWarning("Contraseña en texto plano detectada en la BD durante el login. Se valida correctamente.");
                    return true;
                }
                return false;
            }

            return PasswordHasher.Verify(password, hash);
        }

        public async Task<bool> EmailExisteAsync(string email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            var cleanEmail = email.Trim().ToLowerInvariant();
            using var SQL = await _DB.CreateDbContextAsync();
            return await SQL.Administradors.AnyAsync(a => a.Email.ToLower() == cleanEmail)
                || await SQL.Mecanico.AnyAsync(m => m.Email == cleanEmail);
        }

        public async Task<AuthenticatedUser?> LoginAsync(string email, string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            var cleanEmail = email.Trim();
            using var SQL = await _DB.CreateDbContextAsync();

            // 1. Buscamos en Administradores (insensible a mayúsculas/minúsculas y espacios)
            var admin = await SQL.Administradors.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email.ToLower() == cleanEmail.ToLower());

            if (admin is not null)
            {
                if (!admin.Activo)
                {
                    _logger.LogWarning("Intento de login rechazado: Administrador inactivo ({Email}).", cleanEmail);
                    return null;
                }

                if (VerifyPassword(password, admin.HashPassword))
                {
                    _logger.LogInformation("Login exitoso como Administrador: {Email}", cleanEmail);
                    return new AuthenticatedUser(
                        admin.AdministradorId,
                        admin.Email,
                        admin.Nombre,
                        Entidad.Constantes.Roles.Administrador,
                        admin.EsAdminGlobal);
                }
                else
                {
                    _logger.LogWarning("Intento de login rechazado: Contraseña incorrecta para Administrador ({Email}).", cleanEmail);
                    return null;
                }
            }

            // 2. Si no es Administrador, buscamos en Mecánicos
            var mec = await SQL.Mecanico.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Email.ToLower() == cleanEmail.ToLower());

            if (mec is not null)
            {
                if (!mec.Activo)
                {
                    _logger.LogWarning("Intento de login rechazado: Mecánico inactivo ({Email}).", cleanEmail);
                    return null;
                }

                if (VerifyPassword(password, mec.HashPassword))
                {
                    _logger.LogInformation("Login exitoso como Mecánico: {Email}", cleanEmail);
                    return new AuthenticatedUser(
                        mec.MecanicoId,
                        mec.Email,
                        mec.Nombre,
                        Entidad.Constantes.Roles.Mecanico,
                        false);
                }
                else
                {
                    _logger.LogWarning("Intento de login rechazado: Contraseña incorrecta para Mecánico ({Email}).", cleanEmail);
                    return null;
                }
            }

            _logger.LogWarning("Intento de login rechazado: Correo no encontrado en Administradores ni Mecánicos ({Email}).", cleanEmail);
            return null;
        }

        public async Task<bool> RegisterAdminAsync(string email, string nombre, string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            var cleanEmail = email.Trim().ToLower();
            using var SQL = await _DB.CreateDbContextAsync();

            bool existeEmail = await SQL.Administradors.AnyAsync(a => a.Email.ToLower() == cleanEmail)
                || await SQL.Mecanico.AnyAsync(m => m.Email.ToLower() == cleanEmail);
            if (existeEmail) return false;

            var admin = new Administrador
            {
                Email = email.Trim(),
                Nombre = nombre.Trim(),
                HashPassword = HashPassword(password),
                Activo = true
            };

            SQL.Administradors.Add(admin);
            await SQL.SaveChangesAsync();

            return true;
        }
    }
}
