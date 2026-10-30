using System;

namespace Mecano.Entidad.Utilidades
{
    /// <summary>
    /// Regla #5 del negocio: cancelar una cita con menos de 24 horas de antelación
    /// es una cancelación tardía y debe avisarse en la UI y quedar registrada aparte
    /// en la auditoría. Vive en <c>Entidad</c> para que la UI (Blazor) y el servicio
    /// compartan el mismo umbral y la misma aritmética.
    /// </summary>
    public static class ReglasCita
    {
        /// <summary>
        /// Horas mínimas de antelación para que una cancelación no sea tardía.
        /// El límite es estricto: exactamente 24 h todavía no es tardía.
        /// </summary>
        public const double HorasAvisoCancelacion = 24;

        /// <summary>
        /// Instante programado de la cita. La entidad guarda la fecha y la hora por
        /// separado (<see cref="DateTime"/> + <see cref="TimeOnly"/>); aquí se combinan
        /// en un único <see cref="DateTime"/> local, que es el que comparan la UI y el
        /// servicio.
        /// </summary>
        public static DateTime InicioProgramado(DateTime fecha, TimeOnly horaInicio) =>
            fecha.Date + horaInicio.ToTimeSpan();

        /// <summary>
        /// Indica si cancelar en <paramref name="ahora"/> cae dentro de la ventana de
        /// 24 horas previas al inicio programado. Las citas ya vencidas (diferencia
        /// negativa) también cuentan como tardías; el límite exacto de 24 h no.
        /// </summary>
        public static bool EsCancelacionTardia(DateTime inicioProgramado, DateTime ahora) =>
            (inicioProgramado - ahora).TotalHours < HorasAvisoCancelacion;
    }
}
