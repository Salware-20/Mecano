using System;
using System.Collections.Generic;
using Mecano.Entidad.Clases;
using MecanicoClase = Mecano.Entidad.Clases.Mecanico;

namespace Mecano.Entidad.DTOs
{
    public sealed class HorarioDisponibleDTO
    {
        public TimeOnly HoraInicio { get; init; }
        public TimeOnly HoraFin { get; init; }
        public bool EstaOcupado { get; init; }
        public List<MecanicoClase> MecanicosLibres { get; init; } = new();
    }
}
