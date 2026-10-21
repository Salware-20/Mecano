using System;
using System.Collections.Generic;
using Mecano.Entidad.Clases;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Proyección de listado de citas para el mecánico (/mecanico/citas). A propósito
    /// no es <see cref="CitaDTO"/>: esa expone el PII de cada colega de la cita
    /// (cédula, teléfono, correo) vía <c>MecanicoDTO</c>, y el mecánico solo debe ver
    /// el nombre de sus compañeros, nunca sus datos de contacto. Tampoco trae precio
    /// del servicio ni notas de administración.
    /// </summary>
    public class CitaListaMecanicoDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public EstadoCita Estado { get; set; }

        // El cliente de la cita: solo el nombre (el mecánico nunca ve cédula,
        // teléfono ni correo del cliente).
        public int ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public bool ClienteActivo { get; set; }

        // Vehículo de la cita: datos de trabajo, sin PII.
        public int VehiculoId { get; set; }
        public string? VehiculoPlaca { get; set; }
        public string? VehiculoMarca { get; set; }
        public string? VehiculoModelo { get; set; }
        public int VehiculoAnio { get; set; }

        // Servicio: nombre y duración para la agenda; sin precio (es información
        // comercial de administración).
        public int ServicioId { get; set; }
        public string? ServicioNombre { get; set; }
        public int ServicioDuracionMinutos { get; set; }
        public bool ServicioActivo { get; set; }

        /// <summary>
        /// Mecánicos asignados a la cita, solo con nombre: los colegas como compañeros
        /// de trabajo, sin exponer sus datos personales en el listado.
        /// </summary>
        public List<string> MecanicosNombres { get; set; } = new();
    }
}
