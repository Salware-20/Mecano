using Mecano.Entidad.Constantes;

namespace Mecano.Entidad.DTOs.Administrador
{
    public class AdministradorListaDTO
    {
        public int AdministradorId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public TipoIdentificacion TipoIdentificacion { get; set; } = TipoIdentificacion.Nacional;
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
        public bool EsAdminGlobal { get; set; }
    }
}
