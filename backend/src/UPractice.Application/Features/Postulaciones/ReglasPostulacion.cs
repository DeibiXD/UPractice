using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;
using UPractice.Domain.Reglas;

namespace UPractice.Application.Features.Postulaciones;

/// <summary>
/// Reglas que comparten HU-05 (postularse) y HU-06 (reactivar): "mismas validaciones" según el contrato.
/// Ninguna consulta escribe Where por EmpresaId ni por estudiante: los filtros globales ya aíslan los datos.
/// </summary>
internal static class ReglasPostulacion
{
    /// <summary>Honduras es UTC-6 todo el año (sin horario de verano). La fecha de cierre se compara en hora de Honduras.</summary>
    private static DateOnly HoyEnHonduras() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));

    public static bool VacanteVigente(EstadoVacante estado, DateOnly fechaCierre) =>
        estado == EstadoVacante.Publicada && fechaCierre >= HoyEnHonduras();

    public static Guid ExigirUsuario(ICurrentUser actual) =>
        actual.UsuarioId ?? throw new NoAutenticadoException("La sesión ya no es válida. Inicie sesión de nuevo.");

    /// <summary>Datos de la vacante que necesita quien crea o reactiva una postulación.</summary>
    public sealed record VacanteDisponible(Guid EmpresaId, decimal? IndiceMinimo);

    /// <summary>La vacante debe estar publicada, vigente, con empresa activa y habilitada en la universidad del estudiante.</summary>
    public static async Task<VacanteDisponible> ExigirVacanteDisponibleAsync(IAppDbContext db, Guid vacanteId, CancellationToken ct)
    {
        var vacante = await db.Vacantes
            .Where(v => v.Id == vacanteId)
            .Select(v => new { v.Estado, v.FechaCierre, v.EmpresaId, v.IndiceMinimo, EmpresaActiva = v.Empresa.Activa })
            .FirstOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("La vacante no existe.");

        // El filtro global del estudiante solo deja ver las habilitaciones aprobadas de su universidad.
        var habilitada = await db.HabilitacionesEmpresa.AnyAsync(h => h.EmpresaId == vacante.EmpresaId, ct);

        if (!vacante.EmpresaActiva || !habilitada || !VacanteVigente(vacante.Estado, vacante.FechaCierre))
            throw new ReglaNegocioException(CodigosError.VacanteNoDisponible, "La vacante ya no está disponible para postularse.");

        return new VacanteDisponible(vacante.EmpresaId, vacante.IndiceMinimo);
    }

    /// <summary>Un estudiante con una práctica aceptada no entra de nuevo al embudo (D-20).</summary>
    public static async Task ExigirSinPracticaAceptadaAsync(IAppDbContext db, CancellationToken ct)
    {
        if (await db.Postulaciones.AnyAsync(p => p.Estado == EstadoPostulacion.Aceptada, ct))
            throw new ReglaNegocioException(CodigosError.PracticaYaAceptada, "Ya aceptó una oferta de práctica.");
    }

    /// <summary>Perfil verificado por la coordinación y carrera permitida por la vacante. Devuelve el perfil para calcular CumpleRequisitos.</summary>
    public static async Task<PerfilEstudiante> ExigirPerfilElegibleAsync(IAppDbContext db, Guid vacanteId, CancellationToken ct)
    {
        var perfil = await db.PerfilesEstudiante.AsNoTracking().FirstOrDefaultAsync(ct);
        if (perfil is null || perfil.EstadoVerificacion != EstadoVerificacion.Verificado)
            throw new ReglaNegocioException(CodigosError.PerfilNoVerificado,
                "Su perfil académico debe estar verificado por la coordinación para postularse.");

        var carreraPermitida = await db.VacanteCarreras.AnyAsync(vc => vc.VacanteId == vacanteId && vc.CarreraId == perfil.CarreraId, ct);
        if (!carreraPermitida)
            throw new ReglaNegocioException(CodigosError.CarreraNoElegible, "Su carrera no está entre las que acepta esta vacante.");

        return perfil;
    }

    /// <summary>El CV debe ser del estudiante (el filtro global lo garantiza) y no estar dado de baja (HU-03).</summary>
    public static async Task ExigirCurriculumAsync(IAppDbContext db, Guid curriculumId, CancellationToken ct)
    {
        var existe = await db.Curriculums.AnyAsync(c => c.Id == curriculumId && !c.Eliminado, ct);
        if (!existe) throw new NoEncontradoException("El currículum no existe.");
    }

    /// <summary>Foto del cumplimiento del índice al postular. El índice mínimo es deseable, no excluyente (D-06).</summary>
    public static bool CumpleIndice(decimal? indiceMinimo, decimal indiceAcademico) =>
        indiceMinimo is null || indiceAcademico >= indiceMinimo;

    public static HistorialPostulacion NuevoHistorial(Postulacion p, EstadoPostulacion? anterior, EstadoPostulacion nuevo, Guid cambiadoPorId, string? comentario = null) => new()
    {
        EmpresaId = p.EmpresaId,
        PostulacionId = p.Id,
        EstadoAnterior = anterior,
        EstadoNuevo = nuevo,
        CambiadoPorId = cambiadoPorId,
        Comentario = comentario,
    };

    /// <summary>Acciones del estudiante según la máquina de estados. Reactivar solo si la vacante sigue disponible y no tiene práctica aceptada.</summary>
    public static IReadOnlyList<AccionPostulacion> AccionesPara(EstadoPostulacion estado, bool puedeReactivar) =>
        TransicionesPostulacion.Siguientes(estado, ActorPostulacion.Estudiante)
            .Select(destino => destino switch
            {
                EstadoPostulacion.Retirada => (AccionPostulacion?)AccionPostulacion.Retirar,
                EstadoPostulacion.Enviada when puedeReactivar => AccionPostulacion.Reactivar,
                EstadoPostulacion.Aceptada => AccionPostulacion.AceptarOferta,
                EstadoPostulacion.Declinada => AccionPostulacion.DeclinarOferta,
                _ => null,
            })
            .OfType<AccionPostulacion>()
            .Order()
            .ToList();
}
