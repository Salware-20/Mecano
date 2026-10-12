using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.Constantes;
using Mecano.Entidad.DTOs.Comentario;
using Mecano.Logica.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecano.Logica.Servicios;

public class ComentarioService : IComentarioService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<ComentarioService> _logger;

    public ComentarioService(IDbContextFactory<MySQLDBContext> factory, ILogger<ComentarioService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<ComentarioDTO>> ObtenerPorClienteAsync(int clienteId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Comentario
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .OrderBy(c => c.FechaHora)   // orden cronológico del hilo
            .Select(c => new ComentarioDTO
            {
                Id = c.ComentarioId,
                Texto = c.Texto,
                FechaHora = c.FechaHora,
                AutorNombre = c.AutorNombre,
                AutorTipo = c.AutorTipo
            })
            .ToListAsync();
    }

    public async Task<int> CrearAsync(CrearComentarioDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var texto = dto.Texto?.Trim() ?? string.Empty;
        var autor = dto.AutorNombre?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(texto))
            throw new ArgumentException("El comentario no puede estar vacío.", nameof(dto));
        if (string.IsNullOrWhiteSpace(autor))
            throw new ArgumentException("El autor es obligatorio.", nameof(dto));
        if (!Enum.IsDefined(dto.AutorTipo))
            throw new ArgumentException("Tipo de autor inválido.", nameof(dto));

        await using var context = await _factory.CreateDbContextAsync();

        if (!await context.Cliente.AnyAsync(c => c.ClienteId == dto.ClienteId))
            throw new InvalidOperationException($"El cliente {dto.ClienteId} no existe.");

        var comentario = new Comentario
        {
            ClienteId = dto.ClienteId,
            Texto = texto,
            AutorNombre = autor,
            AutorTipo = dto.AutorTipo,
            FechaHora = DateTime.Now
        };

        context.Comentario.Add(comentario);
        await context.SaveChangesAsync();

        _logger.LogInformation("Comentario {Id} creado por {Autor} para el cliente {ClienteId}.",
            comentario.ComentarioId, comentario.AutorNombre, dto.ClienteId);
        return comentario.ComentarioId;
    }

    public async Task<bool> EliminarAsync(int comentarioId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var comentario = await context.Comentario
            .FirstOrDefaultAsync(c => c.ComentarioId == comentarioId);
        if (comentario is null) return false;

        context.Comentario.Remove(comentario);
        await context.SaveChangesAsync();

        _logger.LogInformation("Comentario {Id} eliminado.", comentarioId);
        return true;
    }
}
