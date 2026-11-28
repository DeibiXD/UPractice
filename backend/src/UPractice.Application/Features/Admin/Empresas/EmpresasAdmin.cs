using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Admin.Empresas;

// ===== Contratos (S5.6) =====

public sealed record ListarEmpresasAdminRequest(string? Busqueda = null, bool? Activa = null, int Pagina = 1, int TamanoPagina = 10);

public sealed record HabilitacionResumenDto(string Universidad, EstadoHabilitacion Estado);

public sealed record EmpresaAdminDto(
    Guid Id,
    string NombreComercial,
    string RazonSocial,
    string Rtn,
    string Sector,
    bool Activa,
    string? MotivoSuspension,
    DateTime CreadoEn,
    IReadOnlyList<HabilitacionResumenDto> Habilitaciones);

/// <summary><c>Activa</c> es nullable a propósito: un cuerpo vacío da 400 en lugar de suspender por omisión.</summary>
public sealed record CambiarEstadoEmpresaRequest(bool? Activa, string? MotivoSuspension);

public static class MensajesEmpresasAdmin
{
    public const string Activa = "Indique si activa o suspende la empresa.";
    public const string Motivo = "Indique el motivo (10 a 500 caracteres).";
    public const string Busqueda = "Máximo 100 caracteres.";
    public const string Pagina = "La página debe ser mayor o igual a 1.";
    public const string TamanoPagina = "El tamaño de página debe estar entre 1 y 50.";
}

// ===== Validadores =====

public sealed class ListarEmpresasAdminValidator : AbstractValidator<ListarEmpresasAdminRequest>
{
    public ListarEmpresasAdminValidator()
    {
        RuleFor(x => x.Busqueda).MaximumLength(100).WithMessage(MensajesEmpresasAdmin.Busqueda);
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1).WithMessage(MensajesEmpresasAdmin.Pagina);
        RuleFor(x => x.TamanoPagina).InclusiveBetween(1, 50).WithMessage(MensajesEmpresasAdmin.TamanoPagina);
    }
}

public sealed class CambiarEstadoEmpresaValidator : AbstractValidator<CambiarEstadoEmpresaRequest>
{
    public CambiarEstadoEmpresaValidator()
    {
        RuleFor(x => x.Activa).NotNull().WithMessage(MensajesEmpresasAdmin.Activa);
        // El motivo es obligatorio (10–500) solo al suspender; coincide con el CHECK de la tabla Empresas.
        RuleFor(x => x.MotivoSuspension)
            .Must(m => m is { } t && t.Trim().Length is >= 10 and <= 500).WithMessage(MensajesEmpresasAdmin.Motivo)
            .When(x => x.Activa == false);
    }
}

// ===== Handlers =====

/// <summary>
/// GET /admin/empresas?busqueda=&amp;activa=&amp;pagina=&amp;tamanoPagina= (HU-A04). Empresas no tiene filtro global y el
/// SuperAdmin ve todas las HabilitacionesEmpresa (D-14); vacantes, postulaciones y perfiles siguen ocultos para él.
/// </summary>
public sealed class ListarEmpresasAdminHandler(IAppDbContext db)
{
    public async Task<PaginaResultado<EmpresaAdminDto>> EjecutarAsync(ListarEmpresasAdminRequest request, CancellationToken ct)
    {
        var consulta = db.Empresas.AsNoTracking();
        if (request.Activa is { } activa) consulta = consulta.Where(e => e.Activa == activa);
        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            var texto = request.Busqueda.Trim().ToLower();
            consulta = consulta.Where(e => e.NombreComercial.ToLower().Contains(texto)
                                        || e.RazonSocial.ToLower().Contains(texto)
                                        || e.Rtn.Contains(texto));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderBy(e => e.NombreComercial).ThenBy(e => e.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina).Take(request.TamanoPagina)
            .Select(e => new EmpresaAdminDto(
                e.Id, e.NombreComercial, e.RazonSocial, e.Rtn, e.Sector, e.Activa, e.MotivoSuspension, e.CreadoEn,
                e.Habilitaciones.OrderBy(h => h.Universidad.Siglas)
                    .Select(h => new HabilitacionResumenDto(h.Universidad.Siglas, h.Estado)).ToList()))
            .ToListAsync(ct);

        var paginas = (int)Math.Ceiling(total / (double)request.TamanoPagina);
        return new PaginaResultado<EmpresaAdminDto>(items, request.Pagina, request.TamanoPagina, total, paginas);
    }
}

/// <summary>
/// PATCH /admin/empresas/{id}/estado (HU-A04). Suspendida: sus usuarios no pueden iniciar sesión (LoginHandler) y sus
/// vacantes dejan de verse para los estudiantes (filtro global de Vacantes). Al reactivarla se limpia el motivo.
/// </summary>
public sealed class CambiarEstadoEmpresaHandler(IAppDbContext db)
{
    public async Task EjecutarAsync(Guid id, CambiarEstadoEmpresaRequest request, CancellationToken ct)
    {
        var empresa = await db.Empresas.SingleOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NoEncontradoException("La empresa no existe.");

        empresa.Activa = request.Activa!.Value;
        empresa.MotivoSuspension = empresa.Activa ? null : request.MotivoSuspension!.Trim();
        await db.SaveChangesAsync(ct);
    }
}
