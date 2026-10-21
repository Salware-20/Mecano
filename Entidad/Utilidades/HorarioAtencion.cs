using System;

namespace Mecano.Entidad.Utilidades
{
    /// <summary>
    /// Horario de atención del taller, centralizado. El 08:00 / 18:00 estaba
    /// escrito a mano en dos lugares de CitaService; el panel de control también
    /// lo necesita para calcular la ocupación diaria, así que vive en un solo sitio.
    /// </summary>
    public static class HorarioAtencion
    {
        public static TimeOnly Apertura { get; } = new TimeOnly(8, 0);
        public static TimeOnly Cierre { get; } = new TimeOnly(18, 0);

        /// <summary>Minutos disponibles por mecánico en un día laborable (600 = 10 h).</summary>
        public static int MinutosPorDia => (int)(Cierre - Apertura).TotalMinutes;

        /// <summary>El taller no atiende los domingos.</summary>
        public static bool EsDiaNoLaboral(DateTime fecha) => fecha.DayOfWeek == DayOfWeek.Sunday;
    }
}
