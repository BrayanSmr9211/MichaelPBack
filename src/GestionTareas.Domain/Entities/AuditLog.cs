namespace GestionTareas.Domain.Entities;

/// <summary>
/// Registro de auditoria generado por los consumers de RabbitMQ.
/// Cada evento procesado desde la cola queda trazado en esta tabla.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>Tipo de evento: TareaCreada, EstadoCambiado, TareaCompletada</summary>
    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>Id de la tarea involucrada</summary>
    public int TareaId { get; set; }

    /// <summary>Descripcion legible del evento procesado</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>JSON con el payload completo del mensaje recibido</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }
}