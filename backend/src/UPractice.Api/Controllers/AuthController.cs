using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Api.Seguridad;
using UPractice.Application.Features.Auth;
using UPractice.Application.Features.Auth.Empresa;

namespace UPractice.Api.Controllers;

/// <summary>Autenticación (HU-01, D-17). Los registros públicos los agregan HU-01 y HU-E01.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Registra una empresa, su administrador y solicitudes de habilitación iniciales.</summary>
    [HttpPost("registro/empresa")]
    [EndpointSummary("Registra una empresa y solicita sus habilitaciones iniciales")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> RegistrarEmpresa(
        RegistroEmpresaRequest request,
        [FromServices] RegistroEmpresaHandler handler,
        CancellationToken ct)
    {
        var respuesta = await handler.EjecutarAsync(request, ct);
        return Created("/api/v1/auth/me", respuesta);
    }

    /// <summary>Registra una cuenta de estudiante y devuelve una sesión lista para usar.</summary>
    [HttpPost("registro/estudiante")]
    [EndpointSummary("Registra una cuenta de estudiante")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LoginResponse>> RegistrarEstudiante(
        RegistroEstudianteRequest request,
        [FromServices] RegistroEstudianteHandler handler,
        CancellationToken ct)
    {
        var respuesta = await handler.EjecutarAsync(request, ct);
        return Created("/api/v1/auth/me", respuesta);
    }

    /// <summary>Inicia sesión y devuelve el token cifrado y los datos del usuario.</summary>
    [HttpPost("login")]
    [EndpointSummary("Inicia sesión")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<LoginResponse> Login(LoginRequest request, [FromServices] LoginHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(request, ct);

    /// <summary>Datos del usuario actual (el frontend no puede leer el token).</summary>
    [HttpGet("me")]
    [EndpointSummary("Obtiene el usuario autenticado")]
    [Authorize]
    [PermitirPasswordTemporal]
    [ProducesResponseType<UsuarioActualDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<UsuarioActualDto> Me([FromServices] ObtenerUsuarioActualHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(ct);

    /// <summary>Cambia la contraseña y devuelve un token nuevo (obligatorio si la cuenta tiene contraseña temporal).</summary>
    [HttpPut("password")]
    [EndpointSummary("Cambia la contraseña")]
    [PermitirPasswordTemporal]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<LoginResponse> CambiarPassword(CambiarPasswordRequest request, [FromServices] CambiarPasswordHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(request, ct);
}
