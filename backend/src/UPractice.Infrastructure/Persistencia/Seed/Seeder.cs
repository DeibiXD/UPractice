using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia.Seed;

/// <summary>
/// Seed por ambiente (§2.2 del diseño). Es idempotente: se puede correr en cada arranque.
/// - Siempre: catálogo de habilidades y el primer SuperAdmin (Seed:SuperAdminCorreo / Seed:SuperAdminPassword).
/// - Solo si Seed:Demo = true: universidades, coordinadores, empresas aprobadas, estudiantes y vacantes de ejemplo.
/// </summary>
internal sealed partial class Seeder(AppDbContext db, IHasherPassword hasher, IConfiguration configuration, ILogger<Seeder> logger)
{
    private static readonly string[] HabilidadesBase =
    [
        "C#", ".NET", "Java", "Python", "JavaScript", "TypeScript", "React", "SQL", "PostgreSQL", "Git",
        "Docker", "Excel avanzado", "Power BI", "Análisis de datos", "Contabilidad", "Finanzas",
        "Inglés", "Comunicación efectiva", "Trabajo en equipo", "Diseño UX/UI", "Redes",
    ];

    public async Task EjecutarAsync(CancellationToken ct = default)
    {
        await SembrarHabilidadesAsync(ct);
        await SembrarSuperAdminAsync(ct);
        if (configuration.GetValue<bool>("Seed:Demo"))
            await SembrarDemoAsync(ct);
    }

    private async Task SembrarHabilidadesAsync(CancellationToken ct)
    {
        var existentes = await db.Habilidades.Select(h => h.Nombre.ToLower()).ToListAsync(ct);
        var nuevas = HabilidadesBase.Where(n => !existentes.Contains(n.ToLower())).Select(n => new Habilidad { Nombre = n }).ToList();
        if (nuevas.Count == 0) return;
        db.Habilidades.AddRange(nuevas);
        await db.SaveChangesAsync(ct);
    }

    private async Task SembrarSuperAdminAsync(CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(u => u.Rol == Rol.SuperAdmin, ct)) return;

        var correo = (configuration["Seed:SuperAdminCorreo"] ?? "admin@upractice.app").Trim().ToLowerInvariant();
        var password = configuration["Seed:SuperAdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No se creó el SuperAdmin: falta Seed:SuperAdminPassword (variable Seed__SuperAdminPassword).");
            return;
        }

        db.Usuarios.Add(new Usuario
        {
            Correo = correo,
            PasswordHash = hasher.Hashear(password),
            Nombre1 = "Administrador",
            Apellido1 = "UPractice",
            Rol = Rol.SuperAdmin,
            DebeCambiarPassword = true, // D-17
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("SuperAdmin {Correo} creado.", correo);
    }
}
