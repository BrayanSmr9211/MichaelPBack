using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Interfaces;
using GestionTareas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionTareas.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByTareaIdAsync(int tareaId)
        => await _context.AuditLogs
            .Where(a => a.TareaId == tareaId)
            .OrderByDescending(a => a.FechaRegistro)
            .ToListAsync();
}