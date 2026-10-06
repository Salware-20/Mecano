using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mecano.Entidad.Clases;
using Mecano.Entidad.DTOs;

namespace Mecano.Logica.Interfaces;

public interface IServicioService
{
    Task<List<Servicio>> ObtenerTodosActivosAsync();
    Task<Servicio?> ObtenerPorIdAsync(int servicioId);
    TimeOnly CalcularHoraFin(TimeOnly horaInicio, int duracionMinutos);

    Task<List<ServicioDTO>> ObtenerTodosAsync(bool soloActivos = true);
    Task<ServicioDTO?> ObtenerDetallePorIdAsync(int id);
    Task<List<ServicioDTO>> ObtenerPorCategoriaAsync(int categoriaId, bool soloActivos = true);
    Task<int> CrearAsync(CrearServicioDTO dto);
    Task<bool> ActualizarAsync(ActualizarServicioDTO dto);
    Task<bool> EliminarAsync(int id);
    Task<bool> ReactivarAsync(int id);
    Task<bool> ExisteNombreEnCategoriaAsync(string nombre, int? categoriaId, int? excluirId = null);
}
