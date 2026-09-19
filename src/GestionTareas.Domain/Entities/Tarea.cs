using GestionTareas.Domain.Enums;

namespace GestionTareas.Domain.Entities;

public class Tarea
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public EstadoTarea Estado { get; set; } = EstadoTarea.Pending;
    public int UsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    /// <summary>
    /// JSON adicional: prioridad, fechaEstimada, etiquetas, metadata.
    /// </summary>
    public string? InfoAdicional { get; set; }
    public Usuario? Usuario { get; set; }
}
