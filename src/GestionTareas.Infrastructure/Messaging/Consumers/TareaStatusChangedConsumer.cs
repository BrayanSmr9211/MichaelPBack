using GestionTareas.Application.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GestionTareas.Infrastructure.Messaging.Consumers;

public class TareaStatusChangedConsumer : IConsumer<TareaStatusChangedMessage>
{
    private readonly ILogger<TareaStatusChangedConsumer> _logger;

    public TareaStatusChangedConsumer(ILogger<TareaStatusChangedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TareaStatusChangedMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Estado de tarea cambiado: Id={TareaId} | Titulo={Titulo} | {EstadoAnterior} -> {EstadoNuevo} | Fecha={Fecha}",
            msg.TareaId, msg.Titulo, msg.EstadoAnterior, msg.EstadoNuevo, msg.FechaCambio);
        // Aqui se podria disparar logica adicional: auditorias, webhooks, etc.
        return Task.CompletedTask;
    }
}
