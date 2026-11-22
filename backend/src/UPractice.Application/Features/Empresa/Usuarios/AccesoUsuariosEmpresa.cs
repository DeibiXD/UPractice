using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Empresa.Usuarios;

internal static class AccesoUsuariosEmpresa
{
    public static async Task<Guid> ComprobarAsync(IAppDbContext db, ICurrentUser actual, CancellationToken ct)
    {
        if (actual.UsuarioId is not { } usuarioId || actual.EmpresaId is not { } empresaId
            || actual.Rol != Rol.AdminEmpresa)
            throw SesionInvalida();

        // Usuarios no tiene filtro global. Revalidar también los tokens emitidos antes de una baja.
        var administradorActivo = await db.Usuarios.AsNoTracking().AnyAsync(u =>
            u.Id == usuarioId && u.EmpresaId == empresaId && u.Activo && u.Rol == Rol.AdminEmpresa
            && u.Empresa != null && u.Empresa.Activa, ct);
        if (!administradorActivo) throw SesionInvalida();

        return empresaId;
    }

    private static NoAutenticadoException SesionInvalida() =>
        new("La sesión ya no es válida. Inicie sesión de nuevo.");
}
