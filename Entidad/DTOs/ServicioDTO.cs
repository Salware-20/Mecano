namespace Mecano.Entidad.DTOs
{
    public class ServicioDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; }
        public int? CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
    }
}
