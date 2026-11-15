using UPractice.Domain.Entidades;

namespace UPractice.Application.Abstracciones;

/// <summary>Emite el JWT firmado y cifrado (JWE, D-15).</summary>
public interface IGeneradorToken
{
    TokenEmitido Generar(Usuario usuario);
}

public sealed record TokenEmitido(string AccessToken, DateTime ExpiraEn);
