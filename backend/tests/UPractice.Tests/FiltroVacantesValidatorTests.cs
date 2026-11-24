using Microsoft.Extensions.DependencyInjection;
using UPractice.Application;
using UPractice.Application.Features.Vacantes;
using UPractice.Domain.Enums;

namespace UPractice.Tests;

public sealed class FiltroVacantesValidatorTests
{
    private readonly FiltroVacantesValidator validador;

    public FiltroVacantesValidatorTests()
    {
        new ServiceCollection().AddApplication();
        validador = new FiltroVacantesValidator();
    }

    [Fact]
    public async Task Consulta_sin_filtros_es_valida_y_pagina_de_diez_en_diez()
    {
        var consulta = new FiltroVacantesQuery();

        Assert.True((await validador.ValidateAsync(consulta)).IsValid);
        Assert.Equal(1, consulta.Pagina);
        Assert.Equal(10, consulta.TamanoPagina);
    }

    [Fact]
    public async Task Filtros_combinados_admiten_remunerada_false_y_tamano_maximo()
    {
        var consulta = new FiltroVacantesQuery("  backend  ", Guid.NewGuid(), Modalidad.Remota, false, 2, 50);

        Assert.True((await validador.ValidateAsync(consulta)).IsValid);
    }

    [Theory]
    [InlineData(0, 10, "pagina", "La página debe ser mayor o igual a 1.")]
    [InlineData(-1, 10, "pagina", "La página debe ser mayor o igual a 1.")]
    [InlineData(1, 0, "tamanoPagina", "El tamaño de página debe estar entre 1 y 50.")]
    [InlineData(1, -1, "tamanoPagina", "El tamaño de página debe estar entre 1 y 50.")]
    [InlineData(1, 51, "tamanoPagina", "El tamaño de página debe estar entre 1 y 50.")]
    public async Task Rechaza_paginacion_invalida_con_mensaje_compartido(int pagina, int tamano, string campo, string mensaje)
    {
        var resultado = await validador.ValidateAsync(new FiltroVacantesQuery(Pagina: pagina, TamanoPagina: tamano));

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo && e.ErrorMessage == mensaje);
    }

    [Fact]
    public async Task Rechaza_guid_vacio_sin_exigir_existencia_de_carrera_en_la_validacion_pura()
    {
        var invalido = await validador.ValidateAsync(new FiltroVacantesQuery(CarreraId: Guid.Empty));
        var valido = await validador.ValidateAsync(new FiltroVacantesQuery(CarreraId: Guid.NewGuid()));

        Assert.Contains(invalido.Errors, e => e.PropertyName == "carreraId" && e.ErrorMessage == "Seleccione una carrera válida.");
        Assert.True(valido.IsValid);
    }

    [Fact]
    public async Task Rechaza_modalidad_desconocida_y_acepta_los_valores_definidos()
    {
        var invalido = await validador.ValidateAsync(new FiltroVacantesQuery(Modalidad: (Modalidad)999));

        Assert.Contains(invalido.Errors, e => e.PropertyName == "modalidad" && e.ErrorMessage == "Seleccione una modalidad.");
        foreach (var modalidad in Enum.GetValues<Modalidad>())
            Assert.True((await validador.ValidateAsync(new FiltroVacantesQuery(Modalidad: modalidad))).IsValid);
    }

    [Fact]
    public async Task Busqueda_en_blanco_y_pagina_grande_son_consultas_validas()
    {
        Assert.True((await validador.ValidateAsync(new FiltroVacantesQuery(Busqueda: " \t "))).IsValid);
        Assert.True((await validador.ValidateAsync(new FiltroVacantesQuery(Pagina: int.MaxValue, TamanoPagina: 50))).IsValid);
    }
}
