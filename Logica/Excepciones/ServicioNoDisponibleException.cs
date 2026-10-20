namespace Mecano.Logica.Excepciones;

public class ServicioNoDisponibleException : Exception
{
    public ServicioNoDisponibleException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}