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

public class CategoriaService : ICategoriaService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<CategoriaService> _logger;

    public CategoriaService(IDbContextFactory<MySQLDBContext> factory, ILogger<CategoriaService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Categoria>> ObtenerTodasAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Categorias
            .AsNoTracking()
            .ToListAsync();
    }
}
