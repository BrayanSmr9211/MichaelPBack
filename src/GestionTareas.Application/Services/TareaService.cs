using System.Text.Json;
using GestionTareas.Application.DTOs;
using GestionTareas.Application.Interfaces;
using GestionTareas.Application.Messages;
using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Enums;
using GestionTareas.Domain.Exceptions;
using GestionTareas.Domain.Interfaces;

namespace GestionTareas.Application.Services;

public class TareaService : ITareaService
{
    private readonly ITareaRepository _tareaRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IMessagePublisher _publisher;

    public TareaService(
        ITareaRepository tareaRepo,
        IUsuarioRepository usuarioRepo,
        IMessagePublisher publisher)
    {
        _tareaRepo = tareaRepo;
        _usuarioRepo = usuarioRepo;
        _publisher = publisher;
    }

    public async Task<IEnumerable<TareaResponse>> ListarTareasAsync(int? usuarioId, EstadoTarea? estado)
    {
        var tareas = await _tareaRepo.GetAllAsync(usuarioId, estado);
        return tareas.Select(MapToResponse);
    }

    public async Task<TareaResponse> CrearTareaAsync(CrearTareaRequest request)
    {
        // RN: titulo obligatorio (ya validado por DataAnnotations, doble check en servicio)
        if (string.IsNullOrWhiteSpace(request.Titulo))
            throw new BusinessRuleException("El titulo de la tarea es obligatorio.");

        // RN: usuario debe existir
        var usuario = await _usuarioRepo.GetByIdAsync(request.UsuarioId)
            ?? throw new NotFoundException($"No existe un usuario con Id {request.UsuarioId}.");

        // Validar que InfoAdicional sea JSON valido si se proporciona
        if (!string.IsNullOrWhiteSpace(request.InfoAdicional))
        {
            try
            {
                JsonDocument.Parse(request.InfoAdicional);
            }
            catch (JsonException)
            {
                throw new BusinessRuleException("El campo InfoAdicional debe ser un JSON valido.");
            }
        }

        var tarea = new Tarea
        {
            Titulo = request.Titulo.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            UsuarioId = request.UsuarioId,
            Estado = EstadoTarea.Pending,
            FechaCreacion = DateTime.UtcNow,
            InfoAdicional = request.InfoAdicional
        };

        var creada = await _tareaRepo.CreateAsync(tarea);
        creada.Usuario = usuario;

        await _publisher.PublishAsync(new TareaCreatedMessage(
            creada.Id, creada.Titulo, creada.UsuarioId,
            usuario.Nombre, creada.Estado.ToString(), creada.FechaCreacion));

        return MapToResponse(creada);
    }

    public async Task<TareaResponse> CambiarEstadoAsync(int tareaId, CambiarEstadoRequest request)
    {
        var tarea = await _tareaRepo.GetByIdAsync(tareaId)
            ?? throw new NotFoundException($"No existe una tarea con Id {tareaId}.");

        var estadoAnterior = tarea.Estado;
        var estadoNuevo = request.NuevoEstado;

        // RN: no se permite pasar directamente de Pending a Done
        if (estadoAnterior == EstadoTarea.Pending && estadoNuevo == EstadoTarea.Done)
            throw new BusinessRuleException(
                "No se permite cambiar una tarea directamente de Pending a Done. Debe pasar primero por InProgress.");

        // No tiene sentido cambiar al mismo estado
        if (estadoAnterior == estadoNuevo)
            throw new BusinessRuleException($"La tarea ya se encuentra en estado '{estadoNuevo}'.");

        tarea.Estado = estadoNuevo;
        tarea.FechaActualizacion = DateTime.UtcNow;

        var actualizada = await _tareaRepo.UpdateAsync(tarea);

        await _publisher.PublishAsync(new TareaStatusChangedMessage(
            actualizada.Id, actualizada.Titulo, actualizada.UsuarioId,
            estadoAnterior.ToString(), estadoNuevo.ToString(), DateTime.UtcNow));

        return MapToResponse(actualizada);
    }

    private static TareaResponse MapToResponse(Tarea t) => new(
        t.Id,
        t.Titulo,
        t.Descripcion,
        t.Estado.ToString(),
        t.UsuarioId,
        t.Usuario?.Nombre ?? string.Empty,
        t.FechaCreacion,
        t.FechaActualizacion,
        t.InfoAdicional
    );
}
