using GestionTareas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestionTareas.Infrastructure.Data;

/// <summary>
/// Permite a dotnet-ef crear el DbContext en tiempo de diseno
/// sin necesidad de levantar el host completo.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=DESKTOP-PELKD5V\\MSSQLSERVER2;Database=GestionTareas;User Id=sa;Password=YOUR_PASSWORD_HERE;TrustServerCertificate=True;");
        return new AppDbContext(optionsBuilder.Options);
    }
}
