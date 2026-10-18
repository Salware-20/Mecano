using System.Globalization;
using Mecano.Entidad.Clases;

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
        public const int LargoDimexMin = 11;
        public const int LargoDimexMax = 12;
        public const int LargoTelefono = 8;
        public const int LargoPlaca = 6;

        // Formato fijo para toda la app: el precio es colón costarricense sin importar
        // el navegador o el servidor. A diferencia de es-CR de .NET (que da "₡35 000,00"
        // con espacio no-cortable U+00A0), aquí el separador de miles es punto y el
        // decimal es coma: "₡35.000" o "₡35.000,50".
        //
        // Se construye una sola vez a nivel de estático porque NumberFormatInfo es un
        // objeto mutable: clonarlo en cada llamada a Moneda() generaría basura (GC) sin
        // ninguna ganancia. Al ser readonly y no exponerse nunca, se usa solo como lectura.
        private static readonly NumberFormatInfo FormatoMonedaCR = CrearFormatoMonedaCR();

        private static NumberFormatInfo CrearFormatoMonedaCR()
        {
            var nfi = (NumberFormatInfo)new CultureInfo("es-CR").NumberFormat.Clone();
            nfi.CurrencyGroupSeparator = ".";
            nfi.NumberGroupSeparator = ".";
            nfi.CurrencyDecimalSeparator = ",";
            nfi.NumberDecimalSeparator = ",";
            nfi.CurrencySymbol = "₡";
            return nfi;
        }

        /// <summary>
        /// Formatea un monto como moneda costarricense con decimales dinámicos:
        /// 35000 → "₡35.000" (sin decimales si el monto es entero) y
        /// 35000.5 → "₡35.000,50". El formato es fijo (no depende de la cultura del
        /// circuito), para que el resultado sea idéntico en cualquier navegador o
        /// servidor. Uso exclusivamente de presentación: nunca para parsear ni persistir.
        /// </summary>
        public static string Moneda(decimal monto) =>
            monto.ToString(monto % 1 == 0 ? "C0" : "C2", FormatoMonedaCR);

        /// <summary>
        /// Presenta una duración en minutos de forma legible en español, con singular/plural
        /// correctos y omitiendo la parte cero: 60 → "1 hora", 120 → "2 horas",
        /// 90 → "1 hora 30 minutos", 45 → "45 minutos", 1 → "1 minuto",
        /// 0 o negativo → "0 minutos". Solo presentación.
        /// </summary>
        public static string Duracion(int minutos)
        {
            if (minutos <= 0)
                return "0 minutos";

            var horas = minutos / 60;
            var resto = minutos % 60;

            var textoHoras = horas == 1 ? "1 hora" : $"{horas} horas";
            var textoMinutos = resto == 1 ? "1 minuto" : $"{resto} minutos";

            if (horas > 0 && resto > 0)
                return $"{textoHoras} {textoMinutos}";

            return horas > 0 ? textoHoras : textoMinutos;
        }

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
        /// Normaliza y valida el documento de identidad según su tipo.
        /// Lanza <see cref="InvalidOperationException"/> si no cumple con las reglas del tipo.
        /// </summary>
        public static string NormalizarIdentificacion(string? raw, Mecano.Entidad.Constantes.TipoIdentificacion tipo)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("El número de identificación es obligatorio.");

            switch (tipo)
            {
                case Mecano.Entidad.Constantes.TipoIdentificacion.Nacional:
                    var digitosNac = SoloDigitos(raw);
                    if (digitosNac.Length != LargoCedulaNacional)
                        throw new InvalidOperationException($"La cédula nacional debe tener exactamente {LargoCedulaNacional} dígitos.");
                    return digitosNac;

                case Mecano.Entidad.Constantes.TipoIdentificacion.Dimex:
                    var digitosDimex = SoloDigitos(raw);
                    if (digitosDimex.Length < LargoDimexMin || digitosDimex.Length > LargoDimexMax)
                        throw new InvalidOperationException($"El DIMEX debe tener entre {LargoDimexMin} y {LargoDimexMax} dígitos.");
                    return digitosDimex;

                case Mecano.Entidad.Constantes.TipoIdentificacion.Pasaporte:
                    var pasaporte = raw.Trim();
                    if (pasaporte.Length == 0)
                        throw new InvalidOperationException("El pasaporte no puede estar vacío.");
                    if (pasaporte.Length > 20)
                        throw new InvalidOperationException("El pasaporte no puede exceder 20 caracteres.");
                    return pasaporte;

                default:
                    throw new InvalidOperationException("Tipo de identificación no válido.");
            }
        }

        /// <summary>
        /// Presenta la identificación de forma legible según su tipo.
        /// </summary>
        public static string FormatearIdentificacion(string? raw, Mecano.Entidad.Constantes.TipoIdentificacion tipo)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            return tipo switch
            {
                Mecano.Entidad.Constantes.TipoIdentificacion.Nacional => CedulaNacional(raw),
                _ => raw.Trim()
            };
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

        /// <summary>
        /// Nombre legible de un estado de cita para la UI. El identificador del enum no
        /// admite espacios (EnProceso), así que el "En Proceso" es solo de presentación.
        /// </summary>
        public static string NombreEstado(EstadoCita estado) => estado switch
        {
            EstadoCita.EnProceso => "En Proceso",
            _ => estado.ToString()
        };

        /// <summary>
        /// Clases de color de Bootstrap para la insignia de un estado de cita. Centralizado
        /// aquí para que las pantallas no dupliquen el mapeo estado → color.
        /// </summary>
        public static string BadgeEstado(EstadoCita estado) => estado switch
        {
            EstadoCita.Pendiente => "bg-secondary",
            EstadoCita.Confirmado => "bg-primary",
            EstadoCita.EnProceso => "bg-info text-dark",
            EstadoCita.Finalizado => "bg-success",
            EstadoCita.Cancelado => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
