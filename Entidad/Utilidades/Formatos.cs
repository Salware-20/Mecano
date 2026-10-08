using System.Globalization;

namespace Mecano.Entidad.Utilidades
{
    /// <summary>
    /// Normalización y formateo de identificadores de Cliente y Vehiculo, y formateo
    /// de precios en colones (moneda de la app).
    ///
    /// Invariante de almacenamiento: Cliente.CedulaIdentidad y Cliente.Telefono se guardan
    /// únicamente con dígitos, y Vehiculo.Placa se guarda en mayúsculas sin guiones
    /// (ABC123). El formato legible es cosa de la capa de presentación, nunca de la de
    /// persistencia.
    ///
    /// Este es el motivo de la normalización en ClienteService y VehiculoService: los
    /// índices únicos comparan cadenas crudas, así que "1-0111-0111" y "101110111"
    /// (o "abc-123" y "ABC123") pasarían como dos registros distintos si se guardaran
    /// tal cual.
    /// </summary>
    public static class Formatos
    {
        public const int LargoCedulaNacional = 9;
        public const int LargoTelefono = 8;
        public const int LargoPlaca = 6;

        // Cultura fija para toda la app: el precio es colón costarricense sin importar
        // el navegador o el servidor. es-CR de .NET da "₡35 000,00" (símbolo ₡ U+20A1,
        // coma decimal, espacio no-cortable U+00A0 como separador de miles).
        private static readonly CultureInfo EsCr = new("es-CR");

        /// <summary>
        /// Formatea un monto como moneda costarricense: 35000 → "₡35 000,00".
        /// Siempre 2 decimales y siempre con la cultura es-CR (no depende de la cultura
        /// del circuito), para que el resultado sea idéntico en cualquier navegador o
        /// servidor. Uso exclusivamente de presentación: nunca para parsear ni persistir.
        /// </summary>
        public static string Moneda(decimal monto) => monto.ToString("C2", EsCr);

        /// <summary>
        /// Elimina todo carácter que no sea un dígito. Devuelve cadena vacía si el input es null.
        /// </summary>
        public static string SoloDigitos(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            // char.IsDigit acepta dígitos Unicode. Para cédulas y teléfonos costarricenses
            // queremos ASCII 0-9, así que descartamos el resto de forma explícita.
            var digitos = new char[raw.Length];
            var largo = 0;

            foreach (var c in raw)
            {
                if (c is >= '0' and <= '9')
                    digitos[largo++] = c;
            }

            return new string(digitos, 0, largo);
        }

        /// <summary>
        /// Formatea "101110111" como "1-0111-0111" (1-4-4).
        /// Devuelve cadena vacía si el input es null/vacío.
        /// Si el largo no es el esperado, devuelve el input original sin modificar: preferimos
        /// mostrar el dato tal cual antes que imprimir guiones en posiciones que no corresponden.
        /// </summary>
        public static string CedulaNacional(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var digitos = SoloDigitos(raw);
            if (digitos.Length != LargoCedulaNacional)
                return raw;

            return $"{digitos[..1]}-{digitos[1..5]}-{digitos[5..]}";
        }

        /// <summary>
        /// Formatea "88888888" como "8888-8888" (4-4).
        /// Mismas reglas que <see cref="CedulaNacional"/> ante null o largo inesperado.
        /// </summary>
        public static string Telefono(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var digitos = SoloDigitos(raw);
            if (digitos.Length != LargoTelefono)
                return raw;

            return $"{digitos[..4]}-{digitos[4..]}";
        }

        /// <summary>
        /// Forma de almacenamiento de la placa: mayúsculas ASCII y solo caracteres
        /// alfanuméricos, sin guiones ni espacios. "abc-123" → "ABC123".
        /// Devuelve cadena vacía si el input es null/vacío o no contiene alfanuméricos.
        /// Solo conserva ASCII 0-9 / A-Z a propósito: char.IsLetter aceptaría letras
        /// Unicode que no corresponden a una placa costarricense.
        /// </summary>
        public static string NormalizarPlaca(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var buffer = new char[raw.Length];
            var largo = 0;

            foreach (var c in raw)
            {
                if (c is (>= '0' and <= '9') or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'))
                    buffer[largo++] = char.ToUpperInvariant(c);
            }

            return new string(buffer, 0, largo);
        }

        /// <summary>
        /// Formatea "ABC123" como "ABC-123" (3-3).
        /// Devuelve cadena vacía si el input es null/vacío.
        /// Si el largo normalizado no es el esperado, devuelve el input original sin
        /// modificar: preferimos mostrar el dato tal cual antes que imprimir guiones en
        /// posiciones que no corresponden. Misma regla que <see cref="CedulaNacional"/>.
        /// </summary>
        public static string Placa(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var normalizada = NormalizarPlaca(raw);
            if (normalizada.Length != LargoPlaca)
                return raw;

            return $"{normalizada[..3]}-{normalizada[3..]}";
        }
    }
}
