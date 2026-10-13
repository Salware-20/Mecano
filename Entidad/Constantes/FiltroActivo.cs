namespace Mecano.Entidad.Constantes
{
    /// <summary>
    /// Filtro de estado para los listados administrativos (Clientes / Mecánicos).
    /// Reemplaza el booleano "mostrar inactivos" por tres estados explícitos.
    /// </summary>
    public enum FiltroActivo
    {
        Activos = 1,
        Inactivos = 2,
        Todos = 3
    }
}
