using System.Security.Claims;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs.Mecanico;
using Mecano.Entidad.DTOs.Perfil;
using Mecano.Entidad.Utilidades;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Mecano.Components.Pages
{
    public partial class Perfil : ComponentBase
    {
        [CascadingParameter]
        private Task<AuthenticationState>? AuthState { get; set; }

        private PerfilAdminDTO? perfil;
        private ActualizarPerfilAdminDTO perfilModel = new();
        private CambiarPasswordDTO passwordModel = new();

        // --- Vista del mecánico: su ficha (solo lectura) ---
        private MecanicoDTO? perfilMec;

        private int adminId;
        private int mecanicoId;
        private bool esAdmin;
        private bool esAdminGlobal;

        private bool cargando = true;
        private bool guardandoPerfil;
        private bool guardandoPassword;

        private string? mensajeExitoPerfil;
        private string? mensajeErrorPerfil;
        private string? mensajeExitoPassword;
        private string? mensajeErrorPassword;

        // Los campos de contraseña nunca usan type="password" (equipos compartidos del
        // taller): el ocultado se logra con -webkit-text-security y cada campo tiene su
        // propio interruptor de visibilidad.
        private bool mostrarPasswordActual;
        private bool mostrarPasswordNueva;
        private bool mostrarPasswordConfirmar;

        protected override async Task OnInitializedAsync()
        {
            if (AuthState is not null)
            {
                var state = await AuthState;
                // Bifurcación por rol desde el claim de sesión; por defecto rama
                // restringida (esAdmin = false) por si el estado no llegara.
                esAdmin = state.User.IsInRole(Roles.Administrador);
                adminId = int.TryParse(
                    state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
                esAdminGlobal = state.User.FindFirst("EsAdminGlobal")?.Value == "true";

                // En el rol Mecánico, el NameIdentifier ES el MecanicoId (así lo emite
                // el login): el mismo claim sirve para identificar la cuenta.
                mecanicoId = esAdmin ? 0 : adminId;
            }

            await CargarPerfil();
        }

        private async Task CargarPerfil()
        {
            cargando = true;
            try
            {
                if (esAdmin)
                {
                    perfil = await AdministradorService.ObtenerPerfilAsync(adminId);
                    if (perfil is not null)
                    {
                        perfilModel = new ActualizarPerfilAdminDTO
                        {
                            Nombre               = perfil.Nombre,
                            Cedula               = perfil.Cedula,
                            TipoIdentificacion   = perfil.TipoIdentificacion,
                            CorreoNotificaciones = perfil.CorreoNotificaciones
                        };
                    }
                }
                else
                {
                    perfilMec = mecanicoId == 0
                        ? null
                        : await MecanicoService.ObtenerDetallePorIdAsync(mecanicoId);
                }
            }
            finally
            {
                cargando = false;
            }
        }

        // ~~~ Presentación ~~~

        /// <summary>Nombre visible según el rol (admin DTO o ficha del mecánico).</summary>
        private string? NombreCuenta => esAdmin ? perfil?.Nombre : perfilMec?.Nombre;

        /// <summary>Iniciales del nombre para el avatar circular (máx. 2 letras).</summary>
        private string Iniciales()
        {
            if (string.IsNullOrWhiteSpace(NombreCuenta))
                return "?";

            var partes = NombreCuenta.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length == 1
                ? partes[0][..1].ToUpperInvariant()
                : $"{partes[0][..1]}{partes[^1][..1]}".ToUpperInvariant();
        }

        private string EtiquetaRol()
        {
            if (!esAdmin)
                return "Mecánico";

            return esAdminGlobal ? "Administrador Global" : "Administrador";
        }

        private void TogglePasswordActual() => mostrarPasswordActual = !mostrarPasswordActual;
        private void TogglePasswordNueva() => mostrarPasswordNueva = !mostrarPasswordNueva;
        private void TogglePasswordConfirmar() => mostrarPasswordConfirmar = !mostrarPasswordConfirmar;

        // ~~~ Guardar datos del perfil ~~~

        private async Task GuardarPerfil()
        {
            // La edición del perfil es exclusiva del administrador (la ficha del
            // mecánico es de solo lectura); guardia por si se invocara de otro modo.
            if (!esAdmin)
                return;

            guardandoPerfil = true;
            mensajeExitoPerfil = null;
            mensajeErrorPerfil = null;

            try
            {
                if (await AdministradorService.ActualizarPerfilAsync(adminId, perfilModel))
                {
                    // Se sincroniza el modelo en memoria en lugar de recargar el
                    // perfil: CargarPerfil() activa el spinner y desmontaría ambas
                    // tarjetas (parpadeo y pérdida del foco) tras cada guardado.
                    // El servicio ya validó la identificación, así que normalizarla
                    // aquí con el mismo helper no puede lanzar.
                    var nombre = perfilModel.Nombre.Trim();
                    var cedula = string.IsNullOrWhiteSpace(perfilModel.Cedula)
                        ? null
                        : Formatos.NormalizarIdentificacion(perfilModel.Cedula, perfilModel.TipoIdentificacion);
                    var correo = string.IsNullOrWhiteSpace(perfilModel.CorreoNotificaciones)
                        ? null
                        : perfilModel.CorreoNotificaciones.Trim();

                    perfilModel.Nombre               = nombre;
                    perfilModel.Cedula               = cedula;
                    perfilModel.CorreoNotificaciones = correo;

                    if (perfil is not null)
                    {
                        perfil.Nombre               = nombre;
                        perfil.Cedula               = cedula;
                        perfil.TipoIdentificacion   = perfilModel.TipoIdentificacion;
                        perfil.CorreoNotificaciones = correo;
                    }

                    mensajeExitoPerfil = "Datos del perfil actualizados correctamente.";
                }
                else
                {
                    mensajeErrorPerfil = "Tu cuenta ya no está disponible. Vuelve a iniciar sesión.";
                }
            }
            catch (InvalidOperationException ex)
            {
                mensajeErrorPerfil = ex.Message;
            }
            finally
            {
                guardandoPerfil = false;
            }
        }

        // ~~~ Cambiar contraseña ~~~

        private async Task GuardarPassword()
        {
            guardandoPassword = true;
            mensajeExitoPassword = null;
            mensajeErrorPassword = null;

            try
            {
                // Mismo DTO de cambio de contraseña en ambos roles, pero con el
                // servicio que corresponde: el mecánico no toca la tabla de cuentas.
                var guardado = esAdmin
                    ? await AdministradorService.CambiarPasswordAsync(adminId, passwordModel)
                    : await MecanicoService.CambiarPasswordAsync(mecanicoId, passwordModel);

                if (guardado)
                {
                    mensajeExitoPassword = "Contraseña actualizada correctamente.";
                    passwordModel = new CambiarPasswordDTO();
                    mostrarPasswordActual = mostrarPasswordNueva = mostrarPasswordConfirmar = false;
                }
                else
                {
                    mensajeErrorPassword = "Tu cuenta ya no está disponible. Vuelve a iniciar sesión.";
                }
            }
            catch (InvalidOperationException ex)
            {
                mensajeErrorPassword = ex.Message;
            }
            finally
            {
                guardandoPassword = false;
            }
        }
    }
}
