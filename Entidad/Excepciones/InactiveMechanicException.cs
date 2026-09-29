using System;

namespace Mecano.Entidad.Excepciones
{
    public sealed class InactiveMechanicException : Exception
    {
        public InactiveMechanicException()
            : base("El mecánico seleccionado no está activo.") { }

        public InactiveMechanicException(string message) : base(message) { }

        public InactiveMechanicException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
