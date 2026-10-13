using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Microsoft.AspNetCore.Components;

namespace Mecano.Components.Pages.Citas;

public partial class CitaDetalle : ComponentBase
{
    [Parameter]
    public int Id { get; set; }

    private CitaDetalleDTO? cita;
    private bool cargando = true;

    // --- Edición (Fase 2: vehículo, servicio y notas) ---
    private bool mostrarModalEditar;
    private bool guardando;
    private string? mensajeError;
    private string? mensajeExito;
    private EditarCitaDTO modeloEditar = new();
    private List<Vehiculo> vehiculos = new();
    private List<ServicioDTO> servicios = new();

    protected override async Task OnParametersSetAsync()
    {
        cargando = true;
        mostrarModalEditar = false;
        mensajeError = null;
        mensajeExito = null;
        try
        {
            cita = await CitaService.ObtenerDetallePorIdAsync(Id);
        }
        finally
        {
            cargando = false;
        }
    }

    private static string BadgeEstado(EstadoCita estado) => estado switch
    {
        EstadoCita.Pendiente => "bg-secondary",
        EstadoCita.Confirmada => "bg-primary",
        EstadoCita.EnProceso => "bg-info text-dark",
        EstadoCita.Finalizada => "bg-success",
        EstadoCita.Cancelada => "bg-danger",
        _ => "bg-secondary"
    };

    private static bool EsEditable(EstadoCita estado) =>
        estado != EstadoCita.Cancelada && estado != EstadoCita.Finalizada;

    private async Task AbrirEditar()
    {
        if (cita is null)
            return;

        mensajeError = null;
        mensajeExito = null;
        modeloEditar = new EditarCitaDTO
        {
            Id = cita.Id,
            VehiculoId = cita.VehiculoId,
            ServicioId = cita.ServicioId,
            NotasAdmin = cita.NotasAdmin
        };

        // Vehículos activos del cliente; si el vehículo actual está inactivo, se conserva
        // como opción para no forzar un cambio que el usuario no pidió.
        vehiculos = await VehiculoService.ObtenerPorClienteAsync(cita.ClienteId);
        if (vehiculos.All(v => v.VehiculoId != cita.VehiculoId))
        {
            vehiculos.Insert(0, new Vehiculo
            {
                VehiculoId = cita.VehiculoId,
                ClienteId = cita.ClienteId,
                Placa = cita.VehiculoPlaca ?? string.Empty,
                Marca = cita.VehiculoMarca ?? string.Empty,
                Modelo = cita.VehiculoModelo ?? string.Empty,
                Anio = cita.VehiculoAnio,
                Activo = false
            });
        }

        // Servicios de la especialidad del mecánico; el servicio actual se conserva como opción
        // aunque no pertenezca a esa categoría (datos heredados).
        servicios = cita.MecanicoEspecialidadId > 0
            ? await ServicioService.ObtenerPorCategoriaAsync(cita.MecanicoEspecialidadId)
            : new List<ServicioDTO>();

        if (servicios.All(s => s.Id != cita.ServicioId))
        {
            servicios.Insert(0, new ServicioDTO
            {
                Id = cita.ServicioId,
                Nombre = cita.ServicioNombre ?? "Servicio actual",
                DuracionMinutos = cita.ServicioDuracionMinutos,
                Precio = cita.ServicioPrecio,
                CategoriaId = cita.MecanicoEspecialidadId > 0 ? cita.MecanicoEspecialidadId : null,
                Activo = false
            });
        }

        mostrarModalEditar = true;
    }

    private void CerrarEditar()
    {
        if (guardando)
            return;

        mostrarModalEditar = false;
        mensajeError = null;
    }

    private async Task GuardarEdicion()
    {
        if (cita is null)
            return;

        guardando = true;
        mensajeError = null;
        try
        {
            var actualizado = await CitaService.ActualizarCitaAsync(modeloEditar);
            if (!actualizado)
            {
                mensajeError = "La cita ya no existe.";
                return;
            }

            cita = await CitaService.ObtenerDetallePorIdAsync(Id);
            mostrarModalEditar = false;
            mensajeExito = "Cita actualizada correctamente.";
        }
        catch (Exception ex)
        {
            mensajeError = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }
}
