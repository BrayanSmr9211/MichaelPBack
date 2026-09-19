using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Interfaces;
using GestionTareas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionTareas.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context) { _context = context; }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
        => await _context.Usuarios.OrderBy(u => u.Nombre).ToListAsync();

    public async Task<Usuario?> GetByIdAsync(int id)
        => await _context.Usuarios.FindAsync(id);

    public async Task<Usuario?> GetByEmailAsync(string email)
        => await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim());

    public async Task<bool> ExisteEmailAsync(string email)
        => await _context.Usuarios
            .AnyAsync(u => u.Email == email.ToLower().Trim());

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }
}