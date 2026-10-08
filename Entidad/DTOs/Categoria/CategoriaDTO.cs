namespace Mecano.Entidad.DTOs.Categoria
{
    public class CategoriaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int CantidadServicios { get; set; }
        public int CantidadMecanicos { get; set; }
    }
}
