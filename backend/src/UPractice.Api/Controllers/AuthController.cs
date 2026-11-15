using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Api.Seguridad;
using UPractice.Application.Features.Auth;

namespace UPractice.Api.Controllers;

/// <summary>Autenticación (HU-01, D-17). Los registros públicos los agregan HU-01 y HU-E01.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Inicia sesión y devuelve el token cifrado y los datos del usuario.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<LoginResponse> Login(LoginRequest request, [FromServices] LoginHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(request, ct);

    /// <summary>Datos del usuario actual (el frontend no puede leer el token).</summary>
    [HttpGet("me")]
    [PermitirPasswordTemporal]
    [ProducesResponseType<UsuarioActualDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<UsuarioActualDto> Me([FromServices] ObtenerUsuarioActualHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(ct);

    /// <summary>Cambia la contraseña y devuelve un token nuevo (obligatorio si la cuenta tiene contraseña temporal).</summary>
    [HttpPut("password")]
    [PermitirPasswordTemporal]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<LoginResponse> CambiarPassword(CambiarPasswordRequest request, [FromServices] CambiarPasswordHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(request, ct);
}
