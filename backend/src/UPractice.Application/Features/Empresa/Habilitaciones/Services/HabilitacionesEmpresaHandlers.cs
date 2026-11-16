using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Empresa.Habilitaciones;

/// <summary>Consulta solo las habilitaciones de la empresa indicada por el claim autenticado.</summary>
public sealed class ListarHabilitacionesEmpresaHandler(
    IEmpresaHabilitacionRepository repositorio,
    ICurrentUser usuario)
{
    public async Task<IReadOnlyList<HabilitacionEmpresaDto>> EjecutarAsync(CancellationToken ct)
    {
        var empresaId = usuario.EmpresaId
            ?? throw new NoAutenticadoException("La sesión no contiene una empresa válida.");
        var habilitaciones = await repositorio.ListarAsync(empresaId, ct);
        return habilitaciones.Select(Desde).ToArray();
    }

    internal static HabilitacionEmpresaDto Desde(HabilitacionEmpresa habilitacion) =>
        new(habilitacion.Id,
            new UniversidadHabilitacionDto(habilitacion.Universidad.Id, habilitacion.Universidad.Nombre),
            habilitacion.Estado,
            habilitacion.MotivoRechazo,
            habilitacion.FechaSolicitud,
            habilitacion.FechaResolucion);
}

/// <summary>Solicita una habilitación nueva o reinicia una solicitud previamente rechazada.</summary>
public sealed class SolicitarHabilitacionEmpresaHandler(
    IEmpresaHabilitacionRepository repositorio,
    ICurrentUser usuario)
{
    public async Task<HabilitacionEmpresaDto> EjecutarAsync(SolicitarHabilitacionRequest request, CancellationToken ct)
    {
        var empresaId = usuario.EmpresaId
            ?? throw new NoAutenticadoException("La sesión no contiene una empresa válida.");
        var universidad = await repositorio.ObtenerUniversidadActivaAsync(request.UniversidadId, ct);
        if (universidad is null)
            throw new FluentValidation.ValidationException(
                [new FluentValidation.Results.ValidationFailure(nameof(request.UniversidadId), "Seleccione una universidad válida.")]);

        var habilitacion = await repositorio.ObtenerAsync(empresaId, universidad.Id, ct);
        if (habilitacion is { Estado: EstadoHabilitacion.Pendiente or EstadoHabilitacion.Aprobada })
            throw new ConflictoException(CodigosError.HabilitacionEnCurso,
                "Ya existe una solicitud pendiente o aprobada para esta universidad.");

        if (habilitacion is null)
        {
            habilitacion = new HabilitacionEmpresa
            {
                EmpresaId = empresaId,
                UniversidadId = universidad.Id,
                Universidad = universidad,
                Estado = EstadoHabilitacion.Pendiente,
            };
        }
        else
        {
            habilitacion.Estado = EstadoHabilitacion.Pendiente;
            habilitacion.MotivoRechazo = null;
            habilitacion.ResueltaPorId = null;
            habilitacion.FechaResolucion = null;
            habilitacion.FechaSolicitud = DateTime.UtcNow;
        }

        await repositorio.GuardarSolicitudAsync(habilitacion, ct);
        return ListarHabilitacionesEmpresaHandler.Desde(habilitacion);
    }
}

/// <summary>Impide publicar mientras la empresa del claim no tenga habilitaciones aprobadas.</summary>
public sealed class EmpresaPublicacionGuard(
    IEmpresaHabilitacionRepository repositorio,
    ICurrentUser usuario) : IEmpresaPublicacionGuard
{
    public async Task ValidarPuedePublicarAsync(CancellationToken ct)
    {
        var empresaId = usuario.EmpresaId;
        if (empresaId is null || !await repositorio.TieneHabilitacionAprobadaAsync(empresaId.Value, ct))
            throw new ReglaNegocioException(CodigosError.EmpresaNoHabilitada,
                "La empresa necesita al menos una habilitación aprobada para publicar vacantes.");
    }
}
