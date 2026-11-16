using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Auth;

/// <summary>Registra un estudiante asociado al dominio de una universidad activa.</summary>
public sealed class RegistroEstudianteHandler(
    IAppDbContext db,
    IHasherPassword hasher,
    IGeneradorToken tokens)
{
    public async Task<LoginResponse> EjecutarAsync(RegistroEstudianteRequest request, CancellationToken ct)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct))
            throw new ConflictoException("CORREO_EN_USO", "El correo ya está registrado.");

        var dominio = correo[(correo.LastIndexOf('@') + 1)..];
        var universidad = await db.Universidades
            .FirstOrDefaultAsync(u => u.Activa && u.DominioCorreo.ToLower() == dominio, ct);

        if (universidad is null)
            throw new ReglaNegocioException("CORREO_NO_INSTITUCIONAL", "El correo debe pertenecer a una universidad activa.");

        var usuario = new Usuario
        {
            Correo = correo,
            PasswordHash = hasher.Hashear(request.Password),
            Nombre1 = request.Nombre1.Trim(),
            Nombre2 = string.IsNullOrWhiteSpace(request.Nombre2) ? null : request.Nombre2.Trim(),
            Apellido1 = request.Apellido1.Trim(),
            Apellido2 = string.IsNullOrWhiteSpace(request.Apellido2) ? null : request.Apellido2.Trim(),
            Rol = Rol.Estudiante,
            UniversidadId = universidad.Id,
            Universidad = universidad,
        };

        db.Usuarios.Add(usuario);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct))
                throw new ConflictoException("CORREO_EN_USO", "El correo ya está registrado.");
            throw;
        }

        var token = tokens.Generar(usuario);
        return new LoginResponse(token.AccessToken, token.ExpiraEn, UsuarioActualDto.Desde(usuario));
    }
}
