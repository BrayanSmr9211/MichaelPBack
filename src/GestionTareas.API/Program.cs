using Azure.Identity;
using GestionTareas.API.Middleware;
using GestionTareas.Application.Interfaces;
using GestionTareas.Application.Services;
using GestionTareas.Domain.Interfaces;
using GestionTareas.Infrastructure.Data;
using GestionTareas.Infrastructure.Messaging;
using GestionTareas.Infrastructure.Messaging.Consumers;
using GestionTareas.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// -- Azure Key Vault (si VaultUri esta configurado) --------------------------
// Los secretos en Key Vault sobreescriben los valores de appsettings.
// Nombres de secretos sugeridos: ConnectionStrings--DefaultConnection,
//   RabbitMQ--Username, RabbitMQ--Password
var vaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(vaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(vaultUri),
        new DefaultAzureCredential());
}

// -- Base de datos – SQL Server -----------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -- Repositories -------------------------------------------------------------
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ITareaRepository, TareaRepository>();

// -- Services ------------------------------------------------------------------
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ITareaService, TareaService>();

// -- Messaging – RabbitMQ con MassTransit -------------------------------------
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

var rabbit = builder.Configuration.GetSection("RabbitMQ");
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumer<TareaCreatedConsumer>();
    cfg.AddConsumer<TareaStatusChangedConsumer>();

    cfg.UsingRabbitMq((ctx, rmq) =>
    {
        rmq.Host(rabbit["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbit["Username"] ?? "guest");
            h.Password(rabbit["Password"] ?? "guest");
        });

        rmq.ConfigureEndpoints(ctx);
    });
});

// -- Controllers ---------------------------------------------------------------
builder.Services.AddControllers();

// -- Swagger -------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GestionTareas API",
        Version = "v1",
        Description = "API REST para gestion de tareas y usuarios"
    });
});

// -- CORS – permite cualquier origen para conectar con el front Angular --------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// -- Migraciones / creacion automatica de BD al iniciar -----------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// -- Middleware pipeline -------------------------------------------------------
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GestionTareas API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raiz "/"
});

app.UseCors("AllowAll");
app.MapControllers();

app.Run();

public partial class Program { }
