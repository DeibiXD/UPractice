using Microsoft.EntityFrameworkCore;
using UPractice.Domain.Entidades;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>Busca en el seed de demostración los registros que usan las pruebas (sin filtros globales).</summary>
internal static class Datos
{
    public const string ReclutadorDigital = "rrhh@digitales.example.com";
    public const string ReclutadorFinanzas = "rrhh@finanzas.example.com";
    public const string EstudianteUnah = "ana.lopez@unah.hn";
    public const string CoordinadorUnah = "coordinacion@unah.hn";
    public const string CoordinadorUnitec = "coordinacion@unitec.edu";
    public const string SuperAdmin = "admin@upractice.app";

    public static async Task<Usuario> UsuarioAsync(ApiFixture api, string correo)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Usuarios.AsNoTracking().SingleAsync(u => u.Correo == correo);
    }

    public static async Task<List<Vacante>> VacantesAsync(ApiFixture api)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Vacantes.IgnoreQueryFilters().AsNoTracking().Include(v => v.Empresa).ToListAsync();
    }
}
