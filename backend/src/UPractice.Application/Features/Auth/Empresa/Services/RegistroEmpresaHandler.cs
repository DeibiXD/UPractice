using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Auth.Empresa;

/// <summary>Registra la empresa, su AdminEmpresa y sus solicitudes iniciales de forma atómica.</summary>
public sealed class RegistroEmpresaHandler(
    IEmpresaHabilitacionRepository repositorio,
    IHasherPassword hasher,
    IGeneradorToken tokens)
{
    public async Task<LoginResponse> EjecutarAsync(RegistroEmpresaRequest request, CancellationToken ct)
    {
        var rtn = request.Rtn.Trim();
        var correo = request.Admin.Correo.Trim().ToLowerInvariant();

        if (await repositorio.ExisteRtnAsync(rtn, ct))
            throw new ConflictoException(CodigosError.RtnEnUso, "El RTN ya está registrado.");
        if (await repositorio.ExisteCorreoAsync(correo, ct))
            throw new ConflictoException(CodigosError.CorreoEnUso, "El correo ya está registrado.");

        var empresa = new UPractice.Domain.Entidades.Empresa
        {
            NombreComercial = request.NombreComercial.Trim(),
            RazonSocial = request.RazonSocial.Trim(),
            Rtn = rtn,
            Sector = request.Sector.Trim(),
            SitioWeb = string.IsNullOrWhiteSpace(request.SitioWeb) ? null : request.SitioWeb.Trim(),
        };
        var usuario = new Usuario
        {
            Correo = correo,
            PasswordHash = hasher.Hashear(request.Admin.Password),
            Nombre1 = request.Admin.Nombre1.Trim(),
            Nombre2 = string.IsNullOrWhiteSpace(request.Admin.Nombre2) ? null : request.Admin.Nombre2.Trim(),
            Apellido1 = request.Admin.Apellido1.Trim(),
            Apellido2 = string.IsNullOrWhiteSpace(request.Admin.Apellido2) ? null : request.Admin.Apellido2.Trim(),
            Rol = Rol.AdminEmpresa,
            EmpresaId = empresa.Id,
            Empresa = empresa,
        };

        await repositorio.RegistrarTransaccionalmenteAsync(empresa, usuario, request.UniversidadesIds, ct);

        var token = tokens.Generar(usuario);
        return new LoginResponse(token.AccessToken, token.ExpiraEn, UsuarioActualDto.Desde(usuario));
    }
}
