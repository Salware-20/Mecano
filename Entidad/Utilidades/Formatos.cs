namespace Mecano.Entidad.Utilidades
{
    /// <summary>
    /// Normalización y formateo de identificadores numéricos de Cliente.
    ///
    /// Invariante de almacenamiento: Cliente.CedulaIdentidad y Cliente.Telefono se guardan
    /// únicamente con dígitos, sin guiones ni espacios. El formato legible es cosa de la capa
    /// de presentación, nunca de la de persistencia.
    ///
    /// Este es el motivo de la normalización en ClienteService: el índice único es
    /// (CedulaIdentidad, TipoIdentificacion) y compara cadenas crudas, así que "1-0111-0111"
    /// y "101110111" pasarían como dos clientes distintos si se guardaran tal cual.
    /// </summary>
    public static class Formatos
    {
        public const int LargoCedulaNacional = 9;
        public const int LargoTelefono = 8;

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
    }
}
