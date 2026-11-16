using UPractice.Application.Abstracciones;

namespace UPractice.Infrastructure.Tiempo;

/// <summary>Convierte el instante del reloj a la fecha de negocio de Honduras.</summary>
public sealed class FechaHonduras(TimeProvider reloj) : IFechaHonduras
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Tegucigalpa");

    public DateOnly Hoy() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), Zona).DateTime);

    public DateTime UtcAhora() => reloj.GetUtcNow().UtcDateTime;
}
