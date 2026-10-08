namespace Mecano.Entidad.DTOs
{
    public record AuthenticatedUser(
        int id,
        string email,
        string nombre,
        string rol, // Mecanico o Administrador
        bool esAdminGlobal = false
    )
    {
        public int Id => id;
        public string Email => email;
        public string Nombre => nombre;
        public string Rol => rol;
        public bool EsAdminGlobal => esAdminGlobal;
    }
}
