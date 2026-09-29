using System;

namespace Mecano.Entidad.DTOs
{
    public sealed class CalendarEventDTO
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime Start { get; init; }
        public DateTime End { get; init; }
        public string Color { get; init; } = "#3788d8";
        public bool AllDay { get; init; }
    }
}
