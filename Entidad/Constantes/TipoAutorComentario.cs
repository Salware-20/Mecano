namespace Mecano.Entidad.Constantes
{
    /// <summary>
    /// Quién puede redactar un comentario. El cliente NO es autor:
    /// solo el personal logueado (Administrador o Mecánico).
    /// </summary>
    public enum TipoAutorComentario
    {
        Mecanico = 1,
        Administrador = 2
    }
}
