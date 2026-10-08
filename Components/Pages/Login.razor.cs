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

        protected LoginDTO loginModel { get; set; } = new();
        protected string? ErrorMessage { get; set; }
        protected bool isLoading { get; set; }

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
                    NavigationManager.NavigateTo("/", forceLoad: true);
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
        }
    }
}
