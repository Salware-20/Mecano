using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mecano.Data;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs.Mecanico;
using Mecano.Logica.Interfaces;
using Mecano.Logica.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mecano.Logica.Servicios;

public class MecanicoService : IMecanicoService
{
    private readonly IDbContextFactory<MySQLDBContext> _factory;
    private readonly ILogger<MecanicoService> _logger;

    public MecanicoService(IDbContextFactory<MySQLDBContext> factory, ILogger<MecanicoService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    // ~~~ Métodos existentes — consumidos por AgendarCita. No modificar. ~~~

    public async Task<List<Mecanico>> ObtenerPorEspecialidadAsync(int categoriaId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Mecanico
            .Where(m => m.EspecialidadId == categoriaId && m.Activo)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Mecanico>> ObtenerDisponiblesAsync(int categoriaId, DateTime fecha, TimeOnly horaInicio, TimeOnly horaFin)
    {
        using var context = await _factory.CreateDbContextAsync();

        var busyMechanicIds = await context.Cita
            .Where(c => c.Fecha.Date == fecha.Date
                && c.Estado != EstadoCita.Cancelada
                && c.Estado != EstadoCita.Finalizada
                && c.HoraInicio < horaFin
                && horaInicio < c.HoraFin)
            .Select(c => c.MecanicoId)
            .Distinct()
            .ToListAsync();

        return await context.Mecanico
            .Where(m => m.EspecialidadId == categoriaId
                     && m.Activo
                     && !busyMechanicIds.Contains(m.MecanicoId))
            .AsNoTracking()
            .ToListAsync();
    }

    // ~~~ READ (lista) ~~~

    public async Task<List<MecanicoDTO>> ObtenerTodosAsync(bool soloActivos = true)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Mecanico
            .AsNoTracking()
            .Where(m => !soloActivos || m.Activo)
            .OrderBy(m => m.Nombre)
            .Select(m => new MecanicoDTO
            {
                Id                 = m.MecanicoId,
                Nombre             = m.Nombre,
                Email              = m.Email,
                Cedula             = m.Cedula,
                Telefono           = m.Telefono,
                EspecialidadId     = m.EspecialidadId,
                EspecialidadNombre = m.Especialidad.Nombre,
                Activo             = m.Activo
            })
            .ToListAsync();
    }

    // ~~~ READ (uno) ~~~

    public async Task<MecanicoDTO?> ObtenerDetallePorIdAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        return await context.Mecanico
            .AsNoTracking()
            .Where(m => m.MecanicoId == id)
            .Select(m => new MecanicoDTO
            {
                Id                 = m.MecanicoId,
                Nombre             = m.Nombre,
                Email              = m.Email,
                Cedula             = m.Cedula,
                Telefono           = m.Telefono,
                EspecialidadId     = m.EspecialidadId,
                EspecialidadNombre = m.Especialidad.Nombre,
                Activo             = m.Activo
            })
            .FirstOrDefaultAsync();
    }

    // ~~~ CREATE ~~~

    public async Task<int> CrearAsync(CrearMecanicoDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var nombre   = dto.Nombre.Trim();
        var email    = dto.Email.Trim().ToLowerInvariant();
        var cedula   = dto.Cedula?.Trim();      // solo Trim — los guiones se conservan
        var telefono = dto.Telefono?.Trim();

        // Validar unicidad de email en Administradores y Mecánicos
        if (await ExisteEmailAsync(email))
            throw new InvalidOperationException(
                $"Ya existe un usuario (administrador o mecánico) con el correo '{email}'.");

        // Validar que la especialidad exista
        if (!await context.Categorias.AnyAsync(c => c.CategoriaId == dto.EspecialidadId))
            throw new InvalidOperationException("La especialidad seleccionada no existe.");

        var mecanico = new Mecanico
        {
            Nombre        = nombre,
            Email         = email,
            Cedula        = cedula,
            Telefono      = telefono,
            EspecialidadId = dto.EspecialidadId!.Value,
            HashPassword  = PasswordHasher.Hash(dto.Password),
            Activo        = true
        };

        context.Mecanico.Add(mecanico);
        await context.SaveChangesAsync();
        return mecanico.MecanicoId;
    }

    // ~~~ UPDATE ~~~

    public async Task<bool> ActualizarAsync(ActualizarMecanicoDTO dto)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var mecanico = await context.Mecanico.FindAsync(dto.Id);
        if (mecanico is null) return false;

        var nombre   = dto.Nombre.Trim();
        var email    = dto.Email.Trim().ToLowerInvariant();
        var cedula   = dto.Cedula?.Trim();      // solo Trim — los guiones se conservan
        var telefono = dto.Telefono?.Trim();

        // Validar unicidad de email excluyendo al propio mecánico
        if (await ExisteEmailAsync(email, excluirId: dto.Id))
            throw new InvalidOperationException(
                $"Ya existe un usuario (administrador o mecánico) con el correo '{email}'.");

        // Validar que la especialidad exista
        if (!await context.Categorias.AnyAsync(c => c.CategoriaId == dto.EspecialidadId))
            throw new InvalidOperationException("La especialidad seleccionada no existe.");

        // Contraseña: solo re-hashear si se proporcionó un valor nuevo
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            // [Compare] en el DTO cubre "ambas llenas pero distintas".
            // Aquí cubrimos el caso asimétrico: Password llena, ConfirmarPassword vacía.
            if (string.IsNullOrWhiteSpace(dto.ConfirmarPassword))
                throw new InvalidOperationException("Debe confirmar la contraseña.");

            mecanico.HashPassword = PasswordHasher.Hash(dto.Password);
        }

        mecanico.Nombre        = nombre;
        mecanico.Email         = email;
        mecanico.Cedula        = cedula;
        mecanico.Telefono      = telefono;
        mecanico.EspecialidadId = dto.EspecialidadId!.Value;
        // Activo no se toca en una actualización de datos

        await context.SaveChangesAsync();
        return true;
    }

    // ~~~ DESACTIVAR / REACTIVAR ~~~

    public async Task<bool> DesactivarAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var mecanico = await context.Mecanico.FindAsync(id);
        if (mecanico is null) return false;

        mecanico.Activo = false;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivarAsync(int id)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var mecanico = await context.Mecanico.FindAsync(id);
        if (mecanico is null) return false;

        mecanico.Activo = true;
        await context.SaveChangesAsync();
        return true;
    }

    // ~~~ HELPER ~~~

    public async Task<bool> ExisteEmailAsync(string email, int? excluirId = null)
    {
        var normalized = email.Trim().ToLowerInvariant();
        await using var context = await _factory.CreateDbContextAsync();

        // Los emails de Administrador pueden no estar en minúsculas (RegisterAdminAsync
        // guarda email.Trim() sin ToLower), por lo que se normaliza en la consulta.
        // La colación utf8mb4_0900_ai_ci de MySQL hace la comparación insensible al caso,
        // pero normalizamos explícitamente para coherencia con el valor ya normalizado.
        if (await context.Administradors.AnyAsync(a => a.Email.ToLower() == normalized))
            return true;

        // Los emails de Mecánico siempre se almacenan en minúsculas (invariante de este servicio),
        // por lo que la comparación directa es suficiente y evita la función por fila.
        return await context.Mecanico.AnyAsync(m =>
            m.Email == normalized &&
            (excluirId == null || m.MecanicoId != excluirId));
    }
}
