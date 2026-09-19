using System.ComponentModel.DataAnnotations;
using GestionTareas.Domain.Enums;

namespace GestionTareas.Application.DTOs;

// -- Usuarios --------------------------------------------------------------

public record CrearUsuarioRequest(
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    string Nombre,

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El email no tiene formato valido")]
    [StringLength(200)]
    string Email
);

public record UsuarioResponse(
    int Id,
    string Nombre,
    string Email,
    DateTime FechaCreacion
);

// -- Tareas ----------------------------------------------------------------

public record CrearTareaRequest(
    [Required(ErrorMessage = "El titulo es obligatorio")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El titulo debe tener entre 3 y 200 caracteres")]
    string Titulo,

    [StringLength(1000)]
    string? Descripcion,

    [Required(ErrorMessage = "El usuario asignado es obligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe especificar un usuario valido")]
    int UsuarioId,

    /// <summary>
    /// JSON opcional con prioridad, fechaEstimada, etiquetas, metadata.
    /// Ejemplo: {"prioridad":"Alta","fechaEstimada":"2024-12-31","etiquetas":["backend"]}
    /// </summary>
    string? InfoAdicional
);

public record CambiarEstadoRequest(
    [Required(ErrorMessage = "El estado es obligatorio")]
    EstadoTarea NuevoEstado
);

public record TareaResponse(
    int Id,
    string Titulo,
    string? Descripcion,
    string Estado,
    int UsuarioId,
    string UsuarioNombre,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion,
    string? InfoAdicional
);

public record TareaFilterRequest(
    int? UsuarioId,
    EstadoTarea? Estado
);

// -- Auth ---------------------------------------------------------------

public record RegisterRequest(
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2)]
    string Nombre,

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress]
    [StringLength(200)]
    string Email,

    [Required(ErrorMessage = "La contrasena es obligatoria")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contrasena debe tener minimo 6 caracteres")]
    string Password
);

public record LoginRequest(
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress]
    string Email,

    [Required(ErrorMessage = "La contrasena es obligatoria")]
    string Password
);

public record AuthResponse(
    string Token,
    string Nombre,
    string Email,
    string Rol,
    DateTime Expiracion
);