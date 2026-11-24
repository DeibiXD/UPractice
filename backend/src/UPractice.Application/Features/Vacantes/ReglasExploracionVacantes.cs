using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Vacantes;

internal static class ReglasExploracionVacantes
{
    internal sealed record Estudiante(Guid UsuarioId, Guid UniversidadId, Guid? CarreraId, decimal? IndiceAcademico);

    public static async Task<Estudiante> ExigirEstudianteAsync(IAppDbContext db, ICurrentUser actual, CancellationToken ct)
    {
        if (actual.UsuarioId is not { } usuarioId || actual.UniversidadId is not { } universidadId)
            throw SesionInvalida();

        // Usuarios no tiene filtro global: contrastar identidad, rol y universidad de los claims.
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Rol == Rol.Estudiante && u.Activo && u.UniversidadId == universidadId)
            .Select(u => new Estudiante(u.Id, universidadId,
                u.Perfil == null ? null : (Guid?)u.Perfil.CarreraId,
                u.Perfil == null ? null : (decimal?)u.Perfil.IndiceAcademico))
            .SingleOrDefaultAsync(ct) ?? throw SesionInvalida();
    }

    public static IQueryable<Vacante> Visibles(IAppDbContext db, Estudiante estudiante, DateOnly hoy) =>
        db.Vacantes.Where(v => v.Estado == EstadoVacante.Publicada && v.FechaCierre >= hoy
            && v.Empresa.Activa && db.HabilitacionesEmpresa.Any(h => h.EmpresaId == v.EmpresaId
                && h.UniversidadId == estudiante.UniversidadId && h.Estado == EstadoHabilitacion.Aprobada));

    public static IQueryable<Vacante> Filtrar(IQueryable<Vacante> consulta, FiltroVacantesQuery filtro)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var busqueda = filtro.Busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(v => v.Titulo.ToLower().Contains(busqueda)
                || v.Empresa.NombreComercial.ToLower().Contains(busqueda));
        }
        if (filtro.CarreraId is { } carreraId)
            consulta = consulta.Where(v => v.Carreras.Any(c => c.CarreraId == carreraId));
        if (filtro.Modalidad is { } modalidad) consulta = consulta.Where(v => v.Modalidad == modalidad);
        if (filtro.Remunerada is { } remunerada) consulta = consulta.Where(v => v.Remunerada == remunerada);
        return consulta;
    }

    private static NoAutenticadoException SesionInvalida() =>
        new("La sesión ya no es válida. Inicie sesión de nuevo.");
}
