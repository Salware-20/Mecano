using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Logica.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Mecano.Logica.Servicios
{
    public class AuthServices : IAuthServices
    {
        private readonly IDbContextFactory<MySQLDBContext> _DB;
        private readonly ILogger<AuthServices> _logger;
        private const int SALTSIZE = 16; 
        private const int HASHSIZE = 32; 
        private const int Iterations = 310000; 

        public AuthServices(IDbContextFactory<MySQLDBContext> dB, ILogger<AuthServices> logger)
        {
            _DB = dB;
            _logger = logger;
        }

        public static string HashPassword(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));
            byte[] salt = new byte[SALTSIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            } 
            
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HASHSIZE);

                string saltString = Convert.ToBase64String(salt);
                string hashString = Convert.ToBase64String(hash);

                return $"{saltString}${hashString}";
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            ArgumentException.ThrowIfNullOrWhiteSpace(hash);

            try
            {
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

                string[] parts = hash.Split('$');
                if (parts.Length != 2) return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] storeHash = Convert.FromBase64String(parts[1]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] computedHash = pbkdf2.GetBytes(HASHSIZE);
                    return CryptographicOperations.FixedTimeEquals(storeHash, computedHash);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error verificando la contraseña. Formato de hash posiblemente inválido.");
                return false;
            }
        }

        public async Task<bool> EmailExisteAsync(string email)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            var cleanEmail = email.Trim().ToLower();
            using var SQL = await _DB.CreateDbContextAsync();
            return await SQL.Administradors.AnyAsync(a => a.Email.ToLower() == cleanEmail) 
                || await SQL.Mecanico.AnyAsync(m => m.Email.ToLower() == cleanEmail);
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
