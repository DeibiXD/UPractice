using Microsoft.Extensions.DependencyInjection;
using UPractice.Application;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Vacantes;
using UPractice.Domain.Enums;

namespace UPractice.Tests;

public sealed class GestionVacantesValidatorTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 21);
    private readonly ActualizarVacanteValidator edicion;

    public GestionVacantesValidatorTests()
    {
        new ServiceCollection().AddApplication();
        edicion = new ActualizarVacanteValidator(new FechaFija());
    }

    [Fact]
    public async Task Edicion_acepta_campos_validos_con_indice_y_habilidades_opcionales()
    {
        Assert.True((await edicion.ValidateAsync(Solicitud() with { IndiceMinimo = null, Habilidades = [] })).IsValid);
    }

    [Theory]
    [InlineData("titulo", "titulo")]
    [InlineData("descripcion", "descripcion")]
    [InlineData("modalidad", "modalidad")]
    [InlineData("cupos", "cupos")]
    [InlineData("indice", "indiceMinimo")]
    [InlineData("carreras", "carrerasIds")]
    [InlineData("habilidades", "habilidades")]
    public async Task Edicion_conserva_validaciones_de_los_campos_del_contrato(string caso, string campo)
    {
        var solicitud = Solicitud();
        solicitud = caso switch
        {
            "titulo" => solicitud with { Titulo = "   ab   " },
            "descripcion" => solicitud with { Descripcion = "Breve" },
            "modalidad" => solicitud with { Modalidad = (Modalidad)999 },
            "cupos" => solicitud with { Cupos = 0 },
            "indice" => solicitud with { IndiceMinimo = 100.01m },
            "carreras" => solicitud with { CarrerasIds = [] },
            "habilidades" => solicitud with { Habilidades = [new(Guid.Empty, true)] },
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };

        var resultado = await edicion.ValidateAsync(solicitud);

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Edicion_rechaza_fecha_que_no_sea_posterior_a_hoy_de_Honduras(int dias)
    {
        var resultado = await edicion.ValidateAsync(Solicitud() with { FechaCierre = Hoy.AddDays(dias) });

        Assert.Contains(resultado.Errors, e => e.PropertyName == "fechaCierre"
            && e.ErrorMessage == "La fecha de cierre debe ser posterior a hoy.");
    }

    [Theory]
    [InlineData(0, 10, "pagina")]
    [InlineData(-1, 10, "pagina")]
    [InlineData(1, 0, "tamanoPagina")]
    [InlineData(1, 51, "tamanoPagina")]
    public async Task Listado_rechaza_paginacion_invalida(int pagina, int tamano, string campo)
    {
        var resultado = await new ListarVacantesValidator().ValidateAsync(
            new ListarVacantesRequest { Pagina = pagina, TamanoPagina = tamano });

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    [Fact]
    public async Task Listado_rechaza_estado_desconocido_y_acepta_sus_valores_definidos()
    {
        var validador = new ListarVacantesValidator();
        var invalido = await validador.ValidateAsync(new ListarVacantesRequest { Estado = (EstadoVacante)999 });
        Assert.Contains(invalido.Errors, e => e.PropertyName == "estado");
        foreach (var estado in Enum.GetValues<EstadoVacante>())
            Assert.True((await validador.ValidateAsync(new ListarVacantesRequest { Estado = estado })).IsValid);
    }

    [Fact]
    public async Task Cambio_valida_formato_del_estado_y_deja_la_maquina_de_estados_a_negocio()
    {
        var validador = new CambiarEstadoVacanteValidator();
        Assert.False((await validador.ValidateAsync(new CambiarEstadoVacanteRequest((EstadoVacante)999))).IsValid);
        foreach (var estado in Enum.GetValues<EstadoVacante>())
            Assert.True((await validador.ValidateAsync(new CambiarEstadoVacanteRequest(estado))).IsValid);
    }

    private static ActualizarVacanteRequest Solicitud() => new(
        "Practicante de desarrollo", "Desarrollo de APIs y pruebas automatizadas para el equipo de producto.",
        Modalidad.Hibrida, "Tegucigalpa", 2, true, 75m, Hoy.AddDays(1), [Guid.NewGuid()], []);

    private sealed class FechaFija : IFechaHonduras
    {
        public DateOnly Hoy() => GestionVacantesValidatorTests.Hoy;
        public DateTime UtcAhora() => new(2026, 11, 22, 5, 30, 0, DateTimeKind.Utc);
    }
}
