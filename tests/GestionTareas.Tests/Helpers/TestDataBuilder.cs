using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Enums;

namespace GestionTareas.Tests.Helpers;

/// <summary>
/// Fabrica de objetos de prueba reutilizables en todos los tests.
/// </summary>
public static class TestDataBuilder
{
    public static Usuario BuildUsuario(int id = 1, string nombre = "Carlos Perez", string email = "carlos@test.com")
        => new()
        {
            Id = id,
            Nombre = nombre,
            Email = email,
            FechaCreacion = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    public static Tarea BuildTarea(
        int id = 1,
        string titulo = "Tarea de prueba",
        EstadoTarea estado = EstadoTarea.Pending,
        int usuarioId = 1,
        Usuario? usuario = null,
        string? infoAdicional = null)
        => new()
        {
            Id = id,
            Titulo = titulo,
            Descripcion = "Descripcion de prueba",
            Estado = estado,
            UsuarioId = usuarioId,
            Usuario = usuario,
            FechaCreacion = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            InfoAdicional = infoAdicional
        };
}
