using System.Security.Claims;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Enums;

namespace UPractice.Api.Seguridad;

/// <summary>Lee los claims del token ya descifrado y validado por JwtBearer (S3.1).</summary>
public sealed class CurrentUser(IHttpContextAccessor accesor) : ICurrentUser
{
    private ClaimsPrincipal? Usuario => accesor.HttpContext?.User;

    public Guid? UsuarioId => LeerGuid("sub");
    public Rol? Rol => Enum.TryParse<Rol>(Usuario?.FindFirstValue("role"), out var rol) ? rol : null;
    public Guid? EmpresaId => LeerGuid("empresa_id");
    public Guid? UniversidadId => LeerGuid("universidad_id");

    private Guid? LeerGuid(string claim) =>
        Usuario?.Identity?.IsAuthenticated == true && Guid.TryParse(Usuario.FindFirstValue(claim), out var id) ? id : null;
}
