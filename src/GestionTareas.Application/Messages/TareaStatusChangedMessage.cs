namespace GestionTareas.Application.Messages;

public record TareaStatusChangedMessage(
    int TareaId,
    string Titulo,
    int UsuarioId,
    string EstadoAnterior,
    string EstadoNuevo,
    DateTime FechaCambio
);
