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

public class VehiculoService : IVehiculoService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<VehiculoService> _logger;

    public VehiculoService(IDbContextFactory<MySQLDBContext> factory, ILogger<VehiculoService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Vehiculo>> ObtenerPorClienteAsync(int clienteId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Vehiculo
            .Where(v => v.ClienteId == clienteId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Vehiculo> RegistrarAsync(int clienteId, string placa, string marca, string modelo, int anio)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        if (await context.Vehiculo.AnyAsync(v => v.Placa == placa))
        {
            throw new InvalidOperationException($"Ya existe un vehículo con la placa {placa}");
        }

        var vehiculo = new Vehiculo
        {
            ClienteId = clienteId,
            Placa = placa,
            Marca = marca,
            Modelo = modelo,
            Anio = anio
        };

        context.Vehiculo.Add(vehiculo);
        await context.SaveChangesAsync();
        
        return vehiculo;
    }
}
