namespace GestionTareas.Application.Messages;

/// <summary>
/// Publicado cuando una tarea alcanza el estado Done.
/// Permite disparar logica de cierre: reportes, notificaciones finales, etc.
/// </summary>
public record TareaCompletedMessage(
    int TareaId,
    string Titulo,
    string? Descripcion,
    int UsuarioId,
    string UsuarioNombre,
    DateTime FechaCompletada,
    string? InfoAdicional
);