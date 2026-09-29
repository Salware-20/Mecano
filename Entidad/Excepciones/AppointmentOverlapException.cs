using System;

namespace Mecano.Entidad.Excepciones
{
    public sealed class AppointmentOverlapException : Exception
    {
        public AppointmentOverlapException()
            : base("El mecánico ya tiene una cita asignada en ese horario.") { }

        public AppointmentOverlapException(string message) : base(message) { }

        public AppointmentOverlapException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
