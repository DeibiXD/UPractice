using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Empresa.Usuarios;

public sealed class ListarUsuariosEmpresaHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<IReadOnlyList<UsuarioEmpresaDto>> EjecutarAsync(CancellationToken ct)
    {
        var empresaId = await AccesoUsuariosEmpresa.ComprobarAsync(db, actual, ct);
        var usuarios = await db.Usuarios.AsNoTracking()
            .Where(u => u.EmpresaId == empresaId && (u.Rol == Rol.AdminEmpresa || u.Rol == Rol.Reclutador))
            .OrderBy(u => u.Nombre1).ThenBy(u => u.Apellido1).ThenBy(u => u.Id)
            .Select(u => new { u.Id, u.Nombre1, u.Nombre2, u.Apellido1, u.Apellido2, u.Correo, u.Rol, u.Activo })
            .ToListAsync(ct);

        return usuarios.Select(u => new UsuarioEmpresaDto(u.Id,
            Usuario.ArmarNombreCompleto(u.Nombre1, u.Nombre2, u.Apellido1, u.Apellido2),
            u.Correo, u.Rol, u.Activo)).ToArray();
    }
}
