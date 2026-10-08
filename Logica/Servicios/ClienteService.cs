using System.Linq.Expressions;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs.Cliente;
using Mecano.Entidad.Utilidades;
using Mecano.Logica.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecano.Logica.Servicios;

public class ClienteService : IClienteService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(IDbContextFactory<MySQLDBContext> factory, ILogger<ClienteService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    // ~~~ FILTRO COMPARTIDO ~~~
    private static IQueryable<Cliente> AplicarFiltro(IQueryable<Cliente> query, string? termino, bool incluirInactivos)
    {
        if (!incluirInactivos)
            query = query.Where(c => c.Activo);

        if (string.IsNullOrWhiteSpace(termino))
            return query;

        var digitos = Formatos.SoloDigitos(termino);
        var placaNormalizada = Formatos.NormalizarPlaca(termino);

        // La cédula se almacena solo con dígitos y la placa en mayúsculas sin guiones,
        // pero el usuario las ve formateadas ("1-0111-0111", "ABC-123"). Por eso ambos
        // términos se comparan de dos formas: tal como se escribió, y normalizado, para
        // que "1-0111" encuentre 101110111 y "ABC-123" encuentre ABC123.
        // El nombre no se normaliza: no es un campo numérico/alcanumérico canónico.
        return query.Where(c =>
            EF.Functions.Like(c.NombreCompleto, $"%{termino}%")
            || EF.Functions.Like(c.CedulaIdentidad, $"%{termino}%")
            || (digitos.Length > 0 && EF.Functions.Like(c.CedulaIdentidad, $"%{digitos}%"))
            || c.Vehiculos.Any(v => EF.Functions.Like(v.Placa, $"%{termino}%")
                || (placaNormalizada.Length > 0 && EF.Functions.Like(v.Placa, $"%{placaNormalizada}%"))));
    }

    /// <summary>
    /// Normaliza y valida los campos numéricos de un cliente antes de persistirlos.
    /// Se ejecuta antes de la comprobación de unicidad: el índice único
    /// (CedulaIdentidad, TipoIdentificacion) compara cadenas crudas, así que sin
    /// normalizar "1-0111-0111" y "101110111" entrarían como dos personas distintas.
    /// </summary>
    private static (string Cedula, string? Telefono) NormalizarYValidar(string cedulaIdentidad, string? telefono)
    {
        var cedula = Formatos.SoloDigitos(cedulaIdentidad);
        var tel = Formatos.SoloDigitos(telefono);

        if (cedula.Length != Formatos.LargoCedulaNacional)
        {
            throw new InvalidOperationException(
                $"La cédula debe tener exactamente {Formatos.LargoCedulaNacional} dígitos.");
        }

        // El teléfono es opcional: si viene vacío se guarda null y no se valida.
        if (tel.Length > 0 && tel.Length != Formatos.LargoTelefono)
        {
            throw new InvalidOperationException(
                $"El teléfono debe tener exactamente {Formatos.LargoTelefono} dígitos.");
        }

        return (cedula, tel.Length > 0 ? tel : null);
    }

    /// <summary>
    /// Proyección compartida por las lecturas DTO. Los conteos se resuelven en el servidor
    /// con subconsultas correlacionadas, no con Include, para no materializar las colecciones.
    /// </summary>
    private static readonly Expression<Func<Cliente, ClienteDTO>> Proyeccion = c => new ClienteDTO
    {
        Id = c.ClienteId,
        CedulaIdentidad = c.CedulaIdentidad,
        TipoIdentificacion = c.TipoIdentificacion,
        NombreCompleto = c.NombreCompleto,
        Telefono = c.Telefono,
        Correo = c.Correo,
        Direccion = c.Direccion,
        FechaRegistro = c.FechaRegistro,
        Activo = c.Activo,
        // El conteo coincide con la lista visible de ObtenerPorClienteAsync:
        // cliente activo -> solo vehículos activos; cliente inactivo -> todos los vehículos.
        CantidadVehiculos = c.Activo
            ? c.Vehiculos.Count(v => v.Activo)
            : c.Vehiculos.Count(),
        CantidadCitas = c.Citas.Count()
    };

    // ~~~ LEGADO ~~~
    public async Task<List<Cliente>> BuscarClientesAsync(string? termino, int pagina = 1, int tamanioPagina = 10)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // El contrato legado nunca filtró por Activo; no lo alteramos para no romper a AgendarCita.razor.
        var query = AplicarFiltro(context.Cliente.AsNoTracking(), termino, incluirInactivos: true);

        return await query
            .OrderBy(c => c.NombreCompleto)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .ToListAsync();
    }

    // ~~~ READ (lista paginada) ~~~
    public async Task<List<ClienteDTO>> BuscarClientesPaginadoAsync(string? termino, int pagina, int tamanioPagina, bool incluirInactivos = false)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var query = AplicarFiltro(context.Cliente.AsNoTracking(), termino, incluirInactivos);

        return await query
            .OrderBy(c => c.NombreCompleto)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .Select(Proyeccion)
            .ToListAsync();
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int clienteId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Cliente
            .Include(c => c.Vehiculos)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);
    }

    /// <summary>
    /// Envoltura del contrato legado "register": delega en <see cref="CrearAsync"/> para que
    /// la validación de cédula duplicada y la asignación de TipoIdentificacion tengan una sola fuente.
    /// </summary>
    public async Task<int> RegistrarAsync(string cedulaIdentidad, string nombreCompleto, string? telefono, string? correo)
    {
        return await CrearAsync(new CrearClienteDTO
        {
            CedulaIdentidad = cedulaIdentidad,
            NombreCompleto = nombreCompleto,
            Telefono = telefono,
            Correo = correo
        });
    }

    // ~~~ READ (lista) ~~~
    public async Task<List<ClienteDTO>> ObtenerTodosAsync(bool soloActivos = true)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var query = context.Cliente.AsNoTracking();
        if (soloActivos)
            query = query.Where(c => c.Activo);

        return await query
            .OrderBy(c => c.NombreCompleto)
            .Select(Proyeccion)
            .ToListAsync();
    }

    // ~~~ READ (uno) ~~~
    public async Task<ClienteDTO?> ObtenerDetallePorIdAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Cliente
            .AsNoTracking()
            .Where(c => c.ClienteId == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();
    }

    // ~~~ CREATE ~~~
    public async Task<int> CrearAsync(CrearClienteDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Normalizar y validar ANTES de tocar la base: el chequeo de duplicado y el guardado
        // deben operar sobre la misma forma del valor que realmente se persiste.
        var (cedula, telefono) = NormalizarYValidar(dto.CedulaIdentidad, dto.Telefono);

        if (await ExisteIdentificacionAsync(cedula))
        {
            _logger.LogWarning("Cédula duplicada al crear cliente: {Cedula}", cedula);
            throw new InvalidOperationException($"Ya existe un cliente con la cédula {Formatos.CedulaNacional(cedula)}");
        }

        var nuevo = new Cliente
        {
            CedulaIdentidad = cedula,
            // Fase 1: el tipo lo decide el servicio. La Fase 2 lo elige el usuario en la UI.
            TipoIdentificacion = TipoIdentificacion.Nacional,
            NombreCompleto = dto.NombreCompleto.Trim(),
            Telefono = telefono,
            Correo = dto.Correo?.Trim(),
            Direccion = dto.Direccion?.Trim(),
            FechaRegistro = DateTime.Now,
            Activo = true
        };

        context.Cliente.Add(nuevo);
        await context.SaveChangesAsync();
        return nuevo.ClienteId;
    }

    // ~~~ UPDATE ~~~
    public async Task<bool> ActualizarAsync(ActualizarClienteDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var cliente = await context.Cliente.FindAsync(dto.Id);
        if (cliente is null) return false;

        var (cedula, telefono) = NormalizarYValidar(dto.CedulaIdentidad, dto.Telefono);

        if (await ExisteIdentificacionAsync(cedula, excluirId: dto.Id))
        {
            _logger.LogWarning("Cédula duplicada al actualizar cliente {Id}: {Cedula}", dto.Id, cedula);
            throw new InvalidOperationException($"Ya existe otro cliente con la cédula {Formatos.CedulaNacional(cedula)}");
        }

        cliente.CedulaIdentidad = cedula;
        cliente.NombreCompleto = dto.NombreCompleto.Trim();
        cliente.Telefono = telefono;
        cliente.Correo = dto.Correo?.Trim();
        cliente.Direccion = dto.Direccion?.Trim();

        await context.SaveChangesAsync();
        return true;
    }

    // ~~~ DESACTIVAR (soft delete) ~~~
    public async Task<bool> DesactivarAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var cliente = await context.Cliente.FindAsync(id);
        if (cliente is null) return false;

        // Soft delete en cascada: todos los vehículos activos del cliente pasan a inactivos
        var vehiculos = await context.Vehiculo
            .Where(v => v.ClienteId == id && v.Activo)
            .ToListAsync();

        cliente.Activo = false;
        foreach (var v in vehiculos) v.Activo = false;

        await context.SaveChangesAsync();
        _logger.LogInformation("Cliente {Id} desactivado; {N} vehículos desactivados en cascada.", id, vehiculos.Count);
        return true;
    }

    // ~~~ REACTIVAR ~~~
    public async Task<bool> ReactivarAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var cliente = await context.Cliente.FindAsync(id);
        if (cliente is null) return false;

        // Reactivación en cascada: TODOS los vehículos del cliente (activos e inactivos)
        // vuelven a activos. Sin flag DesactivadoPorCascade (YAGNI).
        var vehiculos = await context.Vehiculo
            .Where(v => v.ClienteId == id)
            .ToListAsync();

        cliente.Activo = true;
        foreach (var v in vehiculos) v.Activo = true;

        await context.SaveChangesAsync();
        _logger.LogInformation("Cliente {Id} reactivado; {N} vehículos reactivados en cascada.", id, vehiculos.Count);
        return true;
    }

    // ~~~ DELETE (hard, solo si no tiene dependencias) ~~~
    public async Task<bool> EliminarDefinitivoAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var cliente = await context.Cliente
            .Include(c => c.Vehiculos)
            .Include(c => c.Citas)
            .FirstOrDefaultAsync(c => c.ClienteId == id);

        if (cliente is null) return false;

        if (cliente.Vehiculos.Any())
            throw new InvalidOperationException("No se puede eliminar el cliente: tiene vehículos registrados.");
        if (cliente.Citas.Any())
            throw new InvalidOperationException("No se puede eliminar el cliente: tiene citas asociadas.");

        context.Cliente.Remove(cliente);
        await context.SaveChangesAsync();
        _logger.LogInformation("Cliente {Id} eliminado definitivamente.", id);
        return true;
    }

    // ~~~ HELPER ~~~
    public async Task<bool> ExisteIdentificacionAsync(string cedulaIdentidad, int? excluirId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();

        // Normalizamos también acá: el método es público y puede llamarse con un valor
        // crudo, en cuyo caso compararía mal contra lo que hay guardado (solo dígitos).
        var normalizada = Formatos.SoloDigitos(cedulaIdentidad);

        if (normalizada.Length == 0)
            return false;

        // Fase 1: el índice único es (CedulaIdentidad, TipoIdentificacion) pero solo
        // se consulta Nacional. La Fase 2 amplía este predicado, no el índice.
        return await context.Cliente.AnyAsync(c =>
            c.CedulaIdentidad == normalizada
            && c.TipoIdentificacion == TipoIdentificacion.Nacional
            && (excluirId == null || c.ClienteId != excluirId));
    }

    // ~~~ COUNT (para paginación) ~~~
    // Comparte AplicarFiltro con BuscarClientesPaginadoAsync para que el total coincida
    // exactamente con el conjunto paginado.
    public async Task<int> ContarBusquedaAsync(string? termino, bool incluirInactivos = false)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await AplicarFiltro(context.Cliente.AsNoTracking(), termino, incluirInactivos)
            .CountAsync();
    }
}
