using GestionTareas.Application.DTOs;
using GestionTareas.Application.Interfaces;
using GestionTareas.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestionTareas.API.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITareaService _service;

    public TasksController(ITareaService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista todas las tareas. Permite filtrar por usuarioId y/o estado.
    /// Resultados ordenados por fecha de creacion.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TareaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? usuarioId,
        [FromQuery] EstadoTarea? estado)
    {
        var tareas = await _service.ListarTareasAsync(usuarioId, estado);
        return Ok(tareas);
    }

    /// <summary>Crea una nueva tarea y la asigna a un usuario.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TareaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CrearTareaRequest request)
    {
        var tarea = await _service.CrearTareaAsync(request);
        return CreatedAtAction(nameof(GetAll), new { }, tarea);
    }

    /// <summary>
    /// Cambia el estado de una tarea.
    /// Regla: no se permite pasar directamente de Pending a Done.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(TareaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] int id,
        [FromBody] CambiarEstadoRequest request)
    {
        var tarea = await _service.CambiarEstadoAsync(id, request);
        return Ok(tarea);
    }
}
