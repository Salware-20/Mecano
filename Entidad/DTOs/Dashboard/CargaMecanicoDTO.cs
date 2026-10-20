namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>Carga de trabajo de un mecánico activo en un período (para la gráfica de barras).</summary>
    public class CargaMecanicoDTO
    {
        public int MecanicoId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Citas no canceladas del período en las que participa este mecánico.</summary>
        public int Citas { get; set; }

        /// <summary>Suma de las duraciones de esas citas (minutos de taller asignados).</summary>
        public int Minutos { get; set; }

        /// <summary>Minutos convertidos a horas con un decimal, listo para graficar.</summary>
        public decimal Horas => Minutos / 60m;
    }
}
