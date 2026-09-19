using System.Text.Json;
using GestionTareas.Application.Messages;
using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GestionTareas.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer del evento TareaCompleted (estado Done).
/// Responsabilidades:
///   1. Persistir auditoria de cierre con resumen completo.
///   2. Simular email de confirmacion de tarea completada.
///   3. Extraer prioridad del JSON si existe (demostracion de JSON_VALUE en .NET).
/// </summary>
public class TareaCompletedConsumer : IConsumer<TareaCompletedMessage>
{
    private readonly IAuditLogRepository _auditRepo;
    private readonly ILogger<TareaCompletedConsumer> _logger;

    public TareaCompletedConsumer(
        IAuditLogRepository auditRepo,
        ILogger<TareaCompletedConsumer> logger)
    {
        _auditRepo = auditRepo;
        _logger    = logger;
    }

    public async Task Consume(ConsumeContext<TareaCompletedMessage> context)
    {
        var msg = context.Message;

        // Extraer prioridad del InfoAdicional si el JSON lo contiene
        var prioridad = ExtraerPrioridad(msg.InfoAdicional);

        _logger.LogInformation(
            "[QUEUE] TareaCompletada -> Id={TareaId} | Titulo={Titulo} | " +
            "Usuario={UsuarioNombre} | Prioridad={Prioridad} | Completada={Fecha}",
            msg.TareaId, msg.Titulo, msg.UsuarioNombre, prioridad, msg.FechaCompletada);

        // 1. Persistir auditoria de cierre
        await _auditRepo.AddAsync(new AuditLog
        {
            TipoEvento  = "TareaCompletada",
            TareaId     = msg.TareaId,
            Descripcion = $"Tarea '{msg.Titulo}' completada por {msg.UsuarioNombre}. Prioridad: {prioridad}.",
            PayloadJson = JsonSerializer.Serialize(new
            {
                msg.TareaId,
                msg.Titulo,
                msg.Descripcion,
                msg.UsuarioId,
                msg.UsuarioNombre,
                msg.FechaCompletada,
                Prioridad = prioridad,
                InfoAdicionalOriginal = msg.InfoAdicional
            }),
            FechaRegistro = DateTime.UtcNow
        });

        // 2. Simular email de confirmacion al usuario
        await SimularEmailCierreAsync(msg, prioridad);
    }

    /// <summary>
    /// Extrae el campo "prioridad" del JSON de InfoAdicional usando System.Text.Json.
    /// Equivalente en SQL Server: JSON_VALUE(InfoAdicional, '$.prioridad')
    /// </summary>
    private static string ExtraerPrioridad(string? infoAdicional)
    {
        if (string.IsNullOrWhiteSpace(infoAdicional))
            return "No especificada";

        try
        {
            using var doc = JsonDocument.Parse(infoAdicional);
            if (doc.RootElement.TryGetProperty("prioridad", out var prop))
                return prop.GetString() ?? "No especificada";
        }
        catch (JsonException)
        {
            // JSON invalido: no bloquear el consumer, solo ignorar
        }

        return "No especificada";
    }

    private Task SimularEmailCierreAsync(TareaCompletedMessage msg, string prioridad)
    {
        _logger.LogInformation(
            "[EMAIL-SIM] Para: usuario-id={UsuarioId} ({UsuarioNombre}) | " +
            "Asunto: Tarea completada: '{Titulo}' | " +
            "Cuerpo: La tarea '{Titulo}' (Id={TareaId}, Prioridad={Prioridad}) " +
            "fue marcada como completada el {Fecha}. Buen trabajo!",
            msg.UsuarioId, msg.UsuarioNombre,
            msg.Titulo, msg.Titulo,
            msg.TareaId, prioridad, msg.FechaCompletada);

        return Task.CompletedTask;
    }
}