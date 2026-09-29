using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Logica.Interfaces;

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

    public async Task<List<Cliente>> BuscarClientesAsync(string termino, int pagina = 1, int tamanioPagina = 10)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        var query = context.Cliente.AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(termino))
        {
            query = query.Where(c => 
                c.NombreCompleto.Contains(termino) || 
                c.CedulaIdentidad.Contains(termino) ||
                c.Vehiculos.Any(v => v.Placa.Contains(termino)));
        }

        return await query
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .ToListAsync();
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int clienteId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Cliente
            .Include(c => c.Vehiculos)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);
    }

    public async Task<Cliente> RegistrarAsync(string cedulaIdentidad, string nombreCompleto, string? telefono, string? correo)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        if (await context.Cliente.AnyAsync(c => c.CedulaIdentidad == cedulaIdentidad))
        {
            throw new InvalidOperationException($"Ya existe un cliente con la cédula {cedulaIdentidad}");
        }

        var cliente = new Cliente
        {
            CedulaIdentidad = cedulaIdentidad,
            NombreCompleto = nombreCompleto,
            Telefono = telefono,
            Correo = correo,
            FechaRegistro = DateTime.Now,
            Activo = true
        };

        context.Cliente.Add(cliente);
        await context.SaveChangesAsync();
        
        return cliente;
    }
}
