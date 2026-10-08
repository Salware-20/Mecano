using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;
using Mecano.Logica.Interfaces;

namespace Mecano.Logica.Servicios;

public class ServicioService : IServicioService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<ServicioService> _logger;

    public ServicioService(IDbContextFactory<MySQLDBContext> factory, ILogger<ServicioService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<Servicio>> ObtenerTodosActivosAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Servicio
            .Include(s => s.Categoria)
            .Where(s => s.Activo)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Servicio?> ObtenerPorIdAsync(int servicioId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Servicio
            .Include(s => s.Categoria)
            .FirstOrDefaultAsync(s => s.ServicioId == servicioId);
    }

    public TimeOnly CalcularHoraFin(TimeOnly horaInicio, int duracionMinutos)
    {
        return horaInicio.AddMinutes(duracionMinutos);
    }

    // ~~~ READ (lista) ~~~
    public async Task<List<ServicioDTO>> ObtenerTodosAsync(bool soloActivos = true)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var query = context.Servicio.AsNoTracking();
        if (soloActivos)
            query = query.Where(s => s.Activo);

        return await query
            .OrderBy(s => s.Categoria == null ? "" : s.Categoria!.Nombre)
            .ThenBy(s => s.Nombre)
            .Select(s => new ServicioDTO
            {
                Id = s.ServicioId,
                Nombre = s.Nombre,
                Descripcion = s.Descripcion,
                DuracionMinutos = s.DuracionMinutos,
                Precio = s.Precio,
                Activo = s.Activo,
                CategoriaId = s.CategoriaId,
                CategoriaNombre = s.Categoria != null ? s.Categoria.Nombre : null
            })
            .ToListAsync();
    }

    // ~~~ READ (uno) ~~~
    public async Task<ServicioDTO?> ObtenerDetallePorIdAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Servicio
            .AsNoTracking()
            .Where(s => s.ServicioId == id)
            .Select(s => new ServicioDTO
            {
                Id = s.ServicioId,
                Nombre = s.Nombre,
                Descripcion = s.Descripcion,
                DuracionMinutos = s.DuracionMinutos,
                Precio = s.Precio,
                Activo = s.Activo,
                CategoriaId = s.CategoriaId,
                CategoriaNombre = s.Categoria != null ? s.Categoria.Nombre : null
            })
            .FirstOrDefaultAsync();
    }

    // ~~~ READ (por categoría) ~~~
    public async Task<List<ServicioDTO>> ObtenerPorCategoriaAsync(int categoriaId, bool soloActivos = true)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var query = context.Servicio
            .AsNoTracking()
            .Where(s => s.CategoriaId == categoriaId);
        if (soloActivos)
            query = query.Where(s => s.Activo);

        return await query
            .OrderBy(s => s.Categoria == null ? "" : s.Categoria!.Nombre)
            .ThenBy(s => s.Nombre)
            .Select(s => new ServicioDTO
            {
                Id = s.ServicioId,
                Nombre = s.Nombre,
                Descripcion = s.Descripcion,
                DuracionMinutos = s.DuracionMinutos,
                Precio = s.Precio,
                Activo = s.Activo,
                CategoriaId = s.CategoriaId,
                CategoriaNombre = s.Categoria != null ? s.Categoria.Nombre : null
            })
            .ToListAsync();
    }

    // ~~~ CREATE ~~~
    public async Task<int> CrearAsync(CrearServicioDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        if (dto.CategoriaId.HasValue &&
            !await context.Categorias.AnyAsync(c => c.CategoriaId == dto.CategoriaId.Value))
        {
            throw new InvalidOperationException(
                $"La categoría con Id {dto.CategoriaId} no existe.");
        }

        if (await ExisteNombreEnCategoriaAsync(dto.Nombre, dto.CategoriaId))
        {
            _logger.LogWarning("Conflicto de nombre al crear servicio: {Nombre} (categoria {CategoriaId}).", dto.Nombre, dto.CategoriaId);
            throw new InvalidOperationException("Ya existe un servicio con ese nombre en esta categoría.");
        }

        var nuevo = new Servicio
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            DuracionMinutos = dto.DuracionMinutos,
            Precio = dto.Precio,
            CategoriaId = dto.CategoriaId,
            Activo = true
        };

        context.Servicio.Add(nuevo);
        await context.SaveChangesAsync();
        return nuevo.ServicioId;
    }

    // ~~~ UPDATE ~~~
    public async Task<bool> ActualizarAsync(ActualizarServicioDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var servicio = await context.Servicio.FindAsync(dto.Id);
        if (servicio is null) return false;

        if (dto.CategoriaId.HasValue &&
            !await context.Categorias.AnyAsync(c => c.CategoriaId == dto.CategoriaId.Value))
        {
            throw new InvalidOperationException(
                $"La categoría con Id {dto.CategoriaId} no existe.");
        }

        if (await ExisteNombreEnCategoriaAsync(dto.Nombre, dto.CategoriaId, excluirId: dto.Id))
        {
            _logger.LogWarning("Conflicto de nombre al actualizar servicio {Id}: {Nombre} (categoria {CategoriaId}).", dto.Id, dto.Nombre, dto.CategoriaId);
            throw new InvalidOperationException($"Ya existe otro servicio con el nombre '{dto.Nombre}' en esta categoría.");
        }

        servicio.Nombre = dto.Nombre.Trim();
        servicio.Descripcion = dto.Descripcion?.Trim();
        servicio.DuracionMinutos = dto.DuracionMinutos;
        servicio.Precio = dto.Precio;
        servicio.CategoriaId = dto.CategoriaId;

        await context.SaveChangesAsync();
        return true;
    }

    // ~~~ DELETE (soft) ~~~
    public async Task<bool> EliminarAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var servicio = await context.Servicio.FindAsync(id);
        if (servicio is null) return false;

        // Soft delete: Cita has a Restrict FK on ServicioId, so we never remove the row.
        servicio.Activo = false;
        await context.SaveChangesAsync();
        return true;
    }

    // ~~~ REACTIVATE ~~~
    public async Task<bool> ReactivarAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var servicio = await context.Servicio.FindAsync(id);
        if (servicio is null) return false;

        servicio.Activo = true;
        await context.SaveChangesAsync();
        _logger.LogInformation("Servicio {Id} reactivado.", id);
        return true;
    }

    // ~~~ HELPER ~~~
    public async Task<bool> ExisteNombreEnCategoriaAsync(string nombre, int? categoriaId, int? excluirId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var normalizado = nombre.Trim().ToLower();

        return await context.Servicio.AnyAsync(s => s.Nombre.ToLower() == normalizado
            && (categoriaId.HasValue ? s.CategoriaId == categoriaId.Value : s.CategoriaId == null)
            && (excluirId == null || s.ServicioId != excluirId));
    }
}
