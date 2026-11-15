using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Auth;

/// <summary>GET /auth/me (HU-01). El frontend no puede leer el token cifrado, así que pide aquí sus datos.</summary>
public sealed class ObtenerUsuarioActualHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<UsuarioActualDto> EjecutarAsync(CancellationToken ct)
    {
        var usuario = await db.Usuarios
            .Include(u => u.Empresa)
            .Include(u => u.Universidad)
            .FirstOrDefaultAsync(u => u.Id == actual.UsuarioId, ct);

        if (usuario is null || !usuario.Activo || usuario.Empresa is { Activa: false })
            throw new NoAutenticadoException("La sesión ya no es válida. Inicie sesión de nuevo.");

        return UsuarioActualDto.Desde(usuario);
    }
}
