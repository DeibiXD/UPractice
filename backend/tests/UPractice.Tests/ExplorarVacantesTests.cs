using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Vacantes;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

[Collection(ColeccionApi.Nombre)]
public sealed class ExplorarVacantesTests(ApiFixture api)
{
    private const string Ruta = "/api/v1/vacantes";

    [Fact]
    public async Task Cada_estudiante_ve_empresas_habilitadas_para_su_universidad()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var clienteA = await escenario.ClienteAsync();
        using var clienteB = await escenario.ClienteAsync(estudianteB: true);
        var vacanteA = await escenario.VacanteAsync();
        var vacanteB = await escenario.VacanteAsync(empresaB: true);

        Assert.Equal(new[] { vacanteA.Id }, Ids(await ObtenerAsync(clienteA, Ruta)));
        Assert.Equal(new[] { vacanteB.Id }, Ids(await ObtenerAsync(clienteB, Ruta)));
        using (var ajenaA = await clienteA.GetAsync($"{Ruta}/{vacanteB.Id}"))
            await ProblemaAsync(ajenaA, HttpStatusCode.NotFound);
        using (var ajenaB = await clienteB.GetAsync($"{Ruta}/{vacanteA.Id}"))
            await ProblemaAsync(ajenaB, HttpStatusCode.NotFound);

