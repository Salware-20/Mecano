namespace Mecano.Entidad.Clases
{
    /// <summary>
    /// Entidad de unión (N:M) entre <see cref="Cita"/> y <see cref="Mecanico"/>.
    /// Permite asignar dos o más mecánicos de la misma especialidad/servicio a una sola cita.
    /// </summary>
    public class CitaMecanico
    {
        public int CitaId { get; set; }
        public Cita Cita { get; set; } = null!;

        public int MecanicoId { get; set; }
        public Mecanico Mecanico { get; set; } = null!;
    }
}
