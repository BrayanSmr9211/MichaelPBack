using System.Text.Json;
using GestionTareas.Application.Messages;
using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GestionTareas.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer del evento TareaCreada.
/// Responsabilidades:
///   1. Persistir registro de auditoria en BD.
///   2. Simular envio de email de notificacion al usuario asignado.
/// </summary>
public class TareaCreatedConsumer : IConsumer<TareaCreatedMessage>
{
    private readonly IAuditLogRepository _auditRepo;
    private readonly ILogger<TareaCreatedConsumer> _logger;

    public TareaCreatedConsumer(
        IAuditLogRepository auditRepo,
        ILogger<TareaCreatedConsumer> logger)
    {
        _auditRepo = auditRepo;
        _logger    = logger;
    }

    public async Task Consume(ConsumeContext<TareaCreatedMessage> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "[QUEUE] TareaCreada recibida -> Id={TareaId} | Titulo={Titulo} | Usuario={UsuarioNombre}",
            msg.TareaId, msg.Titulo, msg.UsuarioNombre);

        // 1. Persistir auditoria
        await _auditRepo.AddAsync(new AuditLog
        {
            TipoEvento    = "TareaCreada",
            TareaId       = msg.TareaId,
            Descripcion   = $"Tarea '{msg.Titulo}' creada y asignada a {msg.UsuarioNombre}",
            PayloadJson   = JsonSerializer.Serialize(msg),
            FechaRegistro = DateTime.UtcNow
        });

        // 2. Simular notificacion por email al usuario asignado
        await SimularEnvioEmailAsync(msg);
    }

    private Task SimularEnvioEmailAsync(TareaCreatedMessage msg)
    {
        // En produccion aqui iria: SmtpClient, SendGrid, AWS SES, etc.
        _logger.LogInformation(
            "[EMAIL-SIM] Para: usuario-id={UsuarioId} ({UsuarioNombre}) | " +
            "Asunto: Nueva tarea asignada: '{Titulo}' | " +
            "Cuerpo: Se te ha asignado la tarea '{Titulo}' (Id={TareaId}). Estado inicial: {Estado}.",
            msg.UsuarioId, msg.UsuarioNombre, msg.Titulo, msg.Titulo, msg.TareaId, msg.Estado);

        return Task.CompletedTask;
    }
}