        await escenario.HabilitacionAsync(empresaB: true, universidadB: false);
        Assert.Equal(new[] { vacanteA.Id, vacanteB.Id }.Order(), Ids(await ObtenerAsync(clienteA, Ruta)).Order());
    }

    [Theory]
    [InlineData(EstadoVacante.Borrador, 10)]
    [InlineData(EstadoVacante.Pausada, 10)]
    [InlineData(EstadoVacante.Cerrada, 10)]
    [InlineData(EstadoVacante.Publicada, -1)]
    public async Task Vacantes_no_publicadas_o_vencidas_no_se_exponen_en_listado_ni_detalle(EstadoVacante estado, int dias)
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var oculta = await escenario.VacanteAsync(estado: estado, diasCierre: dias);
        var visible = await escenario.VacanteAsync();

        Assert.Equal(new[] { visible.Id }, Ids(await ObtenerAsync(cliente, Ruta)));
        using var respuesta = await cliente.GetAsync($"{Ruta}/{oculta.Id}");
        await ProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task El_dia_de_cierre_en_Honduras_aun_es_vigente_y_la_consulta_no_modifica_datos()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var hoy = await escenario.VacanteAsync(diasCierre: 0);
        var vencida = await escenario.VacanteAsync(diasCierre: -1);

        Assert.Equal(new[] { hoy.Id }, Ids(await ObtenerAsync(cliente, Ruta)));
        var detalle = await ObtenerAsync(cliente, $"{Ruta}/{hoy.Id}");
        Assert.Equal(escenario.Hoy.ToString("yyyy-MM-dd"), detalle.GetProperty("fechaCierre").GetString());
        await using var db = api.CrearContextoSinUsuario();
        var persistidas = await db.Vacantes.IgnoreQueryFilters().Where(v => v.Id == hoy.Id || v.Id == vencida.Id).ToListAsync();
        Assert.All(persistidas, v => Assert.Equal(EstadoVacante.Publicada, v.Estado));
    }

    [Theory]
    [InlineData("pausada")]
    [InlineData("cerrada")]
    [InlineData("vencida")]
    [InlineData("empresaSuspendida")]
    [InlineData("habilitacionRevocada")]
    public async Task Una_postulacion_historica_no_reabre_la_visibilidad_del_catalogo(string caso)
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();
        await escenario.PostularAsync(vacante);
        await using (var db = api.CrearContextoSinUsuario())
        {
            if (caso == "empresaSuspendida")
                await db.Empresas.Where(e => e.Id == escenario.Datos.EmpresaA.Id).ExecuteUpdateAsync(s =>
                    s.SetProperty(e => e.Activa, false).SetProperty(e => e.MotivoSuspension, "Suspensión propia de la prueba."));
            else if (caso != "habilitacionRevocada")
                await db.Vacantes.IgnoreQueryFilters().Where(v => v.Id == vacante.Id).ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.Estado, caso == "pausada" ? EstadoVacante.Pausada :
                        caso == "cerrada" ? EstadoVacante.Cerrada : EstadoVacante.Publicada)
                    .SetProperty(v => v.FechaCierre, caso == "vencida" ? escenario.Hoy.AddDays(-1) : escenario.Hoy.AddDays(10)));
        }
        if (caso == "habilitacionRevocada")
            await escenario.HabilitacionAsync(empresaB: false, universidadB: false, estado: EstadoHabilitacion.Rechazada);
        // El filtro global de #44 conserva la vacante para consultar la postulación histórica.
        await using (var dbFiltrado = api.CrearContexto(new UsuarioDePrueba(escenario.EstudianteA.Id,
            Rol.Estudiante, UniversidadId: escenario.Datos.UniversidadA.Id)))
            Assert.True(await dbFiltrado.Vacantes.AnyAsync(v => v.Id == vacante.Id));

        var listado = await ObtenerAsync(cliente, Ruta);
        Assert.Empty(listado.GetProperty("items").EnumerateArray());
        Assert.Equal(0, listado.GetProperty("totalRegistros").GetInt32());
        using var respuesta = await cliente.GetAsync($"{Ruta}/{vacante.Id}");
        await ProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Filtra_texto_de_titulo_o_empresa_sin_importar_mayusculas_y_recorta_espacios()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        await escenario.HabilitacionAsync(empresaB: true, universidadB: false);
        var backend = await escenario.VacanteAsync("Practicante Backend");
        var diseno = await escenario.VacanteAsync("Practicante Diseño");
        await escenario.VacanteAsync("Practicante Finanzas", empresaB: true);

        var porTitulo = await ObtenerAsync(cliente, $"{Ruta}?busqueda={Uri.EscapeDataString("  bAcKeNd  ")}");
        Assert.Equal(new[] { backend.Id }, Ids(porTitulo));
        var porEmpresa = await ObtenerAsync(cliente, $"{Ruta}?busqueda={Uri.EscapeDataString("  EMPRESA A DE PRUEBA  ")}");
        Assert.Equal(new[] { backend.Id, diseno.Id }.Order(), Ids(porEmpresa).Order());
        var blanco = await ObtenerAsync(cliente, $"{Ruta}?busqueda={Uri.EscapeDataString("   ")}");
        Assert.Equal(3, blanco.GetProperty("totalRegistros").GetInt32());
    }

    [Fact]
    public async Task Los_filtros_se_combinan_y_remunerada_false_es_una_condicion_real()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var esperada = await escenario.VacanteAsync("Practicante Backend", modalidad: Modalidad.Remota, remunerada: false);
        await escenario.VacanteAsync("Practicante Diseño", modalidad: Modalidad.Remota, remunerada: false);
        await escenario.VacanteAsync("Practicante Backend", modalidad: Modalidad.Hibrida, remunerada: false);
        await escenario.VacanteAsync("Practicante Backend", modalidad: Modalidad.Remota, remunerada: true);
        await escenario.VacanteAsync("Practicante Backend", modalidad: Modalidad.Remota, remunerada: false,
            carrerasIds: [escenario.CarreraAlternativa.Id]);

        var pagina = await ObtenerAsync(cliente,
            $"{Ruta}?busqueda=backend&carreraId={escenario.Datos.CarreraA.Id}&modalidad=Remota&remunerada=false");

        Assert.Equal(new[] { esperada.Id }, Ids(pagina));
        Assert.Equal(1, pagina.GetProperty("totalRegistros").GetInt32());
    }

    [Fact]
    public async Task Carrera_es_filtro_opcional_y_una_carrera_inexistente_devuelve_pagina_vacia()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        await escenario.VacanteAsync();
        var alternativa = await escenario.VacanteAsync(carrerasIds: [escenario.CarreraAlternativa.Id]);

        Assert.Equal(2, (await ObtenerAsync(cliente, Ruta)).GetProperty("totalRegistros").GetInt32());
        Assert.Equal(new[] { alternativa.Id }, Ids(await ObtenerAsync(cliente, $"{Ruta}?carreraId={escenario.CarreraAlternativa.Id}")));
        var inexistente = await ObtenerAsync(cliente, $"{Ruta}?carreraId={Guid.NewGuid()}");
        Assert.Empty(inexistente.GetProperty("items").EnumerateArray());
        Assert.Equal(0, inexistente.GetProperty("totalRegistros").GetInt32());
        Assert.Equal(0, inexistente.GetProperty("totalPaginas").GetInt32());
    }

    [Fact]
    public async Task Pagina_de_diez_en_diez_ordenando_fecha_y_desempatando_por_id_sin_duplicados()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var fecha = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        var vacantes = new List<Vacante>();
        for (var i = 0; i < 12; i++)
            vacantes.Add(await escenario.VacanteAsync(fechaPublicacion: fecha.AddHours(i / 3)));
        await escenario.VacanteAsync(estado: EstadoVacante.Pausada, fechaPublicacion: fecha.AddYears(1));

        var primera = await ObtenerAsync(cliente, Ruta);
        var segunda = await ObtenerAsync(cliente, $"{Ruta}?pagina=2");
        var fuera = await ObtenerAsync(cliente, $"{Ruta}?pagina=2147483647&tamanoPagina=50");

        Assert.Equal(1, primera.GetProperty("pagina").GetInt32());
        Assert.Equal(10, primera.GetProperty("tamanoPagina").GetInt32());
        Assert.Equal(12, primera.GetProperty("totalRegistros").GetInt32());
        Assert.Equal(2, primera.GetProperty("totalPaginas").GetInt32());
        Assert.Equal(10, primera.GetProperty("items").GetArrayLength());
        Assert.Equal(2, segunda.GetProperty("items").GetArrayLength());
        Assert.Equal(vacantes.OrderByDescending(v => v.FechaPublicacion).ThenByDescending(v => v.Id).Select(v => v.Id),
            Ids(primera).Concat(Ids(segunda)));
        Assert.Empty(fuera.GetProperty("items").EnumerateArray());
        Assert.Equal(12, fuera.GetProperty("totalRegistros").GetInt32());
    }

    [Fact]
    public async Task Detalle_y_resumen_respetan_el_contrato_y_no_exponen_datos_internos_de_empresa()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(indiceMinimo: null);

        var detalle = await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}");
        var item = Assert.Single((await ObtenerAsync(cliente, Ruta)).GetProperty("items").EnumerateArray());
        var nombresResumen = new[] { "id", "titulo", "empresa", "modalidad", "ubicacion", "remunerada",
            "carreras", "fechaPublicacion", "fechaCierre", "cumploRequisitos", "yaPostulado" };
        Assert.Equal(nombresResumen.Order(), item.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(nombresResumen.Concat(["descripcion", "cupos", "carrerasDetalle", "indiceMinimo", "habilidades"]).Order(),
            detalle.EnumerateObject().Select(p => p.Name).Order());
        foreach (var dto in new[] { item, detalle })
        {
            Assert.Equal(vacante.Id, dto.GetProperty("id").GetGuid());
            Assert.Equal(vacante.Titulo, dto.GetProperty("titulo").GetString());
            Assert.Equal("Hibrida", dto.GetProperty("modalidad").GetString());
            Assert.Equal(vacante.Ubicacion, dto.GetProperty("ubicacion").GetString());
            Assert.True(dto.GetProperty("remunerada").GetBoolean());
            Assert.Equal(escenario.Datos.CarreraA.Nombre, Assert.Single(dto.GetProperty("carreras").EnumerateArray()).GetString());
            Assert.Equal(vacante.FechaCierre.ToString("yyyy-MM-dd"), dto.GetProperty("fechaCierre").GetString());
            Assert.Equal(DateTimeKind.Utc, dto.GetProperty("fechaPublicacion").GetDateTime().Kind);
            Assert.Equal(escenario.Datos.EmpresaA.Id, dto.GetProperty("empresa").GetProperty("id").GetGuid());
            Assert.Equal(escenario.Datos.EmpresaA.NombreComercial, dto.GetProperty("empresa").GetProperty("nombre").GetString());
            Assert.Equal(2, dto.GetProperty("empresa").EnumerateObject().Count());
            Assert.Contains(dto.GetProperty("cumploRequisitos").ValueKind, new[] { JsonValueKind.True, JsonValueKind.False });
            Assert.False(dto.GetProperty("yaPostulado").GetBoolean());
        }
        Assert.Equal(vacante.Descripcion, detalle.GetProperty("descripcion").GetString());
        Assert.Equal(2, detalle.GetProperty("cupos").GetInt32());
        Assert.Equal(JsonValueKind.Null, detalle.GetProperty("indiceMinimo").ValueKind);
        var carrera = Assert.Single(detalle.GetProperty("carrerasDetalle").EnumerateArray());
        Assert.Equal(escenario.Datos.CarreraA.Id, carrera.GetProperty("id").GetGuid());
        Assert.Equal(escenario.Datos.FacultadA.Nombre, carrera.GetProperty("facultad").GetString());
        Assert.Equal(escenario.Datos.UniversidadA.Id, carrera.GetProperty("universidadId").GetGuid());
        var habilidades = detalle.GetProperty("habilidades").EnumerateArray().ToArray();
        Assert.Equal(2, habilidades.Length);
        var requerida = Assert.Single(habilidades, h => h.GetProperty("esRequerida").GetBoolean());
        Assert.Equal(escenario.Datos.Habilidades[0].Id, requerida.GetProperty("id").GetGuid());
        Assert.Equal(escenario.Datos.Habilidades[0].Nombre, requerida.GetProperty("nombre").GetString());
        var deseable = Assert.Single(habilidades, h => !h.GetProperty("esRequerida").GetBoolean());
        Assert.Equal(escenario.Datos.Habilidades[1].Id, deseable.GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData(EstadoPostulacion.Enviada)]
    [InlineData(EstadoPostulacion.Retirada)]
    [InlineData(EstadoPostulacion.Rechazada)]
    public async Task YaPostulado_incluye_cualquier_postulacion_propia_aunque_se_haya_retirado(EstadoPostulacion estado)
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();
        await escenario.PostularAsync(vacante, estado: estado);

        Assert.True((await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}")).GetProperty("yaPostulado").GetBoolean());
        Assert.True(Assert.Single((await ObtenerAsync(cliente, Ruta)).GetProperty("items").EnumerateArray())
            .GetProperty("yaPostulado").GetBoolean());
    }

    [Fact]
    public async Task Postulacion_de_otro_estudiante_no_activa_mi_indicador()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();
        await escenario.PostularAsync(vacante, estudianteB: true);

        Assert.False((await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}")).GetProperty("yaPostulado").GetBoolean());
        Assert.False(Assert.Single((await ObtenerAsync(cliente, Ruta)).GetProperty("items").EnumerateArray())
            .GetProperty("yaPostulado").GetBoolean());
    }

    [Fact]
    public async Task Estudiante_sin_perfil_puede_explorar_y_no_se_marca_como_cumplidor()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api, conPerfil: false);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();

        Assert.False((await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}")).GetProperty("cumploRequisitos").GetBoolean());
        Assert.False(Assert.Single((await ObtenerAsync(cliente, Ruta)).GetProperty("items").EnumerateArray())
            .GetProperty("cumploRequisitos").GetBoolean());
    }

    [Theory]
    [InlineData(EstadoVerificacion.Pendiente)]
    [InlineData(EstadoVerificacion.Rechazado)]
    public async Task La_verificacion_del_perfil_no_bloquea_explorar(EstadoVerificacion verificacion)
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api, verificacion: verificacion);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();

        var resumen = Assert.Single((await ObtenerAsync(cliente, Ruta)).GetProperty("items").EnumerateArray());
        var detalle = await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}");
        Assert.Equal(vacante.Id, resumen.GetProperty("id").GetGuid());
        Assert.Equal(vacante.Id, detalle.GetProperty("id").GetGuid());
        Assert.True(resumen.GetProperty("cumploRequisitos").GetBoolean());
        Assert.True(detalle.GetProperty("cumploRequisitos").GetBoolean());
    }

    [Theory]
    [InlineData("indiceBajo", false)]
    [InlineData("carreraAjena", false)]
    [InlineData("faltaRequerida", false)]
    [InlineData("todasRequeridas", true)]
    [InlineData("faltaDeseable", true)]
    [InlineData("sinRequeridas", true)]
    [InlineData("sinIndiceMinimo", true)]
    [InlineData("limiteIndice", true)]
    public async Task CumploRequisitos_combina_carrera_indice_y_todas_las_habilidades_requeridas(string caso, bool esperado)
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var habilidades = caso switch
        {
            "faltaRequerida" or "todasRequeridas" => new[]
            {
                new HabilidadVacanteRequest(escenario.Datos.Habilidades[0].Id, true),
                new HabilidadVacanteRequest(escenario.Datos.Habilidades[1].Id, true),
            },
            "sinRequeridas" => [],
            _ => escenario.Datos.Solicitud().Habilidades,
        };
        if (caso is "faltaRequerida" or "todasRequeridas")
        {
            await using var db = api.CrearContextoSinUsuario();
            // Una habilidad de otro estudiante nunca debe satisfacer mis requisitos.
            db.EstudianteHabilidades.Add(new()
            {
                EstudianteId = caso == "faltaRequerida" ? escenario.EstudianteB.Id : escenario.EstudianteA.Id,
                HabilidadId = escenario.Datos.Habilidades[1].Id,
            });
            await db.SaveChangesAsync();
        }
        var vacante = await escenario.VacanteAsync(
            carrerasIds: [caso == "carreraAjena" ? escenario.CarreraAlternativa.Id : escenario.Datos.CarreraA.Id],
            indiceMinimo: caso == "sinIndiceMinimo" ? null : caso == "indiceBajo" ? 90m : caso == "limiteIndice" ? 85m : 75m,
            habilidades: habilidades);

        var detalle = await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}");
        var resumen = Assert.Single((await ObtenerAsync(cliente, Ruta)).GetProperty("items").EnumerateArray());
        Assert.Equal(esperado, detalle.GetProperty("cumploRequisitos").GetBoolean());
        Assert.Equal(esperado, resumen.GetProperty("cumploRequisitos").GetBoolean());
    }

    [Fact]
    public async Task El_indicador_es_personal_y_se_actualiza_con_las_habilidades_de_cada_estudiante()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var clienteA = await escenario.ClienteAsync();
        using var clienteB = await escenario.ClienteAsync(estudianteB: true);
        await escenario.HabilitacionAsync(empresaB: false, universidadB: true);
        var vacante = await escenario.VacanteAsync(carrerasIds: [escenario.Datos.CarreraA.Id, escenario.Datos.CarreraB.Id]);

        Assert.True((await ObtenerAsync(clienteA, $"{Ruta}/{vacante.Id}")).GetProperty("cumploRequisitos").GetBoolean());
        Assert.False((await ObtenerAsync(clienteB, $"{Ruta}/{vacante.Id}")).GetProperty("cumploRequisitos").GetBoolean());
        await using (var db = api.CrearContextoSinUsuario())
        {
            db.EstudianteHabilidades.Add(new()
            {
                EstudianteId = escenario.EstudianteB.Id, HabilidadId = escenario.Datos.Habilidades[0].Id,
            });
            await db.SaveChangesAsync();
        }
        Assert.True((await ObtenerAsync(clienteB, $"{Ruta}/{vacante.Id}")).GetProperty("cumploRequisitos").GetBoolean());
    }

    [Fact]
    public async Task Calcular_requisitos_actuales_no_reutiliza_ni_modifica_la_foto_de_la_postulacion()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(indiceMinimo: 75m);
        await escenario.PostularAsync(vacante);
        await using (var db = api.CrearContextoSinUsuario())
            await db.Vacantes.IgnoreQueryFilters().Where(v => v.Id == vacante.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.IndiceMinimo, 95m));

        var detalle = await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}");

        Assert.False(detalle.GetProperty("cumploRequisitos").GetBoolean());
        Assert.True(detalle.GetProperty("yaPostulado").GetBoolean());
        await using var verificacion = api.CrearContextoSinUsuario();
        Assert.True(await verificacion.Postulaciones.IgnoreQueryFilters()
            .Where(p => p.VacanteId == vacante.Id && p.EstudianteId == escenario.EstudianteA.Id)
            .Select(p => p.CumpleRequisitos).SingleAsync());
    }

    [Theory]
    [InlineData("?pagina=0", "pagina")]
    [InlineData("?tamanoPagina=51", "tamanoPagina")]
    [InlineData("?modalidad=999", "modalidad")]
    [InlineData("?modalidad=Desconocida", "modalidad")]
    [InlineData("?carreraId=00000000-0000-0000-0000-000000000000", "carreraId")]
    [InlineData("?carreraId=no-es-guid", "carreraId")]
    [InlineData("?remunerada=no-es-bool", "remunerada")]
    public async Task Filtros_invalidos_devuelven_400_por_campo(string consulta, string campo)
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();

        using var respuesta = await cliente.GetAsync(Ruta + consulta);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.True(problema.GetProperty("errors").TryGetProperty(campo, out _));
    }

    [Fact]
    public async Task Detalle_inexistente_devuelve_404_con_ProblemDetails()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();

        using var respuesta = await cliente.GetAsync($"{Ruta}/{Guid.NewGuid()}");

        await ProblemaAsync(respuesta, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(Datos.ReclutadorDigital)]
    [InlineData("admin@digitales.example.com")]
    [InlineData(Datos.CoordinadorUnah)]
    [InlineData(Datos.SuperAdmin)]
    public async Task Roles_distintos_de_estudiante_no_pueden_acceder(string correo)
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await api.ClienteAutenticadoAsync(correo,
            correo == Datos.SuperAdmin ? ApiFixture.PasswordSuperAdmin : ApiFixture.PasswordDemo);
        var vacante = await escenario.VacanteAsync();
        foreach (var ruta in new[] { Ruta, $"{Ruta}/{vacante.Id}" })
        {
            using var respuesta = await cliente.GetAsync(ruta);
            await ProblemaAsync(respuesta, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task Sin_token_no_se_puede_consultar_listado_ni_detalle()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = api.CreateClient();
        var vacante = await escenario.VacanteAsync();
        foreach (var ruta in new[] { Ruta, $"{Ruta}/{vacante.Id}" })
        {
            using var respuesta = await cliente.GetAsync(ruta);
            await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Estudiante_con_marca_temporal_en_BD_conserva_el_acceso_definido_por_IAM()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api, passwordTemporal: true);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();
        // GeneradorJwt excluye pwd_temp para Estudiante: D-17 distingue su registro propio
        // de las cuentas creadas por otra persona. Exploración conserva ese contrato de IAM.
        Assert.True(escenario.EstudianteA.DebeCambiarPassword);

        var listado = await ObtenerAsync(cliente, Ruta);
        var detalle = await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}");

        Assert.Equal(vacante.Id, Assert.Single(listado.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(vacante.Id, detalle.GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("inactivo")]
    [InlineData("universidad")]
    [InlineData("rol")]
    public async Task Una_identidad_modificada_despues_del_login_no_puede_usar_los_claims_antiguos(string cambio)
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();
        await using (var db = api.CrearContextoSinUsuario())
        {
            var consulta = db.Usuarios.Where(u => u.Id == escenario.EstudianteA.Id);
            if (cambio == "inactivo") await consulta.ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false));
            else if (cambio == "universidad")
                await consulta.ExecuteUpdateAsync(s => s.SetProperty(u => u.UniversidadId, escenario.Datos.UniversidadB.Id));
            else await consulta.ExecuteUpdateAsync(s => s.SetProperty(u => u.Rol, Rol.Coordinador));
        }

        foreach (var ruta in new[] { Ruta, $"{Ruta}/{vacante.Id}" })
        {
            using var respuesta = await cliente.GetAsync(ruta);
            await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Token_con_identidad_inexistente_devuelve_401()
    {
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        var inexistente = new Usuario
        {
            Id = Guid.NewGuid(), Rol = Rol.Estudiante, UniversidadId = escenario.Datos.UniversidadA.Id,
            Correo = "inexistente@vacantes.example.test", Nombre1 = "Inexistente", Apellido1 = "Prueba",
        };
        using var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            api.Servicio<IGeneradorToken>().Generar(inexistente).AccessToken);
        foreach (var ruta in new[] { Ruta, $"{Ruta}/{Guid.NewGuid()}" })
        {
            using var respuesta = await cliente.GetAsync(ruta);
            await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task OpenApi_documenta_las_dos_consultas_los_filtros_y_los_errores()
    {
        using var cliente = api.CreateClient();
        var documento = await ObtenerAsync(cliente, "/openapi/v1.json");
        var paths = documento.GetProperty("paths");
        var listado = paths.GetProperty(Ruta).GetProperty("get");
        var detalle = paths.GetProperty(Ruta + "/{id}").GetProperty("get");
        var parametros = listado.GetProperty("parameters").EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToArray();
        foreach (var nombre in new[] { "busqueda", "carreraId", "modalidad", "remunerada", "pagina", "tamanoPagina" })
            Assert.Contains(parametros, p => string.Equals(nombre, p, StringComparison.OrdinalIgnoreCase));
        foreach (var codigo in new[] { "200", "400", "401", "403" })
            Assert.True(listado.GetProperty("responses").TryGetProperty(codigo, out _));
        foreach (var codigo in new[] { "200", "401", "403", "404" })
            Assert.True(detalle.GetProperty("responses").TryGetProperty(codigo, out _));
        var schemas = documento.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("VacanteResumenDto", out _));
        Assert.True(schemas.TryGetProperty("VacanteDetalleDto", out _));
    }

    private static IEnumerable<Guid> Ids(JsonElement pagina) =>
        pagina.GetProperty("items").EnumerateArray().Select(v => v.GetProperty("id").GetGuid());

    private static async Task<JsonElement> ObtenerAsync(HttpClient cliente, string ruta)
    {
        using var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> ProblemaAsync(HttpResponseMessage respuesta, HttpStatusCode esperado, string? codigo = null)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)esperado, problema.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("traceId").GetString()));
        if (codigo is not null) Assert.Equal(codigo, problema.GetProperty("code").GetString());
        return problema;
    }
}
