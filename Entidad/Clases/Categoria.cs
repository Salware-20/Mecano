namespace Mecano.Entidad.Clases
{
    public class Categoria
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public ICollection<Servicio> servicios { get; } = new List<Servicio>();
    }
}
