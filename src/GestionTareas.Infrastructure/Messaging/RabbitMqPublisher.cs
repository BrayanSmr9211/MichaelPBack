using GestionTareas.Domain.Interfaces;
using MassTransit;

namespace GestionTareas.Infrastructure.Messaging;

/// <summary>
/// Implementacion de IMessagePublisher usando MassTransit con RabbitMQ.
/// </summary>
public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RabbitMqPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T message) where T : class
    {
        await _publishEndpoint.Publish(message);
    }
}
