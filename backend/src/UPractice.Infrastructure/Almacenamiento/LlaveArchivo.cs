using System.Text.RegularExpressions;

namespace UPractice.Infrastructure.Almacenamiento;

/// <summary>
/// Defensa en profundidad: las llaves las genera la API (<c>LlavesArchivo</c>), pero antes de tocar el disco
/// o Supabase se comprueba que no traigan "..", barras iniciales ni caracteres raros.
/// </summary>
internal static partial class LlaveArchivo
{
    [GeneratedRegex(@"^[A-Za-z0-9_-]+(/[A-Za-z0-9_-]+)*\.[a-z0-9]{1,5}$")]
    private static partial Regex Formato();

    public static string Validar(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 300 || !Formato().IsMatch(key))
            throw new ArgumentException($"La llave de archivo '{key}' no tiene un formato válido.", nameof(key));
        return key;
    }

    public static bool EsValida(string? key) =>
        !string.IsNullOrWhiteSpace(key) && key.Length <= 300 && Formato().IsMatch(key);
}
