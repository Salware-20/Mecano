using System;
using System.Security.Cryptography;

namespace Mecano.Logica.Utilidades;

/// <summary>
/// Utilidad estática de hashing de contraseñas. Fuente única de verdad para
/// el algoritmo PBKDF2-SHA256 usado en la aplicación.
///
/// Formato de almacenamiento: {saltBase64}${hashBase64}
///
/// No verifica contraseñas en texto plano (hash sin '$'): esa responsabilidad
/// recae en los llamadores (AuthServices.VerifyPassword) que necesitan contexto
/// de log para ese caso.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize  = 16;
    private const int HashSize  = 32;
    private const int Iterations = 310_000;

    /// <summary>
    /// Genera un hash seguro para la contraseña proporcionada.
    /// </summary>
    /// <param name="password">Contraseña en texto plano. No puede ser nula ni estar vacía.</param>
    /// <returns>Cadena con formato {saltBase64}${hashBase64}.</returns>
    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));

        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verifica una contraseña contra un hash almacenado.
    /// Devuelve false ante cualquier error de formato — no lanza excepciones.
    /// Devuelve false si el hash no contiene '$' (texto plano): el llamador
    /// debe manejar ese caso si corresponde.
    /// </summary>
    /// <param name="password">Contraseña en texto plano a verificar.</param>
    /// <param name="storedHash">Hash almacenado con formato {saltBase64}${hashBase64}.</param>
    /// <returns>true si la contraseña coincide con el hash; false en cualquier otro caso.</returns>
    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        if (!storedHash.Contains('$'))
            return false;

        try
        {
            string[] parts = storedHash.Split('$');
            if (parts.Length != 2) return false;

            byte[] salt        = Convert.FromBase64String(parts[0]);
            byte[] storedBytes = Convert.FromBase64String(parts[1]);

            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return CryptographicOperations.FixedTimeEquals(storedBytes, computedHash);
        }
        catch
        {
            // Formato de hash inválido u otro error de criptografía: denegar silenciosamente.
            return false;
        }
    }
}
