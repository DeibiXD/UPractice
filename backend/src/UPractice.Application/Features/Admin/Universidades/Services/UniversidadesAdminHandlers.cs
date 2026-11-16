using UPractice.Application.Features.Admin.Universidades.DTOs;
using UPractice.Application.Features.Admin.Universidades.Interfaces;
using UPractice.Domain.Entidades;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Admin.Universidades.Services;

/// <summary>Lista todas las universidades, incluidas las inactivas, para SuperAdmin.</summary>
public sealed class ListarUniversidadesAdminHandler(IUniversidadRepository repositorio)
{
    public Task<IReadOnlyList<UniversidadAdminResponseDto>> EjecutarAsync(CancellationToken ct) =>
        repositorio.GetAllAdminAsync(ct);
}

/// <summary>Registra una universidad con siglas y dominio normalizados.</summary>
public sealed class CrearUniversidadHandler(IUniversidadRepository repositorio)
{
    public async Task<UniversidadAdminResponseDto> EjecutarAsync(CrearUniversidadRequestDto request, CancellationToken ct)
    {
        var siglas = NormalizarSiglas(request.Siglas);
        var dominio = NormalizarDominio(request.DominioCorreo);
        await ValidarUnicidadAsync(siglas, dominio, null, ct);

        var universidad = new Universidad
        {
            Nombre = request.Nombre.Trim(),
            Siglas = siglas,
            DominioCorreo = dominio,
        };
        await repositorio.AddAsync(universidad, ct);

        return await repositorio.GetAdminByIdAsync(universidad.Id, ct)
            ?? throw new InvalidOperationException("La universidad creada no pudo recuperarse.");
    }

    internal static string NormalizarSiglas(string siglas) => siglas.Trim().ToUpperInvariant();
    internal static string NormalizarDominio(string dominio) => dominio.Trim().ToLowerInvariant();

    internal static async Task ValidarUnicidadAsync(
        IUniversidadRepository repositorio,
        string siglas,
        string dominio,
        Guid? exceptoId,
        CancellationToken ct)
    {
        if (await repositorio.ExisteSiglasAsync(siglas, exceptoId, ct))
            throw new ConflictoException(CodigosError.SiglasEnUso, "Las siglas ya están registradas.");
        if (await repositorio.ExisteDominioAsync(dominio, exceptoId, ct))
            throw new ConflictoException(CodigosError.DominioEnUso, "El dominio ya está registrado.");
    }

    private Task ValidarUnicidadAsync(string siglas, string dominio, Guid? exceptoId, CancellationToken ct) =>
        ValidarUnicidadAsync(repositorio, siglas, dominio, exceptoId, ct);
}

/// <summary>Actualiza nombre, siglas y dominio sin modificar el estado activo.</summary>
public sealed class ActualizarUniversidadHandler(IUniversidadRepository repositorio)
{
    public async Task<UniversidadAdminResponseDto> EjecutarAsync(
        Guid id,
        ActualizarUniversidadRequestDto request,
        CancellationToken ct)
    {
        var universidad = await repositorio.GetByIdAsync(id, ct)
            ?? throw new NoEncontradoException("La universidad no existe.");
        var siglas = CrearUniversidadHandler.NormalizarSiglas(request.Siglas);
        var dominio = CrearUniversidadHandler.NormalizarDominio(request.DominioCorreo);
        await CrearUniversidadHandler.ValidarUnicidadAsync(repositorio, siglas, dominio, id, ct);

        universidad.Nombre = request.Nombre.Trim();
        universidad.Siglas = siglas;
        universidad.DominioCorreo = dominio;
        await repositorio.UpdateAsync(universidad, ct);

        return await repositorio.GetAdminByIdAsync(id, ct)
            ?? throw new NoEncontradoException("La universidad no existe.");
    }
}

/// <summary>Activa o desactiva una universidad sin eliminar registros asociados.</summary>
public sealed class CambiarEstadoUniversidadHandler(IUniversidadRepository repositorio)
{
    public async Task<UniversidadAdminResponseDto> EjecutarAsync(
        Guid id,
        CambiarEstadoUniversidadRequestDto request,
        CancellationToken ct)
    {
        var universidad = await repositorio.GetByIdAsync(id, ct)
            ?? throw new NoEncontradoException("La universidad no existe.");
        universidad.Activa = request.Activa!.Value;
        await repositorio.UpdateAsync(universidad, ct);

        return await repositorio.GetAdminByIdAsync(id, ct)
            ?? throw new NoEncontradoException("La universidad no existe.");
    }
}
