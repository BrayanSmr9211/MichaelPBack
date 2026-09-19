using GestionTareas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionTareas.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario>  Usuarios  => Set<Usuario>();
    public DbSet<Tarea>    Tareas    => Set<Tarea>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.FechaCreacion).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");
        });

        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Descripcion).HasMaxLength(1000);
            entity.Property(t => t.Estado).IsRequired().HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.FechaCreacion).IsRequired();
            entity.Property(t => t.InfoAdicional).HasColumnType("nvarchar(max)");
            entity.HasIndex(t => t.UsuarioId).HasDatabaseName("IX_Tasks_UsuarioId");
            entity.HasIndex(t => t.Estado).HasDatabaseName("IX_Tasks_Estado");
            entity.HasOne(t => t.Usuario).WithMany(u => u.Tareas).HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.TipoEvento).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Descripcion).IsRequired().HasMaxLength(500);
            entity.Property(a => a.PayloadJson).HasColumnType("nvarchar(max)");
            entity.Property(a => a.FechaRegistro).IsRequired();
            entity.HasIndex(a => a.TareaId).HasDatabaseName("IX_AuditLogs_TareaId");
            entity.HasIndex(a => a.TipoEvento).HasDatabaseName("IX_AuditLogs_TipoEvento");
        });
    }
}
