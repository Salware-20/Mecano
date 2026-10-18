using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.Clases
{
    public enum EstadoCita
    {
        Pendiente,
        Confirmado,
        EnProceso,
        Finalizado,
        Cancelado
    }

    public class Cita
    {
        public int CitaId { get; set; }
        
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
        
        public int VehiculoId { get; set; }
        public Vehiculo Vehiculo { get; set; } = null!;
        
        public int ServicioId { get; set; }
        public Servicio Servicio { get; set; } = null!;

        // Relación N:M: una cita puede tener varios mecánicos de la misma especialidad.
        public ICollection<CitaMecanico> CitaMecanicos { get; set; } = new List<CitaMecanico>();

        public DateTime Fecha { get; set; }
        public TimeOnly HoraInicio { get; set; }
        public TimeOnly HoraFin { get; set; }
        
        public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
        [MaxLength(1000)]
        public string? NotasAdmin { get; set; }
        
        public ICollection<NotificacionLog> Notificaciones { get; } = new List<NotificacionLog>();
        
        public bool OverlapsWith(TimeOnly otherStart, TimeOnly otherEnd) => HoraInicio < otherEnd && otherStart < HoraFin;
        
        public bool EstaActiva() => Estado != EstadoCita.Cancelado && Estado != EstadoCita.Finalizado;
    }
}
