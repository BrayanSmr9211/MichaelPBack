using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Enums;

namespace GestionTareas.Domain.Interfaces;

public interface ITareaRepository
{
    Task<IEnumerable<Tarea>> GetAllAsync(int? usuarioId, EstadoTarea? estado);
    Task<Tarea?> GetByIdAsync(int id);
    Task<Tarea> CreateAsync(Tarea tarea);
    Task<Tarea> UpdateAsync(Tarea tarea);
}
