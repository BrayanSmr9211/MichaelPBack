using GestionTareas.Application.DTOs;
using GestionTareas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GestionTareas.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsersController(IUsuarioService service)
    {
        _service = service;
    }

    /// <summary>Lista todos los usuarios registrados.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UsuarioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await _service.ListarUsuariosAsync();
        return Ok(usuarios);
    }

    /// <summary>Crea un nuevo usuario.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CrearUsuarioRequest request)
    {
        var usuario = await _service.CrearUsuarioAsync(request);
        return CreatedAtAction(nameof(GetAll), new { }, usuario);
    }
}
