using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Auth;

/// <summary>POST /auth/login (HU-01). Cualquier fallo responde 401 con el mismo mensaje genérico.</summary>
public sealed class LoginHandler(IAppDbContext db, IHasherPassword hasher, IGeneradorToken tokens)
{
    // Hash de una contraseña aleatoria: se verifica contra él cuando el correo no existe,
    // para que la respuesta tarde lo mismo y no revele qué correos están registrados.
    private static string? hashFicticio;

    public async Task<LoginResponse> EjecutarAsync(LoginRequest request, CancellationToken ct)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios
            .Include(u => u.Empresa)
            .Include(u => u.Universidad)
            .FirstOrDefaultAsync(u => u.Correo == correo, ct);

        var passwordValida = hasher.Verificar(usuario?.PasswordHash ?? (hashFicticio ??= hasher.Hashear(Guid.NewGuid().ToString())), request.Password);
        if (usuario is null || !passwordValida || !usuario.Activo || usuario.Empresa is { Activa: false })
            throw new NoAutenticadoException();

        var token = tokens.Generar(usuario);
        return new LoginResponse(token.AccessToken, token.ExpiraEn, UsuarioActualDto.Desde(usuario));
    }
}
