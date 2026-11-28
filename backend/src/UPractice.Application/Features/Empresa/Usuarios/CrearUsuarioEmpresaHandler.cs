using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun;
using UPractice.Application.Comun.Contratos;
using UPractice.Domain.Entidades;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Empresa.Usuarios;

public sealed class CrearUsuarioEmpresaHandler(IAppDbContext db, ICurrentUser actual, IHasherPassword hasher)
{
    public async Task<CuentaCreadaDto> EjecutarAsync(CrearUsuarioEmpresaRequest request, CancellationToken ct)
    {
        var empresaId = await AccesoUsuariosEmpresa.ComprobarAsync(db, actual, ct);
        var correo = request.Correo.Trim().ToLowerInvariant();

        // El correo es único en toda la plataforma, incluso para cuentas inactivas o de otro rol.
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct)) throw CorreoEnUso();

        var passwordTemporal = PasswordTemporal.Generar();
        var usuario = new Usuario
        {
            Nombre1 = request.Nombre1.Trim(),
            Nombre2 = string.IsNullOrWhiteSpace(request.Nombre2) ? null : request.Nombre2.Trim(),
            Apellido1 = request.Apellido1.Trim(),
            Apellido2 = string.IsNullOrWhiteSpace(request.Apellido2) ? null : request.Apellido2.Trim(),
            Correo = correo,
            Rol = request.Rol,
            EmpresaId = empresaId,
            Activo = true,
            DebeCambiarPassword = true,
            PasswordHash = hasher.Hashear(passwordTemporal),
        };

        db.Usuarios.Add(usuario);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // La restricción única resuelve también dos solicitudes concurrentes con el mismo correo.
            if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct)) throw CorreoEnUso();
            throw;
        }

        return new CuentaCreadaDto(usuario.Id, usuario.Correo, passwordTemporal);
    }

    private static ConflictoException CorreoEnUso() =>
        new(CodigosError.CorreoEnUso, "El correo ya está registrado.");
}
