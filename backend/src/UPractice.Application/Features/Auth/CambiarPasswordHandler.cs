using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Auth;

/// <summary>
/// PUT /auth/password (D-17). Quita la marca de contraseña temporal y devuelve un token nuevo sin "pwd_temp".
/// </summary>
public sealed class CambiarPasswordHandler(IAppDbContext db, ICurrentUser actual, IHasherPassword hasher, IGeneradorToken tokens)
{
    public async Task<LoginResponse> EjecutarAsync(CambiarPasswordRequest request, CancellationToken ct)
    {
        var usuario = await db.Usuarios
            .Include(u => u.Empresa)
            .Include(u => u.Universidad)
            .FirstOrDefaultAsync(u => u.Id == actual.UsuarioId, ct)
            ?? throw new NoAutenticadoException("La sesión ya no es válida. Inicie sesión de nuevo.");

        if (!hasher.Verificar(usuario.PasswordHash, request.PasswordActual))
            throw new ValidationException([new ValidationFailure("passwordActual", "La contraseña actual no es correcta.")]);
        if (request.PasswordNuevo == request.PasswordActual)
            throw new ValidationException([new ValidationFailure("passwordNuevo", "La nueva contraseña debe ser distinta de la actual.")]);

        usuario.PasswordHash = hasher.Hashear(request.PasswordNuevo);
        usuario.DebeCambiarPassword = false;
        await db.SaveChangesAsync(ct);

        var token = tokens.Generar(usuario);
        return new LoginResponse(token.AccessToken, token.ExpiraEn, UsuarioActualDto.Desde(usuario));
    }
}
