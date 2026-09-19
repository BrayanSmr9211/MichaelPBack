using GestionTareas.Application.DTOs;

namespace GestionTareas.Application.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResponse>> ListarUsuariosAsync();
    Task<UsuarioResponse> CrearUsuarioAsync(CrearUsuarioRequest request);
}
