namespace GestionTareas.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "User";
    public DateTime FechaCreacion { get; set; }

    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}