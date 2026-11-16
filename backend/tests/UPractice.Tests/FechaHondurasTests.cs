using System.Globalization;
using UPractice.Infrastructure.Tiempo;

namespace UPractice.Tests;

public sealed class FechaHondurasTests
{
    [Theory]
    [InlineData("2026-11-16T00:30:00Z", "2026-11-15")]
    [InlineData("2026-11-16T05:59:59Z", "2026-11-15")]
    [InlineData("2026-11-16T06:00:00Z", "2026-11-16")]
    public void Hoy_usa_el_dia_de_Honduras_al_cruzar_la_medianoche_UTC(string instanteUtc, string fechaEsperada)
    {
        var instante = DateTimeOffset.Parse(instanteUtc, CultureInfo.InvariantCulture);
        var fecha = new FechaHonduras(new RelojFijo(instante));

        Assert.Equal(DateOnly.ParseExact(fechaEsperada, "yyyy-MM-dd", CultureInfo.InvariantCulture), fecha.Hoy());
    }

    [Fact]
    public void UtcAhora_conserva_el_instante_y_el_tipo_UTC()
    {
        var instante = new DateTimeOffset(2026, 11, 16, 5, 30, 0, TimeSpan.Zero);
        var fecha = new FechaHonduras(new RelojFijo(instante));

        var actual = fecha.UtcAhora();

        Assert.Equal(DateTimeKind.Utc, actual.Kind);
        Assert.Equal(instante.UtcDateTime, actual);
    }

    private sealed class RelojFijo(DateTimeOffset instante) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instante;
    }
}
