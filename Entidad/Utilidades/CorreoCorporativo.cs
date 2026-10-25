using System;
using System.Text.RegularExpressions;

namespace Mecano.Entidad.Utilidades
{
    /// <summary>
    /// Reglas del correo corporativo de los administradores. El dominio es fijo:
    /// la persona solo aporta el alias y el servicio compone <c>alias@mecano.cr</c>.
    /// </summary>
    public static class CorreoCorporativo
    {
        /// <summary>Dominio corporativo único para las cuentas de administración.</summary>
        public const string Dominio = "@mecano.cr";

        /// <summary>Largo máximo permitido para el alias (sin dominio).</summary>
        public const int LargoMaximoAlias = 64;

        /// <summary>
        /// Normaliza un alias: recorta extremos, pasa a minúsculas y, si se pegó
        /// el correo corporativo completo, elimina el sufijo <c>@mecano.cr</c>.
        /// No elimina dominios externos: esos deben fallar la validación.
        /// </summary>
        public static string NormalizarAlias(string? alias)
        {
            var valor = (alias ?? string.Empty).Trim().ToLowerInvariant();

            if (valor.EndsWith(Dominio, StringComparison.Ordinal))
                valor = valor[..^Dominio.Length];

            return valor;
        }

        /// <summary>Compone el correo corporativo a partir del alias.</summary>
        public static string Componer(string? alias) =>
            $"{NormalizarAlias(alias)}{Dominio}";

        /// <summary>Indica si el alias (ya normalizado) cumple el patrón y el largo.</summary>
        public static bool EsAliasValido(string? alias) =>
            !string.IsNullOrEmpty(alias) &&
            alias.Length <= LargoMaximoAlias &&
            Regex.IsMatch(alias, Patrones.AliasCorreo);

        /// <summary>
        /// Normaliza y revalida un alias para usarlo desde los servicios. Lanza
        /// <see cref="InvalidOperationException"/> con un mensaje apto para la UI
        /// cuando está vacío, excede el largo o no cumple el patrón.
        /// </summary>
        public static string NormalizarYValidarAlias(string? alias)
        {
            var valor = NormalizarAlias(alias);

            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException("El alias de correo es obligatorio.");
            if (valor.Length > LargoMaximoAlias)
                throw new InvalidOperationException($"El alias no puede superar {LargoMaximoAlias} caracteres.");
            if (!EsAliasValido(valor))
                throw new InvalidOperationException(Patrones.MensajeAliasCorreo);

            return valor;
        }
    }
}
