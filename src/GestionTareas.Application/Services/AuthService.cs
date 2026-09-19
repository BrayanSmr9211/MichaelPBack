using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionTareas.Application.DTOs;
using GestionTareas.Application.Interfaces;
using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Exceptions;
using GestionTareas.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GestionTareas.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _repo;
    private readonly IConfiguration     _config;

    public AuthService(IUsuarioRepository repo, IConfiguration config)
    {
        _repo   = repo;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _repo.ExisteEmailAsync(request.Email))
            throw new BusinessRuleException(
                string.Format("Ya existe una cuenta registrada con el email '{0}'.", request.Email));

        var usuario = new Usuario
        {
            Nombre        = request.Nombre.Trim(),
            Email         = request.Email.Trim().ToLower(),
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol           = "User",
            FechaCreacion = DateTime.UtcNow
        };

        var creado = await _repo.CreateAsync(usuario);
        return GenerarToken(creado);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var usuario = await _repo.GetByEmailAsync(request.Email.Trim().ToLower())
            ?? throw new BusinessRuleException("Email o contrasena incorrectos.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            throw new BusinessRuleException("Email o contrasena incorrectos.");

        return GenerarToken(usuario);
    }

    private AuthResponse GenerarToken(Usuario usuario)
    {
        var jwt       = _config.GetSection("JwtSettings");
        var secretKey = jwt["SecretKey"] ?? "GestionTareas_SuperSecretKey_2024_MinLength32Chars!";
        var issuer    = jwt["Issuer"]    ?? "GestionTareas";
        var audience  = jwt["Audience"]  ?? "GestionTareasApp";
        var expHours  = int.TryParse(jwt["ExpirationHours"], out var h) ? h : 8;

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var exp   = DateTime.UtcNow.AddHours(expHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(JwtRegisteredClaimNames.Name,  usuario.Nombre),
            new Claim(ClaimTypes.Role,               usuario.Rol),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            exp,
            signingCredentials: creds);

        return new AuthResponse(
            Token:      new JwtSecurityTokenHandler().WriteToken(token),
            Nombre:     usuario.Nombre,
            Email:      usuario.Email,
            Rol:        usuario.Rol,
            Expiracion: exp);
    }
}