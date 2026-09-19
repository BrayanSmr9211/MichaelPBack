using GestionTareas.Application.DTOs;
using GestionTareas.Domain.Enums;

namespace GestionTareas.Application.Interfaces;

public interface ITareaService
{
    Task<IEnumerable<TareaResponse>> ListarTareasAsync(int? usuarioId, EstadoTarea? estado);
    Task<TareaResponse> CrearTareaAsync(CrearTareaRequest request);
    Task<TareaResponse> CambiarEstadoAsync(int tareaId, CambiarEstadoRequest request);
}
