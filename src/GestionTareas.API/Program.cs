using System.Text;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Azure Key Vault (activa si VaultUri esta configurado) ───────────────────
// Secretos recomendados en Key Vault:
//   JwtSettings--SecretKey, ConnectionStrings--DefaultConnection,
//   RabbitMQ--Username, RabbitMQ--Password
var vaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(vaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(vaultUri), new DefaultAzureCredential());
}

// ── SQL Server ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository,  UsuarioRepository>();
builder.Services.AddScoped<ITareaRepository,    TareaRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ITareaService,   TareaService>();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"] ?? "GestionTareas_SuperSecretKey_2024_MinLength32Chars!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings["Issuer"]   ?? "GestionTareas",
        ValidAudience            = jwtSettings["Audience"] ?? "GestionTareasApp",
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ── Messaging – RabbitMQ con MassTransit ─────────────────────────────────────
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

var rabbit = builder.Configuration.GetSection("RabbitMQ");
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumer<TareaCreatedConsumer>();
    cfg.AddConsumer<TareaStatusChangedConsumer>();
    cfg.AddConsumer<TareaCompletedConsumer>();

    cfg.UsingRabbitMq((ctx, rmq) =>
    {
        rmq.Host(rabbit["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbit["Username"] ?? "guest");
            h.Password(rabbit["Password"] ?? "guest");
        });
        rmq.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        rmq.ConfigureEndpoints(ctx);
    });
});

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger con soporte JWT ───────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "GestionTareas API",
        Version     = "v1",
        Description = "API REST para gestion de tareas. Usar /api/auth/login para obtener el JWT."
    });

    // Boton Authorize en Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT en el header. Formato: Bearer {token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── Migraciones automaticas ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GestionTareas API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.UseAuthentication();   // <-- antes de UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
