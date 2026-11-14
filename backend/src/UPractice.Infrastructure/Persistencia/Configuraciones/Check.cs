namespace UPractice.Infrastructure.Persistencia.Configuraciones;

/// <summary>Arma los CHECK de PostgreSQL para columnas enum guardadas como texto (D-10).</summary>
internal static class Check
{
    internal static string Enum<TEnum>(string columna) where TEnum : struct, System.Enum
        => $"\"{columna}\" IN ({string.Join(", ", System.Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})";
}
