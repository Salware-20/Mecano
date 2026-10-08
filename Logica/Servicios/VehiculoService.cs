using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Entidad.Utilidades;
using Mecano.Logica.Interfaces;

namespace Mecano.Logica.Servicios;

public class VehiculoService : IVehiculoService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<VehiculoService> _logger;

    public VehiculoService(IDbContextFactory<MySQLDBContext> factory, ILogger<VehiculoService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    // ~~~ READ (por cliente) ~~~
    public async Task<List<Vehiculo>> ObtenerPorClienteAsync(int clienteId, bool incluirInactivos = false)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Por defecto solo activos; ClienteDetalle pasa incluirInactivos = true cuando el cliente está inactivo
        // para que el administrador pueda ver/transferir los vehículos que quedaron en cascada.
        var query = context.Vehiculo.Where(v => v.ClienteId == clienteId);
        if (!incluirInactivos)
            query = query.Where(v => v.Activo);

        return await query.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Normaliza y valida los campos con invariante antes de persistir. Se ejecuta antes
    /// de la comprobación de unicidad: el índice único de Placa compara cadenas crudas,
    /// así que sin normalizar "abc-123" y "ABC123" entrarían como dos vehículos distintos.
    /// </summary>
    private static (string Placa, int Anio) NormalizarYValidar(string placaCruda, int anio)
    {
        var placa = Formatos.NormalizarPlaca(placaCruda);

        if (placa.Length != Formatos.LargoPlaca)
        {
            throw new InvalidOperationException(
                $"La placa debe tener exactamente {Formatos.LargoPlaca} caracteres alfanuméricos.");
        }

        if (anio is < 1900 or > 2100)
        {
            throw new InvalidOperationException("El año debe estar entre 1900 y 2100.");
        }

        return (placa, anio);
    }

    // ~~~ CREATE ~~~
    public async Task<int> CrearAsync(NuevoVehiculoDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Normalizar y validar ANTES de tocar la base: el chequeo de duplicado y el
        // guardado deben operar sobre la misma forma del valor que realmente se persiste.
        var (placa, anio) = NormalizarYValidar(dto.Placa, dto.Anio);

        // El índice único es global (incluye vehículos desactivados), así que el
        // chequeo también lo es: si no, el SaveChanges explotaría en DbUpdateException.
        if (await context.Vehiculo.AnyAsync(v => v.Placa == placa))
        {
            _logger.LogWarning("Placa duplicada al crear vehículo: {Placa}", placa);
            throw new InvalidOperationException(
                $"Ya existe un vehículo con la placa {Formatos.Placa(placa)}");
        }

        var nuevo = new Vehiculo
        {
            ClienteId = dto.ClienteId,
            Placa = placa,
            Marca = dto.Marca.Trim(),
            Modelo = dto.Modelo.Trim(),
            Anio = anio,
            Activo = true
        };

        context.Vehiculo.Add(nuevo);
        await context.SaveChangesAsync();
        return nuevo.VehiculoId;
    }

    // ~~~ UPDATE ~~~
    public async Task<bool> ActualizarAsync(ActualizarVehiculoDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var vehiculo = await context.Vehiculo.FindAsync(dto.Id);
        if (vehiculo is null) return false;

        var (placa, anio) = NormalizarYValidar(dto.Placa, dto.Anio);

        if (await context.Vehiculo.AnyAsync(v => v.Placa == placa && v.VehiculoId != dto.Id))
        {
            _logger.LogWarning("Placa duplicada al actualizar vehículo {Id}: {Placa}", dto.Id, placa);
            throw new InvalidOperationException(
                $"Ya existe otro vehículo con la placa {Formatos.Placa(placa)}");
        }

        vehiculo.Placa = placa;
        vehiculo.Marca = dto.Marca.Trim();
        vehiculo.Modelo = dto.Modelo.Trim();
        vehiculo.Anio = anio;

        await context.SaveChangesAsync();
        return true;
    }

    // ~~~ ELIMINAR (soft delete) ~~~
    public async Task<bool> EliminarAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var vehiculo = await context.Vehiculo.FindAsync(id);
        if (vehiculo is null) return false;

        // Soft delete: el vehículo tiene citas con FK Restrict y es historial del
        // cliente; un borrado duro destruiría el vínculo sin dejar rastro.
        vehiculo.Activo = false;
        await context.SaveChangesAsync();
        _logger.LogInformation("Vehículo {Id} desactivado.", id);
        return true;
    }

    // ~~~ TRANSFERIR ~~~
    public async Task<bool> TransferirAsync(int vehiculoId, int nuevoClienteId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var vehiculo = await context.Vehiculo.FindAsync(vehiculoId);
        if (vehiculo is null) return false;

        // Mismo propietario: no-op con mensaje claro (evita duplicar lógica cliente-side)
        if (vehiculo.ClienteId == nuevoClienteId)
        {
            throw new InvalidOperationException("El vehículo ya pertenece a este cliente.");
        }

        // Cliente destino debe existir y estar activo
        var destino = await context.Cliente.FindAsync(nuevoClienteId);
        if (destino is null || !destino.Activo)
        {
            throw new InvalidOperationException("El cliente destino no está activo.");
        }

        // Bloquear si hay citas futuras (Fecha >= hoy y Estado no Cancelada/Finalizada)
        var hoy = DateTime.Today;
        var citasFuturas = await context.Cita.CountAsync(c =>
            c.VehiculoId == vehiculoId
            && c.Fecha >= hoy
            && c.Estado != EstadoCita.Cancelada
            && c.Estado != EstadoCita.Finalizada);

        if (citasFuturas > 0)
        {
            throw new InvalidOperationException(
                $"El vehículo tiene {citasFuturas} citas futuras. Cancelelas antes de transferirlo.");
        }

        // Ejecutar transferencia: cambia dueño y reactiva
        var clienteAnterior = vehiculo.ClienteId;
        vehiculo.ClienteId = nuevoClienteId;
        vehiculo.Activo = true;

        await context.SaveChangesAsync();
        _logger.LogInformation("Vehículo {Id} transferido del cliente {Origen} al cliente {Destino}.", vehiculoId, clienteAnterior, nuevoClienteId);
        return true;
    }
}
