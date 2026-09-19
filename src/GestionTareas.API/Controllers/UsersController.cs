using GestionTareas.Application.DTOs;
using GestionTareas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionTareas.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsersController(IUsuarioService service)
    {
        _service = service;
    }

    /// <summary>Lista todos los usuarios (requiere token).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UsuarioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await _service.ListarUsuariosAsync();
        return Ok(usuarios);
    }
}
