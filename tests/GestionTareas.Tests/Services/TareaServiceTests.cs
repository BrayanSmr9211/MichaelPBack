using FluentAssertions;
using GestionTareas.Application.DTOs;
using GestionTareas.Application.Messages;
using GestionTareas.Application.Services;
using GestionTareas.Domain.Entities;
using GestionTareas.Domain.Enums;
using GestionTareas.Domain.Exceptions;
using GestionTareas.Domain.Interfaces;
using GestionTareas.Tests.Helpers;
using Moq;

namespace GestionTareas.Tests.Services;

public class TareaServiceTests
{
    // -- Mocks compartidos ----------------------------------------------------
    private readonly Mock<ITareaRepository>    _tareaRepoMock    = new();
    private readonly Mock<IUsuarioRepository>  _usuarioRepoMock  = new();
    private readonly Mock<IMessagePublisher>   _publisherMock    = new();
    private readonly TareaService              _sut;

    public TareaServiceTests()
    {
        _sut = new TareaService(
            _tareaRepoMock.Object,
            _usuarioRepoMock.Object,
            _publisherMock.Object);
    }

    // ------------------------------------------------------------------------
    //  ListarTareasAsync
    // ------------------------------------------------------------------------

    [Fact]
    public async Task ListarTareasAsync_SinFiltros_DevuelveTodosLosRegistros()
    {
        // Arrange
        var usuario = TestDataBuilder.BuildUsuario();
        var tareas = new List<Tarea>
        {
            TestDataBuilder.BuildTarea(id: 1, estado: EstadoTarea.Pending,    usuario: usuario),
            TestDataBuilder.BuildTarea(id: 2, estado: EstadoTarea.InProgress, usuario: usuario),
            TestDataBuilder.BuildTarea(id: 3, estado: EstadoTarea.Done,       usuario: usuario)
        };
        _tareaRepoMock
            .Setup(r => r.GetAllAsync(null, null))
            .ReturnsAsync(tareas);

        // Act
        var resultado = await _sut.ListarTareasAsync(null, null);

        // Assert
        resultado.Should().HaveCount(3);
        _tareaRepoMock.Verify(r => r.GetAllAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task ListarTareasAsync_FiltrandoPorEstado_SoloDevuelveTareasConEseEstado()
    {
        // Arrange
        var usuario = TestDataBuilder.BuildUsuario();
        var tareasPending = new List<Tarea>
        {
            TestDataBuilder.BuildTarea(id: 1, estado: EstadoTarea.Pending, usuario: usuario)
        };
        _tareaRepoMock
            .Setup(r => r.GetAllAsync(null, EstadoTarea.Pending))
            .ReturnsAsync(tareasPending);

        // Act
        var resultado = await _sut.ListarTareasAsync(null, EstadoTarea.Pending);

        // Assert
        resultado.Should().HaveCount(1);
        resultado.First().Estado.Should().Be("Pending");
    }

    [Fact]
    public async Task ListarTareasAsync_SinTareas_DevuelveListaVacia()
    {
        // Arrange
        _tareaRepoMock
            .Setup(r => r.GetAllAsync(null, null))
            .ReturnsAsync(new List<Tarea>());

        // Act
        var resultado = await _sut.ListarTareasAsync(null, null);

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarTareasAsync_FiltrandoPorUsuario_LlamARepositorioConElIdCorrecto()
    {
        // Arrange
        var usuario = TestDataBuilder.BuildUsuario(id: 5);
        _tareaRepoMock
            .Setup(r => r.GetAllAsync(5, null))
            .ReturnsAsync(new List<Tarea> { TestDataBuilder.BuildTarea(usuarioId: 5, usuario: usuario) });

        // Act
        var resultado = await _sut.ListarTareasAsync(5, null);

        // Assert
        resultado.Should().HaveCount(1);
        resultado.First().UsuarioId.Should().Be(5);
        _tareaRepoMock.Verify(r => r.GetAllAsync(5, null), Times.Once);
    }

    // ------------------------------------------------------------------------
    //  CrearTareaAsync — casos felices
    // ------------------------------------------------------------------------

    [Fact]
    public async Task CrearTareaAsync_DatosValidos_DevuelveTareaCreada()
    {
        // Arrange
        var usuario = TestDataBuilder.BuildUsuario();
        var request = new CrearTareaRequest("Nueva tarea", "Descripcion", 1, null);
        var tareaCreada = TestDataBuilder.BuildTarea(id: 10, titulo: "Nueva tarea", usuario: usuario);

        _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(usuario);
        _tareaRepoMock.Setup(r => r.CreateAsync(It.IsAny<Tarea>())).ReturnsAsync(tareaCreada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaCreatedMessage>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CrearTareaAsync(request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(10);
        resultado.Titulo.Should().Be("Nueva tarea");
        resultado.Estado.Should().Be("Pending");
        resultado.UsuarioId.Should().Be(1);
        resultado.UsuarioNombre.Should().Be("Carlos Perez");
    }

    [Fact]
    public async Task CrearTareaAsync_DatosValidos_PublicaMensajeRabbitMQ()
    {
        // Arrange
        var usuario = TestDataBuilder.BuildUsuario();
        var request = new CrearTareaRequest("Tarea con evento", null, 1, null);
        var tareaCreada = TestDataBuilder.BuildTarea(id: 1, usuario: usuario);

        _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(usuario);
        _tareaRepoMock.Setup(r => r.CreateAsync(It.IsAny<Tarea>())).ReturnsAsync(tareaCreada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaCreatedMessage>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CrearTareaAsync(request);

        // Assert — se publicó exactamente un mensaje
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<TareaCreatedMessage>()), Times.Once);
    }

    [Fact]
    public async Task CrearTareaAsync_ConInfoAdicionalJsonValido_CreaCorrectamente()
    {
        // Arrange
        var usuario = TestDataBuilder.BuildUsuario();
        var json = "{\"prioridad\":\"Alta\",\"etiquetas\":[\"backend\"]}";
        var request = new CrearTareaRequest("Tarea JSON", null, 1, json);
        var tareaCreada = TestDataBuilder.BuildTarea(id: 1, infoAdicional: json, usuario: usuario);

        _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(usuario);
        _tareaRepoMock.Setup(r => r.CreateAsync(It.IsAny<Tarea>())).ReturnsAsync(tareaCreada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaCreatedMessage>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CrearTareaAsync(request);

        // Assert
        resultado.InfoAdicional.Should().Be(json);
    }

    [Fact]
    public async Task CrearTareaAsync_SinInfoAdicional_CreaCorrectamente()
    {
        // Arrange
        var usuario = TestDataBuilder.BuildUsuario();
        var request = new CrearTareaRequest("Tarea sin JSON", null, 1, null);
        var tareaCreada = TestDataBuilder.BuildTarea(id: 1, usuario: usuario);

        _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(usuario);
        _tareaRepoMock.Setup(r => r.CreateAsync(It.IsAny<Tarea>())).ReturnsAsync(tareaCreada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaCreatedMessage>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CrearTareaAsync(request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.InfoAdicional.Should().BeNull();
    }

    // ------------------------------------------------------------------------
    //  CrearTareaAsync — reglas de negocio (casos de error)
    // ------------------------------------------------------------------------

    [Fact]
    public async Task CrearTareaAsync_UsuarioNoExiste_LanzaNotFoundException()
    {
        // Arrange
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Usuario?)null);
        var request = new CrearTareaRequest("Tarea huerfana", null, 99, null);

        // Act
        var act = async () => await _sut.CrearTareaAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CrearTareaAsync_TituloVacio_LanzaBusinessRuleException(string titulo)
    {
        // Arrange
        var request = new CrearTareaRequest(titulo, null, 1, null);

        // Act
        var act = async () => await _sut.CrearTareaAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*titulo*");
    }

    [Fact]
    public async Task CrearTareaAsync_InfoAdicionalJsonInvalido_LanzaBusinessRuleException()
    {
        // Arrange
        var usuario = TestDataBuilder.BuildUsuario();
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(usuario);
        var request = new CrearTareaRequest("Tarea", null, 1, "esto-no-es-json{{{");

        // Act
        var act = async () => await _sut.CrearTareaAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*JSON*");
    }

    [Fact]
    public async Task CrearTareaAsync_DatosValidos_NuncaLlamaAlRepositorioConTituloSinTrim()
    {
        // Arrange — titulo con espacios: debe ser guardado sin espacios sobrantes
        var usuario = TestDataBuilder.BuildUsuario();
        var request = new CrearTareaRequest("  Tarea con espacios  ", null, 1, null);
        Tarea? tareaGuardada = null;
        var tareaCreada = TestDataBuilder.BuildTarea(id: 1, titulo: "Tarea con espacios", usuario: usuario);

        _usuarioRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(usuario);
        _tareaRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Tarea>()))
            .Callback<Tarea>(t => tareaGuardada = t)
            .ReturnsAsync(tareaCreada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaCreatedMessage>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CrearTareaAsync(request);

        // Assert
        tareaGuardada!.Titulo.Should().Be("Tarea con espacios");
    }

    // ------------------------------------------------------------------------
    //  CambiarEstadoAsync — casos felices
    // ------------------------------------------------------------------------

    [Fact]
    public async Task CambiarEstadoAsync_PendingAInProgress_ActualizaEstadoCorrectamente()
    {
        // Arrange
        var tarea = TestDataBuilder.BuildTarea(id: 1, estado: EstadoTarea.Pending);
        var request = new CambiarEstadoRequest(EstadoTarea.InProgress);
        var tareaActualizada = TestDataBuilder.BuildTarea(id: 1, estado: EstadoTarea.InProgress);

        _tareaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tarea);
        _tareaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Tarea>())).ReturnsAsync(tareaActualizada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaStatusChangedMessage>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CambiarEstadoAsync(1, request);

        // Assert
        resultado.Estado.Should().Be("InProgress");
        _tareaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Tarea>()), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoAsync_InProgressADone_ActualizaEstadoCorrectamente()
    {
        // Arrange
        var tarea = TestDataBuilder.BuildTarea(id: 1, estado: EstadoTarea.InProgress);
        var request = new CambiarEstadoRequest(EstadoTarea.Done);
        var tareaActualizada = TestDataBuilder.BuildTarea(id: 1, estado: EstadoTarea.Done);

        _tareaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tarea);
        _tareaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Tarea>())).ReturnsAsync(tareaActualizada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaStatusChangedMessage>())).Returns(Task.CompletedTask);

