namespace Mecano.Entidad.Utilidades
{
    /// <summary>
    /// Patrones de validación de entrada compartidos por los DTOs de escritura
    /// (clientes y mecánicos). Se declaran como <c>const</c> para poder usarlos
    /// directamente en los atributos de DataAnnotations y así evitar que las reglas
    /// se dupliquen y diverjan entre los DTOs de crear y actualizar.
    /// </summary>
    public static class Patrones
    {
        /// <summary>
        /// Nombre de persona: solo letras (cualquier alfabeto, incluidas tildes y ñ),
        /// espacios, apóstrofos y guiones. Ej.: "José O'Neil", "María José López-Rojas".
        /// </summary>
        public const string Nombre = @"^[\p{L} '\-]+$";

        /// <summary>
        /// Correo: usuario@dominio.tld, exigiendo al menos un punto y una extensión
        /// alfabética de 2+ letras (.com, .co.cr, ...). Es más estricto que
        /// <c>EmailAddressAttribute</c>, que acepta dominios sin punto.
        /// </summary>
        public const string Correo = @"^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$";

        /// <summary>
        /// Alias de correo corporativo: minúsculas ASCII, números y separadores
        /// simples (punto, guion, guion bajo), sin <c>@</c>, espacios ni dominio.
        /// El servicio compone <c>alias@mecano.cr</c> y revalida esta regla.
        /// </summary>
        public const string AliasCorreo = @"^[a-z0-9]+(?:[._-][a-z0-9]+)*$";

        /// <summary>
        /// Mensaje de error reutilizable para <see cref="Nombre"/> y <see cref="Correo"/>,
        /// de modo que el texto mostrado sea idéntico en todos los formularios.
        /// </summary>
        public const string MensajeNombre = "El nombre solo puede contener letras, espacios, apóstrofos y guiones";

        /// <summary>Mensaje de error reutilizable para <see cref="Correo"/>.</summary>
        public const string MensajeCorreo = "El correo debe tener un formato válido (ej. usuario@dominio.com)";

        /// <summary>Mensaje de error para el alias del correo corporativo.</summary>
        public const string MensajeAliasCorreo = "El alias solo admite minúsculas, números, puntos, guiones y guiones bajos; sin espacios, @ ni dominio.";

        /// <summary>Mensaje de error reutilizable para el teléfono con formato inválido.</summary>
        public const string MensajeTelefonoFormato = "El teléfono no tiene un formato válido";

        /// <summary>Mensaje de error reutilizable para el teléfono obligatorio.</summary>
        public const string MensajeTelefonoRequerido = "Necesitamos un numero de contacto";
    }
}
