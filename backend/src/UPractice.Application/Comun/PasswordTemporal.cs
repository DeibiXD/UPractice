using System.Security.Cryptography;

namespace UPractice.Application.Comun;

/// <summary>
/// Contraseña temporal generada por el servidor (D-17): 20 caracteres con mayúscula, minúscula, número y símbolo,
/// con RandomNumberGenerator. Se muestra una sola vez y el usuario debe cambiarla en su primer ingreso.
/// La usan HU-E02 (cuentas de empresa) y HU-A03 (coordinadores).
/// </summary>
public static class PasswordTemporal
{
    private const string Mayusculas = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijklmnopqrstuvwxyz";
    private const string Numeros = "0123456789";
    private const string Simbolos = "!@#$%*-_+?";
    private const string Alfabeto = Mayusculas + Minusculas + Numeros + Simbolos;

    public static string Generar()
    {
        Span<char> caracteres = stackalloc char[20];
        caracteres[0] = Mayusculas[RandomNumberGenerator.GetInt32(Mayusculas.Length)];
        caracteres[1] = Minusculas[RandomNumberGenerator.GetInt32(Minusculas.Length)];
        caracteres[2] = Numeros[RandomNumberGenerator.GetInt32(Numeros.Length)];
        caracteres[3] = Simbolos[RandomNumberGenerator.GetInt32(Simbolos.Length)];
        for (var i = 4; i < caracteres.Length; i++)
            caracteres[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];

        // Mezcla (Fisher–Yates) para que los obligatorios no queden siempre al inicio.
        for (var i = caracteres.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (caracteres[i], caracteres[j]) = (caracteres[j], caracteres[i]);
        }
        return new string(caracteres);
    }
}
