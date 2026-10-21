namespace Mecano.Components.Shared
{
    /// <summary>
    /// Estado del tema (claro/oscuro) compartido en el circuito. El cambio real lo aplica
    /// window.mecanoTheme (data-bs-theme en &lt;html&gt;); este servicio solo lo refleja en C#
    /// para que los componentes que viven aparte del árbol de renderización —como los gráficos
    /// ApexCharts del panel— puedan re-renderizarse cuando el usuario cambia el tema.
    /// </summary>
    public class ThemeState
    {
        public bool IsDark { get; private set; }

        public event Action? Changed;

        public void Set(bool isDark)
        {
            if (IsDark == isDark) return;
            IsDark = isDark;
            Changed?.Invoke();
        }
    }
}
