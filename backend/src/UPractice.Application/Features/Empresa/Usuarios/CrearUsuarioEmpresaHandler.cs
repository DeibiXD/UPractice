using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
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

        var passwordTemporal = GenerarPasswordTemporal();
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

    private static string GenerarPasswordTemporal()
    {
        const string mayusculas = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string minusculas = "abcdefghijklmnopqrstuvwxyz";
        const string numeros = "0123456789";
        const string simbolos = "!@#$%*-_+?";
        const string alfabeto = mayusculas + minusculas + numeros + simbolos;

        Span<char> caracteres = stackalloc char[20];
        caracteres[0] = mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)];
        caracteres[1] = minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)];
        caracteres[2] = numeros[RandomNumberGenerator.GetInt32(numeros.Length)];
        caracteres[3] = simbolos[RandomNumberGenerator.GetInt32(simbolos.Length)];
        for (var i = 4; i < caracteres.Length; i++)
            caracteres[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];

        for (var i = caracteres.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (caracteres[i], caracteres[j]) = (caracteres[j], caracteres[i]);
        }
        return new string(caracteres);
    }
}
