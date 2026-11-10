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
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<ComentarioService> _logger;

    public ComentarioService(
        IDbContextFactory<MySQLDBContext> factory,
        IAuditoriaService auditoria,
        ILogger<ComentarioService> logger)
    {
        _factory = factory;
        _auditoria = auditoria;
        _logger = logger;
    }

    public async Task<List<ComentarioDTO>> ObtenerPorClienteAsync(int clienteId)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Comentario
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            // El comentario más nuevo va primero; ComentarioId desempata los del mismo segundo
            .OrderByDescending(c => c.FechaHora)
            .ThenByDescending(c => c.ComentarioId)
            .Select(c => new ComentarioDTO
            {
                Id = c.ComentarioId,
                Texto = c.Texto,
                FechaHora = c.FechaHora,
                AutorNombre = c.AutorNombre,
                AutorTipo = c.AutorTipo,
                EditadoEn = c.EditadoEn
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

    public async Task<bool> EditarAsync(int comentarioId, string nuevoTexto, string solicitanteNombre, TipoAutorComentario solicitanteTipo)
    {
        if (string.IsNullOrWhiteSpace(solicitanteNombre))
            throw new UnauthorizedAccessException("No se pudo identificar al usuario actual.");

        var texto = nuevoTexto?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
            throw new ArgumentException("El comentario no puede estar vacío.", nameof(nuevoTexto));

        await using var context = await _factory.CreateDbContextAsync();

        var comentario = await context.Comentario
            .FirstOrDefaultAsync(c => c.ComentarioId == comentarioId);
        if (comentario is null) return false;

        // Regla: solo el autor puede editar su propio comentario.
        var esAutor = string.Equals(comentario.AutorNombre?.Trim(), solicitanteNombre.Trim(), StringComparison.OrdinalIgnoreCase)
                      && comentario.AutorTipo == solicitanteTipo;
        if (!esAutor)
            throw new UnauthorizedAccessException("Solo el autor puede editar este comentario.");

        // Regla: una única edición por comentario.
        if (comentario.EditadoEn is not null)
            throw new InvalidOperationException("Este comentario ya fue editado y solo se puede editar una vez.");

        var textoAnterior = comentario.Texto;
        comentario.Texto = texto;
        comentario.EditadoEn = DateTime.Now;

        // Auditoría atómica: el log guarda SOLO el texto anterior, nunca el nuevo.
        await _auditoria.AgregarEnContextoAsync(
            context,
            "EDITAR_COMENTARIO",
            "Comentario",
            comentarioId.ToString(),
            $"Texto anterior: {textoAnterior}");

        await context.SaveChangesAsync();

        _logger.LogInformation("Comentario {Id} editado por {Autor}.", comentarioId, comentario.AutorNombre);
        return true;
    }

    public async Task<bool> EliminarAsync(int comentarioId, string solicitanteNombre, TipoAutorComentario solicitanteTipo)
    {
        if (string.IsNullOrWhiteSpace(solicitanteNombre))
            throw new UnauthorizedAccessException("No se pudo identificar al usuario actual.");

        await using var context = await _factory.CreateDbContextAsync();

        var comentario = await context.Comentario
            .FirstOrDefaultAsync(c => c.ComentarioId == comentarioId);
        if (comentario is null) return false;

        // Regla: solo el autor puede eliminar su propio comentario.
        // La identidad guardada es (AutorNombre, AutorTipo), así que comparamos ambos.
        var esAutor = string.Equals(comentario.AutorNombre?.Trim(), solicitanteNombre.Trim(), StringComparison.OrdinalIgnoreCase)
                      && comentario.AutorTipo == solicitanteTipo;
        if (!esAutor)
            throw new UnauthorizedAccessException("Solo el autor puede eliminar este comentario.");

        var textoEliminado = comentario.Texto;
        context.Comentario.Remove(comentario);

        // Auditoría atómica: el log guarda el texto que se elimina.
        await _auditoria.AgregarEnContextoAsync(
            context,
            "ELIMINAR_COMENTARIO",
            "Comentario",
            comentarioId.ToString(),
            $"Texto eliminado: {textoEliminado}");

        await context.SaveChangesAsync();

        _logger.LogInformation("Comentario {Id} eliminado.", comentarioId);
        return true;
    }
}
