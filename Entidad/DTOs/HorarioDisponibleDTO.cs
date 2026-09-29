using System;
using System.Collections.Generic;
using Mecano.Entidad.Clases;

namespace Mecano.Entidad.DTOs
{
    public sealed class HorarioDisponibleDTO
    {
        public TimeOnly HoraInicio { get; init; }
        public TimeOnly HoraFin { get; init; }
        public bool EstaOcupado { get; init; }
        public List<Mecanico> MecanicosLibres { get; init; } = new();
    }
}
