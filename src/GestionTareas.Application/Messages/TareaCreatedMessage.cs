namespace GestionTareas.Application.Messages;

public record TareaCreatedMessage(
    int TareaId,
    string Titulo,
    int UsuarioId,
    string UsuarioNombre,
    string Estado,
    DateTime FechaCreacion
);
