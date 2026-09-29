using System;

namespace Mecano.Entidad.DTOs
{
    public sealed class AgendarCitaDTO
    {
        public int ClienteId { get; set; }
        public int? VehiculoId { get; set; }
        public int ServicioId { get; set; }
        public int MecanicoId { get; set; }
        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public NuevoVehiculoDTO? NuevoVehiculo { get; set; }
    }
}
