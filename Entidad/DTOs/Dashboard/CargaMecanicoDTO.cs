namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>Carga de trabajo de un mecánico activo en un período (para la gráfica de barras).</summary>
    public class CargaMecanicoDTO
    {
        public int MecanicoId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Citas no canceladas del período en las que participa este mecánico.</summary>
        public int Citas { get; set; }

        /// <summary>
        /// Suma de las duraciones de esas citas (minutos de taller asignados). Es el
        /// valor que grafica la barra; la presentación «1 h 30 min» (eje) y «1 hora 30
        /// minutos» (tooltip) la aplican los formatters del chart, espejo JS de
        /// <c>Formatos.Duracion</c>.
        /// </summary>
        public int Minutos { get; set; }
    }
}
