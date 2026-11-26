using System.Text.Json.Serialization;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Auth;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Perfil;

/// <summary>Contratos de HU-02 (S5.6, ERD v4: sin teléfono ni resumen).</summary>
public sealed record PrivacidadDto([property: JsonRequired] bool MostrarIndice);

public sealed record PerfilEstudianteDto(
    Guid UsuarioId,
    string NombreCompleto,
    string Correo,
    Referencia Universidad,
    string? NumeroCuenta,
    CarreraDto? Carrera,
    decimal? IndiceAcademico,
    IReadOnlyList<HabilidadDto> Habilidades,
    EstadoVerificacion EstadoVerificacion,
    string? ObservacionVerificacion,
    DateTime? FechaVerificacion,
    PrivacidadDto Privacidad);

/// <summary>Solo mientras el perfil no esté Verificado. <c>IndiceAcademico</c> es nullable para que omitirlo dé 400 y no 0.</summary>
public sealed record ActualizarPerfilAcademicoRequest(string? NumeroCuenta, Guid CarreraId, decimal? IndiceAcademico);

public sealed record ActualizarHabilidadesRequest(IReadOnlyList<Guid>? HabilidadesIds);

/// <summary>Textos idénticos a docs/mensajes-validacion.md y al esquema Zod del frontend.</summary>
public static class MensajesPerfil
{
    public const string NumeroCuenta = "El número de cuenta debe tener solo dígitos (6 a 20).";
    public const string NumeroCuentaEnUso = "El número de cuenta ya está registrado en su universidad.";
    public const string Carrera = "Seleccione una carrera válida.";
    public const string Indice = "El índice debe estar entre 0 y 100.";
    public const string HabilidadesInvalidas = "Seleccione habilidades válidas.";
    public const string HabilidadesDuplicadas = "No repita habilidades.";
    public const string PerfilVerificado = "Su perfil ya fue verificado; los datos académicos no se pueden cambiar.";
    public const string PerfilIncompleto = "Complete primero su perfil académico.";
    public const int MaximoHabilidades = 30;
}
