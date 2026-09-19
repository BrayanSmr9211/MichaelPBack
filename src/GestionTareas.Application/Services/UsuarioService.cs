using GestionTareas.Application.DTOs;
using GestionTareas.Application.Interfaces;
using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Exceptions;
using GestionTareas.Domain.Interfaces;

namespace GestionTareas.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repo;

    public UsuarioService(IUsuarioRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<UsuarioResponse>> ListarUsuariosAsync()
    {
        var usuarios = await _repo.GetAllAsync();
        return usuarios.Select(MapToResponse);
    }

    public async Task<UsuarioResponse> CrearUsuarioAsync(CrearUsuarioRequest request)
    {
        // Validar email unico
        if (await _repo.ExisteEmailAsync(request.Email))
            throw new BusinessRuleException($"Ya existe un usuario registrado con el email '{request.Email}'.");

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = request.Email.Trim().ToLower(),
            FechaCreacion = DateTime.UtcNow
        };

        var creado = await _repo.CreateAsync(usuario);
        return MapToResponse(creado);
    }

    private static UsuarioResponse MapToResponse(Usuario u) =>
        new(u.Id, u.Nombre, u.Email, u.FechaCreacion);
}
