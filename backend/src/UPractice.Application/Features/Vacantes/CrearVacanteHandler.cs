using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Vacantes;

public sealed class CrearVacanteHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task<VacanteEmpresaDto> EjecutarAsync(CrearVacanteRequest request, CancellationToken ct)
    {
        if (actual.EmpresaId is not { } empresaId || actual.UsuarioId is not { } usuarioId)
            throw SesionInvalida();

        // Usuarios no tiene filtro global: comprobar identidad y empresa de forma explícita.
        var autor = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.EmpresaId == empresaId && u.Activo
                && u.Empresa != null && u.Empresa.Activa
                && (u.Rol == Rol.AdminEmpresa || u.Rol == Rol.Reclutador))
            .Select(u => new { u.Nombre1, u.Nombre2, u.Apellido1, u.Apellido2 })
            .SingleOrDefaultAsync(ct)
            ?? throw SesionInvalida();

        var carreras = await db.Carreras.AsNoTracking()
            .Where(c => request.CarrerasIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id, c.Nombre, c.Activa,
                Facultad = c.Facultad.Nombre,
                c.Facultad.UniversidadId,
                UniversidadActiva = c.Facultad.Universidad.Activa,
            })
            .ToListAsync(ct);
        var habilidadesIds = request.Habilidades.Select(h => h.HabilidadId).ToArray();
        var habilidades = await db.Habilidades.AsNoTracking()
            .Where(h => habilidadesIds.Contains(h.Id))
            .Select(h => new { h.Id, h.Nombre })
            .ToListAsync(ct);

        List<ValidationFailure> errores = [];
        if (carreras.Count != request.CarrerasIds.Length || carreras.Any(c => !c.Activa || !c.UniversidadActiva))
            errores.Add(new ValidationFailure("carrerasIds", MensajesVacantes.CarrerasInvalidas));
        if (habilidades.Count != habilidadesIds.Length)
            errores.Add(new ValidationFailure("habilidades", MensajesVacantes.HabilidadesInvalidas));
        if (errores.Count > 0) throw new ValidationException(errores);

        // HasQueryFilter limita estas habilitaciones a la empresa autenticada.
        var universidadesAprobadas = await db.HabilitacionesEmpresa
            .Where(h => h.Estado == EstadoHabilitacion.Aprobada)
            .Select(h => h.UniversidadId)
            .ToListAsync(ct);
        if (request.Publicar && universidadesAprobadas.Count == 0)
            throw new ReglaNegocioException(CodigosError.EmpresaNoHabilitada,
                "La empresa debe tener una habilitación aprobada para publicar vacantes.");
        if (carreras.Any(c => !universidadesAprobadas.Contains(c.UniversidadId)))
            throw new ReglaNegocioException(CodigosError.CarreraNoPermitida,
                "Seleccione carreras de universidades donde su empresa esté habilitada.");

        var vacante = new Vacante
        {
            EmpresaId = empresaId,
            CreadaPorId = usuarioId,
            Titulo = request.Titulo.Trim(),
            Descripcion = request.Descripcion.Trim(),
            Modalidad = request.Modalidad,
            Ubicacion = request.Ubicacion.Trim(),
            Cupos = request.Cupos,
            Remunerada = request.Remunerada,
            IndiceMinimo = request.IndiceMinimo,
            FechaCierre = request.FechaCierre,
            Estado = request.Publicar ? EstadoVacante.Publicada : EstadoVacante.Borrador,
            FechaPublicacion = request.Publicar ? fecha.UtcAhora() : null,
        };
        foreach (var carreraId in request.CarrerasIds)
            vacante.Carreras.Add(new VacanteCarrera { VacanteId = vacante.Id, CarreraId = carreraId });
        foreach (var habilidad in request.Habilidades)
            vacante.Habilidades.Add(new VacanteHabilidad
            {
                VacanteId = vacante.Id, HabilidadId = habilidad.HabilidadId, EsRequerida = habilidad.EsRequerida,
            });

        db.Vacantes.Add(vacante);
        await db.SaveChangesAsync(ct);

        // Devolver la precisión realmente persistida: numeric(5,2) y timestamps de PostgreSQL.
        var guardada = await db.Vacantes.AsNoTracking().SingleAsync(v => v.Id == vacante.Id, ct);
        var carrerasPorId = carreras.ToDictionary(c => c.Id);
        var habilidadesPorId = habilidades.ToDictionary(h => h.Id);
        return new VacanteEmpresaDto(
            guardada.Id, guardada.Titulo, guardada.Descripcion, guardada.Modalidad, guardada.Ubicacion,
            guardada.Cupos, guardada.Remunerada, guardada.IndiceMinimo, guardada.FechaCierre,
            guardada.FechaPublicacion, guardada.Estado,
            request.CarrerasIds.Select(id =>
            {
                var c = carrerasPorId[id];
                return new CarreraDto(c.Id, c.Nombre, c.Facultad, c.UniversidadId);
            }).ToArray(),
            request.Habilidades.Select(h => new HabilidadVacanteDto(
                h.HabilidadId, habilidadesPorId[h.HabilidadId].Nombre, h.EsRequerida)).ToArray(),
            Usuario.ArmarNombreCompleto(autor.Nombre1, autor.Nombre2, autor.Apellido1, autor.Apellido2),
            0, new Dictionary<EstadoPostulacion, int>());
    }

    private static NoAutenticadoException SesionInvalida() =>
        new("La sesión ya no es válida. Inicie sesión de nuevo.");
}
