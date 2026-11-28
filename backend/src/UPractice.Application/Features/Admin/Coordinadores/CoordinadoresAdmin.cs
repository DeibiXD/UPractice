using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Auth;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Admin.Coordinadores;

// ===== Contratos (S5.6) =====

public sealed record CoordinadorAdminDto(Guid Id, string NombreCompleto, string Correo, Referencia Universidad, bool Activo);

public sealed record CrearCoordinadorRequest(
    Guid UniversidadId, string? Nombre1, string? Nombre2, string? Apellido1, string? Apellido2, string? Correo);

public sealed record CambiarEstadoCoordinadorRequest([property: JsonRequired] bool Activo);

public static class MensajesCoordinadores
{
    public const string Universidad = "Seleccione una universidad.";
    public const string UniversidadInvalida = "Seleccione una universidad activa.";
    public const string CorreoEnUso = "El correo ya está registrado.";
}

// ===== Validador (mismas reglas de nombres y correo que el registro y HU-E02) =====

public sealed class CrearCoordinadorValidator : AbstractValidator<CrearCoordinadorRequest>
{
    public CrearCoordinadorValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.UniversidadId).NotEmpty().WithMessage(MensajesCoordinadores.Universidad);
        RuleFor(x => Recortar(x.Nombre1)!).NombreObligatorio().OverridePropertyName("nombre1");
        RuleFor(x => Recortar(x.Nombre2)).NombreOpcional().OverridePropertyName("nombre2");
        RuleFor(x => Recortar(x.Apellido1)!).NombreObligatorio().OverridePropertyName("apellido1");
        RuleFor(x => Recortar(x.Apellido2)).NombreOpcional().OverridePropertyName("apellido2");
        RuleFor(x => Recortar(x.Correo)!).CorreoValido().OverridePropertyName("correo");
    }

    private static string? Recortar(string? valor) => valor?.Trim();
}

// ===== Handlers =====

internal static class ConsultasCoordinadores
{
    /// <summary>Usuarios no tiene filtro global (S3.2): toda consulta de esta HU se limita a Rol = Coordinador.</summary>
    public static IQueryable<Usuario> Coordinadores(IAppDbContext db) => db.Usuarios.Where(u => u.Rol == Rol.Coordinador);

    public static async Task<Usuario> PorIdAsync(IAppDbContext db, Guid id, CancellationToken ct) =>
        await Coordinadores(db).SingleOrDefaultAsync(u => u.Id == id, ct)
        ?? throw new NoEncontradoException("El coordinador no existe.");
}

/// <summary>GET /admin/coordinadores?universidadId= (HU-A03).</summary>
public sealed class ListarCoordinadoresHandler(IAppDbContext db)
{
    public async Task<IReadOnlyList<CoordinadorAdminDto>> EjecutarAsync(Guid? universidadId, CancellationToken ct)
    {
        var filas = await ConsultasCoordinadores.Coordinadores(db).AsNoTracking()
            .Where(u => universidadId == null || u.UniversidadId == universidadId)
            .OrderBy(u => u.Universidad!.Siglas).ThenBy(u => u.Apellido1).ThenBy(u => u.Nombre1)
            .Select(u => new
            {
                u.Id, u.Nombre1, u.Nombre2, u.Apellido1, u.Apellido2, u.Correo, u.Activo,
                UniversidadId = u.UniversidadId!.Value, Universidad = u.Universidad!.Nombre,
            })
            .ToListAsync(ct);

        return filas.Select(f => new CoordinadorAdminDto(
            f.Id, Usuario.ArmarNombreCompleto(f.Nombre1, f.Nombre2, f.Apellido1, f.Apellido2), f.Correo,
            new Referencia(f.UniversidadId, f.Universidad), f.Activo)).ToList();
    }
}

/// <summary>
/// POST /admin/coordinadores (HU-A03). Genera una contraseña temporal que se muestra una sola vez; el coordinador
/// debe cambiarla en su primer ingreso (D-17). Correo repetido en toda la plataforma → 409 CORREO_EN_USO.
/// </summary>
public sealed class CrearCoordinadorHandler(IAppDbContext db, IHasherPassword hasher)
{
    public async Task<CuentaCreadaDto> EjecutarAsync(CrearCoordinadorRequest request, CancellationToken ct)
    {
        var universidadActiva = await db.Universidades.AnyAsync(u => u.Id == request.UniversidadId && u.Activa, ct);
        if (!universidadActiva)
            throw new ValidationException([new ValidationFailure("universidadId", MensajesCoordinadores.UniversidadInvalida)]);

        var correo = request.Correo!.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct)) throw CorreoEnUso();

        var passwordTemporal = PasswordTemporal.Generar();
        var coordinador = new Usuario
        {
            Nombre1 = request.Nombre1!.Trim(),
            Nombre2 = string.IsNullOrWhiteSpace(request.Nombre2) ? null : request.Nombre2.Trim(),
            Apellido1 = request.Apellido1!.Trim(),
            Apellido2 = string.IsNullOrWhiteSpace(request.Apellido2) ? null : request.Apellido2.Trim(),
            Correo = correo,
            Rol = Rol.Coordinador,
            UniversidadId = request.UniversidadId,
            Activo = true,
            DebeCambiarPassword = true,
            PasswordHash = hasher.Hashear(passwordTemporal),
        };
        db.Usuarios.Add(coordinador);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // La restricción única también cubre dos creaciones simultáneas con el mismo correo.
            if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct)) throw CorreoEnUso();
            throw;
        }
        return new CuentaCreadaDto(coordinador.Id, coordinador.Correo, passwordTemporal);
    }

    private static ConflictoException CorreoEnUso() => new(CodigosError.CorreoEnUso, MensajesCoordinadores.CorreoEnUso);
}

/// <summary>PATCH /admin/coordinadores/{id}/estado (HU-A03). Inactivo no puede iniciar sesión.</summary>
public sealed class CambiarEstadoCoordinadorHandler(IAppDbContext db)
{
    public async Task EjecutarAsync(Guid id, CambiarEstadoCoordinadorRequest request, CancellationToken ct)
    {
        var coordinador = await ConsultasCoordinadores.PorIdAsync(db, id, ct);
        coordinador.Activo = request.Activo;
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>POST /admin/coordinadores/{id}/restablecer-password (HU-A03): nueva contraseña temporal; la anterior deja de servir.</summary>
public sealed class RestablecerPasswordCoordinadorHandler(IAppDbContext db, IHasherPassword hasher)
{
    public async Task<CuentaCreadaDto> EjecutarAsync(Guid id, CancellationToken ct)
    {
        var coordinador = await ConsultasCoordinadores.PorIdAsync(db, id, ct);
        var passwordTemporal = PasswordTemporal.Generar();
        coordinador.PasswordHash = hasher.Hashear(passwordTemporal);
        coordinador.DebeCambiarPassword = true;
        await db.SaveChangesAsync(ct);
        return new CuentaCreadaDto(coordinador.Id, coordinador.Correo, passwordTemporal);
    }
}
