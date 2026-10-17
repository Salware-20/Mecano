using Mecano.Entidad.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Mecano.Components.Shared;

/// <summary>
/// Modal de crear/editar vehículo compartido entre <c>ClienteDetalle</c> y <c>VehiculoDetalle</c>.
/// El dueño es responsable del estado y de las acciones (guardar/cancelar, eliminar, transferir);
/// este componente solo dibuja el formulario y reemite el submit.
/// </summary>
public partial class VehiculoFormModal : ComponentBase
{
    /// <summary>Modelo enlazado a los campos. El dueño lo conserva y lo lee al guardar.</summary>
    [Parameter, EditorRequired]
    public NuevoVehiculoDTO Model { get; set; } = default!;

    /// <summary>true = "Editar vehículo"; false = "Nuevo vehículo".</summary>
    [Parameter]
    public bool EsEdicion { get; set; }

    [Parameter]
    public bool Guardando { get; set; }

    [Parameter]
    public string? MensajeError { get; set; }

    /// <summary>Se invoca cuando el formulario es válido y se envía.</summary>
    [Parameter]
    public EventCallback OnGuardar { get; set; }

    [Parameter]
    public EventCallback OnCancelar { get; set; }

    /// <summary>Ranura opcional a la izquierda del pie (p. ej. Eliminar/Transferir en ClienteDetalle).</summary>
    [Parameter]
    public RenderFragment? FooterIzquierdo { get; set; }

    [Parameter]
    public string FormName { get; set; } = "vehiculoForm";

    // EditForm.OnValidSubmit entrega un EditContext; el dueño no lo necesita,
    // así que lo ignoramos y reemitimos la llamada simple.
    private async Task Submit(EditContext _) => await OnGuardar.InvokeAsync();
}
