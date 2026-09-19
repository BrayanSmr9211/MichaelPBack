using GestionTareas.Domain.Entities;

namespace GestionTareas.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario?> GetByEmailAsync(string email);
    Task<bool> ExisteEmailAsync(string email);
    Task<Usuario> CreateAsync(Usuario usuario);
}