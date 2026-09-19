using FluentAssertions;
using GestionTareas.Application.DTOs;
using GestionTareas.Application.Services;
using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Exceptions;
using GestionTareas.Domain.Interfaces;
using GestionTareas.Tests.Helpers;
using Moq;

namespace GestionTareas.Tests.Services;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _repoMock = new();
    private readonly UsuarioService           _sut;

    public UsuarioServiceTests()
    {
        _sut = new UsuarioService(_repoMock.Object);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ListarUsuariosAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ListarUsuariosAsync_HayUsuarios_DevuelveTodosMapeados()
    {
        var usuarios = new List<Usuario>
        {
            TestDataBuilder.BuildUsuario(id: 1, nombre: "Carlos Perez",   email: "carlos@test.com"),
            TestDataBuilder.BuildUsuario(id: 2, nombre: "Maria Lopez",    email: "maria@test.com"),
            TestDataBuilder.BuildUsuario(id: 3, nombre: "Juan Rodriguez", email: "juan@test.com")
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(usuarios);

        var resultado = await _sut.ListarUsuariosAsync();

        resultado.Should().HaveCount(3);
        resultado.Select(u => u.Nombre).Should()
            .Contain(new[] { "Carlos Perez", "Maria Lopez", "Juan Rodriguez" });
    }

    [Fact]
    public async Task ListarUsuariosAsync_SinUsuarios_DevuelveListaVacia()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Usuario>());

        var resultado = await _sut.ListarUsuariosAsync();

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarUsuariosAsync_MapeaCorrectamenteTodosLosCampos()
    {
        var fecha = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var usuario = new Usuario { Id = 7, Nombre = "Ana Gil", Email = "ana@test.com", FechaCreacion = fecha };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Usuario> { usuario });

        var resultado = await _sut.ListarUsuariosAsync();
        var response = resultado.Single();

        response.Id.Should().Be(7);
        response.Nombre.Should().Be("Ana Gil");
        response.Email.Should().Be("ana@test.com");
        response.FechaCreacion.Should().Be(fecha);
    }

    [Fact]
    public async Task ListarUsuariosAsync_SiempreLlamaAlRepositorioUnaVez()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Usuario>());

        await _sut.ListarUsuariosAsync();

        _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CrearUsuarioAsync - casos felices
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CrearUsuarioAsync_DatosValidos_DevuelveUsuarioCreado()
    {
        var request = new CrearUsuarioRequest("Carlos Perez", "carlos@test.com");
        var usuarioGuardado = TestDataBuilder.BuildUsuario(id: 1, nombre: "Carlos Perez", email: "carlos@test.com");

        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Usuario>())).ReturnsAsync(usuarioGuardado);

        var resultado = await _sut.CrearUsuarioAsync(request);

        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(1);
        resultado.Nombre.Should().Be("Carlos Perez");
        resultado.Email.Should().Be("carlos@test.com");
    }

    [Fact]
    public async Task CrearUsuarioAsync_EmailSeGuardaEnMinusculas()
    {
        var request = new CrearUsuarioRequest("Carlos Perez", "CARLOS@TEST.COM");
        Usuario? usuarioCapturado = null;
        var usuarioGuardado = TestDataBuilder.BuildUsuario(email: "carlos@test.com");

        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => usuarioCapturado = u)
            .ReturnsAsync(usuarioGuardado);

        await _sut.CrearUsuarioAsync(request);

        usuarioCapturado!.Email.Should().Be("carlos@test.com");
    }

    [Fact]
    public async Task CrearUsuarioAsync_NombreSeGuardaSinEspaciosSobrantes()
    {
        var request = new CrearUsuarioRequest("  Carlos Perez  ", "carlos@test.com");
        Usuario? usuarioCapturado = null;
        var usuarioGuardado = TestDataBuilder.BuildUsuario(nombre: "Carlos Perez");

        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => usuarioCapturado = u)
            .ReturnsAsync(usuarioGuardado);

        await _sut.CrearUsuarioAsync(request);

        usuarioCapturado!.Nombre.Should().Be("Carlos Perez");
    }

    [Fact]
    public async Task CrearUsuarioAsync_DatosValidos_LlamaCreateAsyncUnaVez()
    {
        var request = new CrearUsuarioRequest("Carlos Perez", "carlos@test.com");
        var usuarioGuardado = TestDataBuilder.BuildUsuario();

        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Usuario>())).ReturnsAsync(usuarioGuardado);

        await _sut.CrearUsuarioAsync(request);

        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Usuario>()), Times.Once);
    }

    [Fact]
    public async Task CrearUsuarioAsync_DatosValidos_VerificaEmailAntesDeCrear()
    {
        var request = new CrearUsuarioRequest("Carlos Perez", "carlos@test.com");
        var usuarioGuardado = TestDataBuilder.BuildUsuario();

        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Usuario>())).ReturnsAsync(usuarioGuardado);

        await _sut.CrearUsuarioAsync(request);

        _repoMock.Verify(r => r.ExisteEmailAsync(It.IsAny<string>()), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CrearUsuarioAsync - reglas de negocio
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CrearUsuarioAsync_EmailDuplicado_LanzaBusinessRuleException()
    {
        var request = new CrearUsuarioRequest("Otro Usuario", "carlos@test.com");
        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(true);

        var act = async () => await _sut.CrearUsuarioAsync(request);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*carlos@test.com*");
    }

    [Fact]
    public async Task CrearUsuarioAsync_EmailDuplicado_NuncaLlamaCreateAsync()
    {
        var request = new CrearUsuarioRequest("Otro Usuario", "duplicado@test.com");
        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(true);

        try { await _sut.CrearUsuarioAsync(request); } catch { }

        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Theory]
    [InlineData("CARLOS@TEST.COM")]
    [InlineData("Carlos@Test.Com")]
    [InlineData("  carlos@test.com  ")]
    public async Task CrearUsuarioAsync_EmailDuplicadoConDistintoCasing_LanzaBusinessRuleException(
        string emailConVariante)
    {
        // El servicio pasa el email sin normalizar a ExisteEmailAsync;
        // la normalizacion la hace el repositorio. Simulamos que el repo
        // devuelve true (ya existe) para cualquier variante del email.
        var request = new CrearUsuarioRequest("Otro Usuario", emailConVariante);
        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(true);

        var act = async () => await _sut.CrearUsuarioAsync(request);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CrearUsuarioAsync_DatosValidos_FechaCreacionEsUtc()
    {
        var request = new CrearUsuarioRequest("Carlos Perez", "carlos@test.com");
        Usuario? usuarioCapturado = null;
        var usuarioGuardado = TestDataBuilder.BuildUsuario();

        _repoMock.Setup(r => r.ExisteEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => usuarioCapturado = u)
            .ReturnsAsync(usuarioGuardado);

        await _sut.CrearUsuarioAsync(request);

        usuarioCapturado!.FechaCreacion.Kind.Should().Be(DateTimeKind.Utc);
    }
}