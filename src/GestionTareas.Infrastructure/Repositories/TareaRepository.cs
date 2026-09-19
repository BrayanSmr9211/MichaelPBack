using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Enums;
using GestionTareas.Domain.Interfaces;
using GestionTareas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionTareas.Infrastructure.Repositories;

public class TareaRepository : ITareaRepository
{
    private readonly AppDbContext _context;

    public TareaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tarea>> GetAllAsync(int? usuarioId, EstadoTarea? estado)
    {
        var query = _context.Tareas
            .Include(t => t.Usuario)
            .AsQueryable();

        if (usuarioId.HasValue)
            query = query.Where(t => t.UsuarioId == usuarioId.Value);

        if (estado.HasValue)
            query = query.Where(t => t.Estado == estado.Value);

        return await query
            .OrderBy(t => t.FechaCreacion)
            .ToListAsync();
    }

    public async Task<Tarea?> GetByIdAsync(int id)
        => await _context.Tareas
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tarea> CreateAsync(Tarea tarea)
    {
        _context.Tareas.Add(tarea);
        await _context.SaveChangesAsync();
        return tarea;
    }

    public async Task<Tarea> UpdateAsync(Tarea tarea)
    {
        _context.Tareas.Update(tarea);
        await _context.SaveChangesAsync();
        return tarea;
    }
}
