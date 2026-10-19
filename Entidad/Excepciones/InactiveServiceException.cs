using System;

namespace Mecano.Entidad.Excepciones
{
    public sealed class InactiveServiceException : Exception
    {
        public InactiveServiceException()
            : base("El servicio seleccionado no está activo.") { }

        public InactiveServiceException(string message) : base(message) { }

        public InactiveServiceException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
