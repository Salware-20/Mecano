using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Categoria;
using Mecano.Logica.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    // ~~~ READ (lista) ~~~
    public async Task<List<CategoriaDTO>> ObtenerTodasAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDTO
            {
                Id = c.CategoriaId,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                CantidadServicios = c.servicios.Count,
                CantidadMecanicos = c.Mecanicos.Count
            })
            .ToListAsync();
    }
    // ~~~ READ (uno) ~~~
    public async Task<CategoriaDTO?> ObtenerPorIdAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Categorias
            .AsNoTracking()
            .Where(c => c.CategoriaId == id)
            .Select(c => new CategoriaDTO
            {
                Id = c.CategoriaId,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                CantidadServicios = c.servicios.Count,
                CantidadMecanicos = c.Mecanicos.Count
            })
            .FirstOrDefaultAsync();
    }
    // ~~~ CREATE ~~~
    public async Task<int> CrearAsync(CrearCategoriaDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        if (await ExisteNombreAsync(dto.Nombre)) 
            throw new InvalidOperationException($"Ya existe una categoría con el nombre '{dto.Nombre}'.");

        var nueva = new Categoria
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion.Trim()
        };

        context.Categorias.Add(nueva);
        await context.SaveChangesAsync();
        return nueva.CategoriaId;
    }
    // ~~~ UPDATE ~~~
    public async Task<bool> ActualizarAsync(ActualizarCategoriaDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var categoria = await context.Categorias.FindAsync(dto.Id);
        if (categoria is null) return false;

        if (await ExisteNombreAsync(dto.Nombre, excluirId: dto.Id)) 
            throw new InvalidOperationException($"Ya existe otra categoría con el nombre '{dto.Nombre}'.");

        categoria.Nombre = dto.Nombre.Trim();
        categoria.Descripcion = dto.Descripcion?.Trim();

        await context.SaveChangesAsync();
        return true;
    }
    // ~~~ DELETE ~~~
    public async Task<bool> EliminarAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var categoria = await context.Categorias
            .Include(c => c.servicios)
            .Include(c => c.Mecanicos)
            .FirstOrDefaultAsync(c => c.CategoriaId == id);

        if (categoria is null) return false;
        // Regla de negocio: no borrar si está en uso.
        if (categoria.servicios.Any() || categoria.Mecanicos.Any())
            throw new InvalidOperationException($"No se puede eliminar: la categoría tiene servicios o mecánicos asociados.");
        context.Categorias.Remove(categoria);
        await context.SaveChangesAsync();
        return true;
    }
    // ~~~ HELPER ~~~
    public async Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var normalizado = nombre.Trim().ToLower();

        return await context.Categorias.AnyAsync(c => c.Nombre.ToLower() == normalizado 
        && (excluirId == null || c.CategoriaId != excluirId));
    }
    

}
