using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Vacantes;

internal static class ReglasVacante
{
    internal sealed record Autor(Guid EmpresaId, Guid UsuarioId, string NombreCompleto);
    internal sealed record Seleccion(CarreraDto[] Carreras, HabilidadVacanteDto[] Habilidades);

    public static async Task<Autor> ExigirUsuarioEmpresaAsync(IAppDbContext db, ICurrentUser actual, CancellationToken ct)
    {
        if (actual.EmpresaId is not { } empresaId || actual.UsuarioId is not { } usuarioId)
            throw SesionInvalida();

        // Usuarios no tiene filtro global; comprobar también bajas posteriores a la emisión del token.
        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.EmpresaId == empresaId && u.Activo
                && u.Empresa != null && u.Empresa.Activa
                && (u.Rol == Rol.AdminEmpresa || u.Rol == Rol.Reclutador))
            .Select(u => new { u.Nombre1, u.Nombre2, u.Apellido1, u.Apellido2 })
            .SingleOrDefaultAsync(ct) ?? throw SesionInvalida();
        return new Autor(empresaId, usuarioId,
            Usuario.ArmarNombreCompleto(usuario.Nombre1, usuario.Nombre2, usuario.Apellido1, usuario.Apellido2));
    }

    public static EstadoVacante EstadoEfectivo(EstadoVacante estado, DateOnly cierre, DateOnly hoy) =>
        estado == EstadoVacante.Publicada && cierre < hoy ? EstadoVacante.Cerrada : estado;

    public static async Task<Seleccion> ValidarSeleccionAsync(IAppDbContext db,
        Guid[] carrerasIds, HabilidadVacanteRequest[] habilidadesSolicitadas, bool publicar, CancellationToken ct)
    {
        var carreras = await db.Carreras.AsNoTracking().Where(c => carrerasIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id, c.Nombre, c.Activa, Facultad = c.Facultad.Nombre,
                c.Facultad.UniversidadId, UniversidadActiva = c.Facultad.Universidad.Activa,
            }).ToListAsync(ct);
        var habilidadesIds = habilidadesSolicitadas.Select(h => h.HabilidadId).ToArray();
        var habilidades = await db.Habilidades.AsNoTracking().Where(h => habilidadesIds.Contains(h.Id))
            .Select(h => new { h.Id, h.Nombre }).ToListAsync(ct);

        List<ValidationFailure> errores = [];
        if (carreras.Count == 0 || carreras.Count != carrerasIds.Length || carreras.Any(c => !c.Activa || !c.UniversidadActiva))
            errores.Add(new ValidationFailure("carrerasIds", MensajesVacantes.CarrerasInvalidas));
        if (habilidades.Count != habilidadesIds.Length)
            errores.Add(new ValidationFailure("habilidades", MensajesVacantes.HabilidadesInvalidas));
        if (errores.Count > 0) throw new ValidationException(errores);

        // El filtro global conserva solamente las habilitaciones de esta empresa.
        var universidadesAprobadas = await db.HabilitacionesEmpresa
            .Where(h => h.Estado == EstadoHabilitacion.Aprobada)
            .Select(h => h.UniversidadId).ToListAsync(ct);
        if (publicar && universidadesAprobadas.Count == 0)
            throw new ReglaNegocioException(CodigosError.EmpresaNoHabilitada,
                "La empresa debe tener una habilitación aprobada para publicar vacantes.");
        if (carreras.Any(c => !universidadesAprobadas.Contains(c.UniversidadId)))
            throw new ReglaNegocioException(CodigosError.CarreraNoPermitida,
                "Seleccione carreras de universidades donde su empresa esté habilitada.");

        var carrerasPorId = carreras.ToDictionary(c => c.Id);
        var habilidadesPorId = habilidades.ToDictionary(h => h.Id);
        return new Seleccion(carrerasIds.Select(id =>
        {
            var carrera = carrerasPorId[id];
            return new CarreraDto(carrera.Id, carrera.Nombre, carrera.Facultad, carrera.UniversidadId);
        }).ToArray(), habilidadesSolicitadas.Select(h => new HabilidadVacanteDto(
            h.HabilidadId, habilidadesPorId[h.HabilidadId].Nombre, h.EsRequerida)).ToArray());
    }

    private static NoAutenticadoException SesionInvalida() =>
        new("La sesión ya no es válida. Inicie sesión de nuevo.");
}
