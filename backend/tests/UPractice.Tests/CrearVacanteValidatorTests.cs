using Microsoft.Extensions.DependencyInjection;
using UPractice.Application;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Vacantes;
using UPractice.Domain.Enums;

namespace UPractice.Tests;

public sealed class CrearVacanteValidatorTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 15);
    private readonly CrearVacanteValidator validator;

    public CrearVacanteValidatorTests()
    {
        // Mismo resolver camelCase de la API; no depende de si la fixture HTTP ya arrancó.
        new ServiceCollection().AddApplication();
        validator = new CrearVacanteValidator(new FechaFija(Hoy));
    }

    [Fact]
    public async Task Acepta_limites_validos_e_indice_y_habilidades_opcionales()
    {
        var minimo = Solicitud() with
        {
            Titulo = new string('T', 5), Descripcion = new string('D', 30), Ubicacion = "HN",
            Cupos = 1, IndiceMinimo = null, Habilidades = [],
        };
        var maximo = Solicitud() with
        {
            Titulo = new string('T', 120), Descripcion = new string('D', 4000), Ubicacion = new string('U', 120),
            Cupos = 50, IndiceMinimo = 100m,
        };

        Assert.True((await validator.ValidateAsync(minimo)).IsValid);
        Assert.True((await validator.ValidateAsync(maximo)).IsValid);
    }

    [Theory]
    [InlineData("tituloCorto", "titulo", "El título debe tener entre 5 y 120 caracteres.")]
    [InlineData("tituloLargo", "titulo", "El título debe tener entre 5 y 120 caracteres.")]
    [InlineData("descripcionCorta", "descripcion", "La descripción debe tener entre 30 y 4000 caracteres.")]
    [InlineData("descripcionLarga", "descripcion", "La descripción debe tener entre 30 y 4000 caracteres.")]
    [InlineData("ubicacionCorta", "ubicacion", "Debe tener entre 2 y 120 caracteres.")]
    [InlineData("ubicacionLarga", "ubicacion", "Debe tener entre 2 y 120 caracteres.")]
    [InlineData("sinCupos", "cupos", "Los cupos deben estar entre 1 y 50.")]
    [InlineData("demasiadosCupos", "cupos", "Los cupos deben estar entre 1 y 50.")]
    [InlineData("indiceNegativo", "indiceMinimo", "El índice mínimo debe estar entre 0 y 100.")]
    [InlineData("indiceMayor", "indiceMinimo", "El índice mínimo debe estar entre 0 y 100.")]
    [InlineData("modalidad", "modalidad", "Seleccione una modalidad.")]
    public async Task Rechaza_valores_fuera_del_contrato_con_el_mensaje_compartido(string caso, string campo, string mensaje)
    {
        var solicitud = Solicitud();
        solicitud = caso switch
        {
            "tituloCorto" => solicitud with { Titulo = "1234" },
            "tituloLargo" => solicitud with { Titulo = new string('T', 121) },
            "descripcionCorta" => solicitud with { Descripcion = new string('D', 29) },
            "descripcionLarga" => solicitud with { Descripcion = new string('D', 4001) },
            "ubicacionCorta" => solicitud with { Ubicacion = "H" },
            "ubicacionLarga" => solicitud with { Ubicacion = new string('U', 121) },
            "sinCupos" => solicitud with { Cupos = 0 },
            "demasiadosCupos" => solicitud with { Cupos = 51 },
            "indiceNegativo" => solicitud with { IndiceMinimo = -0.01m },
            "indiceMayor" => solicitud with { IndiceMinimo = 100.01m },
            "modalidad" => solicitud with { Modalidad = (Modalidad)999 },
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };

        var resultado = await validator.ValidateAsync(solicitud);

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo && e.ErrorMessage == mensaje);
    }

    [Theory]
    [InlineData("sinCarreras", "carrerasIds", "Seleccione al menos una carrera.")]
    [InlineData("carreraVacia", "carrerasIds", "Seleccione carreras válidas y activas.")]
    [InlineData("carreraDuplicada", "carrerasIds", "No repita carreras.")]
    [InlineData("habilidadVacia", "habilidades", "Seleccione habilidades válidas.")]
    [InlineData("habilidadDuplicada", "habilidades", "No repita habilidades.")]
    public async Task Valida_selecciones_sin_consultar_la_base(string caso, string campo, string mensaje)
    {
        var solicitud = Solicitud();
        var id = solicitud.CarrerasIds[0];
        solicitud = caso switch
        {
            "sinCarreras" => solicitud with { CarrerasIds = [] },
            "carreraVacia" => solicitud with { CarrerasIds = [Guid.Empty] },
            "carreraDuplicada" => solicitud with { CarrerasIds = [id, id] },
            "habilidadVacia" => solicitud with { Habilidades = [new(Guid.Empty, false)] },
            "habilidadDuplicada" => solicitud with { Habilidades = [new(id, true), new(id, false)] },
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };

        var resultado = await validator.ValidateAsync(solicitud);

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo && e.ErrorMessage == mensaje);
    }

    [Fact]
    public async Task Colecciones_nulas_o_elementos_nulos_devuelven_errores_en_vez_de_excepciones()
    {
        var carreras = await validator.ValidateAsync(Solicitud() with { CarrerasIds = null! });
        var habilidades = await validator.ValidateAsync(Solicitud() with { Habilidades = null! });
        var elemento = await validator.ValidateAsync(Solicitud() with { Habilidades = [null!] });

        Assert.Contains(carreras.Errors, e => e.PropertyName == "carrerasIds");
        Assert.Contains(habilidades.Errors, e => e.PropertyName == "habilidades");
        Assert.Contains(elemento.Errors, e => e.PropertyName == "habilidades");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Rechaza_ayer_y_hoy_en_Honduras(int dias)
    {
        var resultado = await validator.ValidateAsync(Solicitud() with { FechaCierre = Hoy.AddDays(dias) });

        Assert.Contains(resultado.Errors, e => e.PropertyName == "fechaCierre"
            && e.ErrorMessage == "La fecha de cierre debe ser posterior a hoy.");
    }

    [Fact]
    public async Task Acepta_manana_de_Honduras_aunque_coincida_con_el_dia_UTC_actual()
    {
        var fecha = new FechaFija(Hoy); // 05:30 UTC del 16 todavía es 15 en Honduras.
        var validador = new CrearVacanteValidator(fecha);
        var solicitud = Solicitud() with { FechaCierre = new DateOnly(2026, 11, 16) };

        Assert.True((await validador.ValidateAsync(solicitud)).IsValid);
    }

    private static CrearVacanteRequest Solicitud() => new(
        "Practicante de desarrollo", "Desarrollo de APIs y pruebas automatizadas para el equipo de producto.",
        Modalidad.Hibrida, "Tegucigalpa", 2, true, 75m, Hoy.AddDays(1), [Guid.NewGuid()], [], false);

    private sealed class FechaFija(DateOnly hoy) : IFechaHonduras
    {
        public DateOnly Hoy() => hoy;
        public DateTime UtcAhora() => new(2026, 11, 16, 5, 30, 0, DateTimeKind.Utc);
    }
}
