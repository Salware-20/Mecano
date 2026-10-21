using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mecano.Components.Pages
{
    public partial class Login : ComponentBase
    {
        [Inject]
        public IJSRuntime JS { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        /// <summary>
        /// Ruta a la que volver tras iniciar sesión. La aporta el middleware de cookies
        /// (?ReturnUrl=...) cuando se intenta acceder a una página protegida sin sesión.
        /// Blazor la enlaza desde la query string y ya viene decodificada.
        /// </summary>
        [SupplyParameterFromQuery(Name = "ReturnUrl")]
        public string? ReturnUrl { get; set; }

        protected LoginDTO loginModel { get; set; } = new();
        protected string? ErrorMessage { get; set; }
        protected bool isLoading { get; set; }

        /// <summary>
        /// Controla si la contraseña se muestra en claro. El campo nunca usa type="password"
        /// (para que el navegador no ofrezca guardarla en equipos compartidos); el ocultado
        /// se logra con -webkit-text-security.
        /// </summary>
        protected bool mostrarPassword { get; set; } = false;

        private void ToggleMostrarPassword() => mostrarPassword = !mostrarPassword;

        /// <summary>
        /// Solo aceptamos rutas locales relativas ("/clientes", "/citas/5?x=1") para evitar
        /// open redirects: rechazamos absolutas ("https://evil.com"), protocol-relative
        /// ("//evil.com") y el truco de barra invertida ("/\evil.com", que algunos
        /// navegadores normalizan a "//evil.com").
        /// </summary>
        private static bool EsRutaLocal(string? url) =>
            !string.IsNullOrWhiteSpace(url)
            && url[0] == '/'
            && (url.Length < 2 || (url[1] != '/' && url[1] != '\\'));

        protected async Task HandleLogin()
        {
            isLoading = true;
            ErrorMessage = null;

            try
            {
                // CRÍTICO: La petición se realiza mediante JS interop (fetch) con credentials: 'same-origin'
                // para asegurar que la cookie 'Set-Cookie' enviada por el servidor sea recibida y guardada por el navegador.
                // Si usáramos HttpClient inyectado en Blazor Server, la petición saldría del servidor a sí mismo
                // y el navegador del cliente nunca recibiría la cookie de autenticación.
                var result = await JS.InvokeAsync<FetchResult>("window.authFetch.login", "/api/auth/login", loginModel);

                if (result.Ok)
                {
                    // Forzar recarga completa (forceLoad: true) para que el circuito Blazor se inicialice
                    // con la nueva cookie de autenticación ya presente en el navegador.
                    // Destino: ReturnUrl si es una ruta local válida; si no, el panel del rol
                    // (el mecánico entra a /mecanico y el administrador al dashboard "/").
                    string destino;
                    if (EsRutaLocal(ReturnUrl))
                        destino = ReturnUrl!;
                    else if (result.Rol == Roles.Mecanico)
                        destino = "/mecanico";
                    else
                        destino = "/";

                    NavigationManager.NavigateTo(destino, forceLoad: true);
                }
                else
                {
                    // Mensaje genérico para no filtrar si el usuario existe o no
                    ErrorMessage = "Credenciales incorrectas o usuario inactivo.";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Ocurrió un error inesperado al intentar iniciar sesión.";
            }
            finally
            {
                isLoading = false;
            }
        }

        public class FetchResult
        {
            public bool Ok { get; set; }
            public int Status { get; set; }

            /// <summary>
            /// Rol devuelto por /api/auth/login (Administrador | Mecanico). Null si el
            /// servidor no lo envió; el redirect por defecto entonces es el dashboard.
            /// </summary>
            public string? Rol { get; set; }
        }
    }
}
