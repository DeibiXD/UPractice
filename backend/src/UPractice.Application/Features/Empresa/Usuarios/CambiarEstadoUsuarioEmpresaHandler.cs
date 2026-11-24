using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Empresa.Usuarios;

public sealed class CambiarEstadoUsuarioEmpresaHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task EjecutarAsync(Guid id, CambiarEstadoUsuarioRequest request, CancellationToken ct)
    {
        var empresaId = await AccesoUsuariosEmpresa.ComprobarAsync(db, actual, ct);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Id == id && u.EmpresaId == empresaId
            && (u.Rol == Rol.AdminEmpresa || u.Rol == Rol.Reclutador), ct)
            ?? throw new NoEncontradoException("Usuario no encontrado.");

        if (!request.Activo && id == actual.UsuarioId)
            throw new ReglaNegocioException(CodigosError.TransicionInvalida,
                "No puede desactivar su propia cuenta.");

        usuario.Activo = request.Activo;
        await db.SaveChangesAsync(ct);
    }
}
