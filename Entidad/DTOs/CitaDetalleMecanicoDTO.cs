using System;
using System.Collections.Generic;
using Mecano.Entidad.Clases;

namespace Mecano.Entidad.DTOs
{
    /// <summary>
    /// Proyección de detalle de una cita para el mecánico (/citas/{id} con rol Mecanico).
    /// Es la contraparte restringida de <see cref="CitaDetalleDTO"/>: no lleva el precio
    /// del servicio ni <c>NotasAdmin</c> (información comercial y administrativa), y del
    /// cliente solo el nombre y el estado activo — nunca cédula, teléfono ni correo. El
    /// vehículo sí trae kilometraje y notas: son datos de trabajo del taller.
    /// </summary>
    public class CitaDetalleMecanicoDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        public EstadoCita Estado { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Cliente: solo nombre y estado (sin PII de contacto).
        public int ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public bool ClienteActivo { get; set; }

        // Vehículo: datos de trabajo del taller, incluidos kilometraje y notas.
        public int VehiculoId { get; set; }
        public string? VehiculoPlaca { get; set; }
        public string? VehiculoMarca { get; set; }
        public string? VehiculoModelo { get; set; }
        public int VehiculoAnio { get; set; }
        public int? VehiculoKilometraje { get; set; }
        public string? VehiculoNotas { get; set; }

        // Servicio: nombre y duración; sin precio.
        public int ServicioId { get; set; }
        public string? ServicioNombre { get; set; }
        public int ServicioDuracionMinutos { get; set; }
        public bool ServicioActivo { get; set; }

        /// <summary>Mecánicos asignados a la cita, solo con nombre (colegas como compañeros).</summary>
        public List<string> MecanicosNombres { get; set; } = new();
    }
}
