namespace Mecano.Entidad.DTOs.Mecanico
{
    /// <summary>
    /// DTO de lectura para un mecánico. Incluye el nombre de la especialidad
    /// resuelto desde la navegación Categoria.
    /// </summary>
    public class MecanicoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public int EspecialidadId { get; set; }
        public string? EspecialidadNombre { get; set; }
        public bool Activo { get; set; }
    }
}
