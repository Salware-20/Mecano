using Mecano.Entidad.Clases;

namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>Conteo de citas de un día para un estado dado (p. ej. 3 pendientes).</summary>
    public class EstadoConteoDTO
    {
        public EstadoCita Estado { get; set; }
        public int Conteo { get; set; }
    }
}
