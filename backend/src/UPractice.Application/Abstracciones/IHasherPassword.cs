namespace UPractice.Application.Abstracciones;

/// <summary>Hash de contraseñas (D-03). Implementado con PasswordHasher de ASP.NET Core Identity.</summary>
public interface IHasherPassword
{
    string Hashear(string password);
    bool Verificar(string hash, string password);
}
