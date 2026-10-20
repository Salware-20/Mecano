using System;

namespace Mecano.Entidad.DTOs.Dashboard
{
    /// <summary>Resumen operativo de un día para el panel de control (KPIs).</summary>
    public class ResumenDiarioDTO
    {
        public DateTime Fecha { get; set; }
        public CitasHoyDTO CitasHoy { get; set; } = new();
        public TallerPersonalDTO TallerPersonal { get; set; } = new();
        public OcupacionDiariaDTO Ocupacion { get; set; } = new();
    }
}