        // Act
        var resultado = await _sut.CambiarEstadoAsync(1, request);

        // Assert
        resultado.Estado.Should().Be("Done");
    }

    [Fact]
    public async Task CambiarEstadoAsync_CambioExitoso_PublicaMensajeRabbitMQ()
    {
        // Arrange
        var tarea = TestDataBuilder.BuildTarea(estado: EstadoTarea.Pending);
        var tareaActualizada = TestDataBuilder.BuildTarea(estado: EstadoTarea.InProgress);

        _tareaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tarea);
        _tareaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Tarea>())).ReturnsAsync(tareaActualizada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaStatusChangedMessage>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CambiarEstadoAsync(1, new CambiarEstadoRequest(EstadoTarea.InProgress));

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<TareaStatusChangedMessage>()), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoAsync_CambioExitoso_EstableceFechaActualizacion()
    {
        // Arrange
        var tarea = TestDataBuilder.BuildTarea(estado: EstadoTarea.Pending);
        Tarea? tareaGuardada = null;
        var tareaActualizada = TestDataBuilder.BuildTarea(estado: EstadoTarea.InProgress);

        _tareaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tarea);
        _tareaRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Tarea>()))
            .Callback<Tarea>(t => tareaGuardada = t)
            .ReturnsAsync(tareaActualizada);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<TareaStatusChangedMessage>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CambiarEstadoAsync(1, new CambiarEstadoRequest(EstadoTarea.InProgress));

        // Assert
        tareaGuardada!.FechaActualizacion.Should().NotBeNull();
    }

    // ------------------------------------------------------------------------
    //  CambiarEstadoAsync — reglas de negocio (casos de error)
    // ------------------------------------------------------------------------

    [Fact]
    public async Task CambiarEstadoAsync_TareaNoExiste_LanzaNotFoundException()
    {
        // Arrange
        _tareaRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Tarea?)null);

        // Act
        var act = async () => await _sut.CambiarEstadoAsync(999, new CambiarEstadoRequest(EstadoTarea.InProgress));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task CambiarEstadoAsync_PendingADone_LanzaBusinessRuleException()
    {
        // Arrange — regla de negocio principal: no saltar de Pending a Done
        var tarea = TestDataBuilder.BuildTarea(estado: EstadoTarea.Pending);
        _tareaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tarea);

        // Act
        var act = async () => await _sut.CambiarEstadoAsync(1, new CambiarEstadoRequest(EstadoTarea.Done));

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Pending*Done*");
    }

    [Fact]
    public async Task CambiarEstadoAsync_PendingADone_NuncaLlamaAlRepositorioUpdate()
    {
        // Arrange
        var tarea = TestDataBuilder.BuildTarea(estado: EstadoTarea.Pending);
        _tareaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tarea);

        // Act
        try { await _sut.CambiarEstadoAsync(1, new CambiarEstadoRequest(EstadoTarea.Done)); } catch { }

        // Assert — no debe persistir nada si la regla falla
        _tareaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Tarea>()), Times.Never);
    }

    [Theory]
    [InlineData(EstadoTarea.Pending,    EstadoTarea.Pending)]
    [InlineData(EstadoTarea.InProgress, EstadoTarea.InProgress)]
    [InlineData(EstadoTarea.Done,       EstadoTarea.Done)]
    public async Task CambiarEstadoAsync_MismoEstado_LanzaBusinessRuleException(
        EstadoTarea estadoActual, EstadoTarea estadoNuevo)
    {
        // Arrange
        var tarea = TestDataBuilder.BuildTarea(estado: estadoActual);
        _tareaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tarea);

        // Act
        var act = async () => await _sut.CambiarEstadoAsync(1, new CambiarEstadoRequest(estadoNuevo));

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage($"*{estadoNuevo}*");
    }

    [Fact]
    public async Task CambiarEstadoAsync_PendingADone_NuncaPublicaMensaje()
    {
        // Arrange
        var tarea = TestDataBuilder.BuildTarea(estado: EstadoTarea.Pending);
        _tareaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tarea);

        // Act
        try { await _sut.CambiarEstadoAsync(1, new CambiarEstadoRequest(EstadoTarea.Done)); } catch { }

        // Assert
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<TareaStatusChangedMessage>()), Times.Never);
    }
}
