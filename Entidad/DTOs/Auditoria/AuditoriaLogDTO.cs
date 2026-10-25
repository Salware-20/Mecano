using System;

namespace Mecano.Entidad.DTOs.Auditoria
{
    /// <summary>Read model de un registro de auditoría para el visor /auditoria.</summary>
    public class AuditoriaLogDTO
    {
        public long     Id              { get; set; }
        public DateTime FechaHora       { get; set; }
        public string   TipoAccion      { get; set; } = string.Empty;
        public string?  UsuarioNombre   { get; set; }
        public string   EntidadAfectada { get; set; } = string.Empty;
        public string?  EntidadId       { get; set; }
        public string?  Detalles        { get; set; }
    }
}
