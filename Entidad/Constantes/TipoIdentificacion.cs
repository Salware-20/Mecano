namespace Mecano.Entidad.Constantes
{
    /// <summary>
    /// Tipo de documento de identidad que usa un cliente para registrarse.
    /// Fase 1: la UI y las validaciones solo exponen <see cref="Nacional"/>.
    /// Los demás valores quedan preparados en el esquema para la Fase 2.
    /// </summary>
    public enum TipoIdentificacion
    {
        Nacional = 1,
        Dimex = 2,
        Pasaporte = 3
    }
}
