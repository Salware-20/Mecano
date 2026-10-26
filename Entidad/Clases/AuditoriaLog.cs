using System;
using System.ComponentModel.DataAnnotations;

namespace Mecano.Entidad.Clases
{
    public class AuditoriaLog
    {
        public long Id { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string TipoAccion { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? UsuarioId { get; set; }

        [MaxLength(200)]
        public string? UsuarioNombre { get; set; }

        [MaxLength(100)]
        public string EntidadAfectada { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntidadId { get; set; }

        [MaxLength(2000)]
        public string? Detalles { get; set; }
    }
}
