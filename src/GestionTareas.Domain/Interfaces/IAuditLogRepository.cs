using GestionTareas.Domain.Entities;

namespace GestionTareas.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<IEnumerable<AuditLog>> GetByTareaIdAsync(int tareaId);
}