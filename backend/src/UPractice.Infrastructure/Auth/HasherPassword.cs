using Microsoft.AspNetCore.Identity;
using UPractice.Application.Abstracciones;

namespace UPractice.Infrastructure.Auth;

/// <summary>PBKDF2 con sal (formato v3 de ASP.NET Core Identity).</summary>
internal sealed class HasherPassword : IHasherPassword
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Usuario = new();

    public string Hashear(string password) => Hasher.HashPassword(Usuario, password);

    public bool Verificar(string hash, string password)
        => Hasher.VerifyHashedPassword(Usuario, hash, password) != PasswordVerificationResult.Failed;
}
