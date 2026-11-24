using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Features.Vacantes;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

[Collection(ColeccionApi.Nombre)]
public sealed class GestionVacantesTests(ApiFixture api)
{
    private const string Ruta = "/api/v1/empresa/vacantes";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Empresa_sin_vacantes_recibe_una_pagina_vacia_con_totales_en_cero()
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        await escenario.VacanteAsync(empresaB: true);

        var pagina = await ObtenerAsync(cliente, Ruta);

        Assert.Empty(pagina.GetProperty("items").EnumerateArray());
        Assert.Equal(0, pagina.GetProperty("totalRegistros").GetInt32());
        Assert.Equal(0, pagina.GetProperty("totalPaginas").GetInt32());
        Assert.Equal(1, pagina.GetProperty("pagina").GetInt32());
        Assert.Equal(10, pagina.GetProperty("tamanoPagina").GetInt32());
    }

    [Fact]
    public async Task Listado_pagina_solo_vacantes_de_la_empresa_sin_repetir_ni_perder_registros()
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var propias = new List<Guid>();
        for (var i = 0; i < 12; i++)
            propias.Add((await escenario.VacanteAsync()).Id);
        var ajena = await escenario.VacanteAsync(empresaB: true);

        var primera = await ObtenerAsync(cliente, Ruta);
        var segunda = await ObtenerAsync(cliente, $"{Ruta}?pagina=2");
        var fuera = await ObtenerAsync(cliente, $"{Ruta}?pagina=2147483647&tamanoPagina=50");

        Assert.Equal(1, primera.GetProperty("pagina").GetInt32());
        Assert.Equal(10, primera.GetProperty("tamanoPagina").GetInt32());
        Assert.Equal(12, primera.GetProperty("totalRegistros").GetInt32());
        Assert.Equal(2, primera.GetProperty("totalPaginas").GetInt32());
        Assert.Equal(10, primera.GetProperty("items").GetArrayLength());
        Assert.Equal(2, segunda.GetProperty("items").GetArrayLength());
        var ids = Ids(primera).Concat(Ids(segunda)).ToArray();
        Assert.Equal(propias.Order(), ids.Order());
        Assert.DoesNotContain(ajena.Id, ids);
        Assert.Empty(fuera.GetProperty("items").EnumerateArray());
        Assert.Equal(12, fuera.GetProperty("totalRegistros").GetInt32());
    }

    [Fact]
    public async Task Filtro_y_paginacion_usan_estado_efectivo_y_GET_no_materializa_el_vencimiento()
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vencida = await escenario.VacanteAsync(EstadoVacante.Publicada, diasCierre: -1);
        var cerrada = await escenario.VacanteAsync(EstadoVacante.Cerrada);
        var hoy = await escenario.VacanteAsync(EstadoVacante.Publicada, diasCierre: 0);
        var vigente = await escenario.VacanteAsync(EstadoVacante.Publicada);
        var borrador = await escenario.VacanteAsync(EstadoVacante.Borrador, diasCierre: -1);
        var pausada = await escenario.VacanteAsync(EstadoVacante.Pausada, diasCierre: -1);

        var pagina1 = await ObtenerAsync(cliente, $"{Ruta}?estado=Cerrada&tamanoPagina=1");
        var pagina2 = await ObtenerAsync(cliente, $"{Ruta}?estado=Cerrada&tamanoPagina=1&pagina=2");
        Assert.Equal(2, pagina1.GetProperty("totalRegistros").GetInt32());
        Assert.Equal(2, pagina1.GetProperty("totalPaginas").GetInt32());
        Assert.Equal(new[] { vencida.Id, cerrada.Id }.Order(), Ids(pagina1).Concat(Ids(pagina2)).Order());
        foreach (var item in pagina1.GetProperty("items").EnumerateArray().Concat(pagina2.GetProperty("items").EnumerateArray()))
            Assert.Equal("Cerrada", item.GetProperty("estado").GetString());
        var publicadas = await ObtenerAsync(cliente, $"{Ruta}?estado=Publicada");
        Assert.Equal(new[] { hoy.Id, vigente.Id }.Order(), Ids(publicadas).Order());
        Assert.Equal("Borrador", (await ObtenerAsync(cliente, $"{Ruta}/{borrador.Id}")).GetProperty("estado").GetString());
        Assert.Equal("Pausada", (await ObtenerAsync(cliente, $"{Ruta}/{pausada.Id}")).GetProperty("estado").GetString());
        Assert.Equal("Cerrada", (await ObtenerAsync(cliente, $"{Ruta}/{vencida.Id}")).GetProperty("estado").GetString());
        var persistida = await escenario.GuardadaAsync(vencida.Id);
        Assert.Equal(EstadoVacante.Publicada, persistida.Estado);
        Assert.Equal(vencida.FechaCierre, persistida.FechaCierre);
    }

    [Fact]
    public async Task Detalle_y_listado_devuelven_contrato_completo_y_conteos_reales_de_postulaciones()
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(EstadoVacante.Publicada);
        await escenario.PostulacionesAsync(vacante, EstadoPostulacion.Enviada, EstadoPostulacion.Enviada, EstadoPostulacion.Oferta);
        var ajena = await escenario.VacanteAsync(EstadoVacante.Publicada, empresaB: true);
        await escenario.PostulacionesAsync(ajena, EstadoPostulacion.Oferta);

        var detalle = await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}");
        var listado = await ObtenerAsync(cliente, Ruta);
        var item = Assert.Single(listado.GetProperty("items").EnumerateArray());
        foreach (var dto in new[] { detalle, item })
        {
            Assert.Equal(vacante.Id, dto.GetProperty("id").GetGuid());
            Assert.Equal(vacante.Titulo, dto.GetProperty("titulo").GetString());
            Assert.Equal(vacante.Descripcion, dto.GetProperty("descripcion").GetString());
            Assert.Equal("Hibrida", dto.GetProperty("modalidad").GetString());
            Assert.Equal(escenario.Datos.UsuarioA.NombreCompleto, dto.GetProperty("creadaPor").GetString());
            Assert.Equal(3, dto.GetProperty("totalPostulaciones").GetInt32());
            var conteos = dto.GetProperty("postulacionesPorEstado");
            Assert.Equal(2, conteos.GetProperty("Enviada").GetInt32());
            Assert.Equal(1, conteos.GetProperty("Oferta").GetInt32());
            Assert.Equal(3, conteos.EnumerateObject().Sum(p => p.Value.GetInt32()));
            var carrera = Assert.Single(dto.GetProperty("carreras").EnumerateArray());
            Assert.Equal(escenario.Datos.CarreraA.Id, carrera.GetProperty("id").GetGuid());
            Assert.Equal(escenario.Datos.FacultadA.Nombre, carrera.GetProperty("facultad").GetString());
            Assert.Equal(escenario.Datos.UniversidadA.Id, carrera.GetProperty("universidadId").GetGuid());
            Assert.Equal(2, dto.GetProperty("habilidades").GetArrayLength());
        }
    }

    [Fact]
    public async Task Detalle_conserva_carreras_historicas_que_ya_no_estan_activas()
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api, carreraActiva: false);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();

        var detalle = await ObtenerAsync(cliente, $"{Ruta}/{vacante.Id}");

        Assert.Equal(escenario.Datos.CarreraA.Id,
            Assert.Single(detalle.GetProperty("carreras").EnumerateArray()).GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task Recursos_ajenos_e_inexistentes_devuelven_404_sin_modificarlos(string operacion)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var clienteA = await escenario.ClienteAsync();
        using var clienteB = await escenario.ClienteAsync(empresaB: true);
        var propia = await escenario.VacanteAsync();
        var ajena = await escenario.VacanteAsync(empresaB: true);
        foreach (var (cliente, idAjeno) in new[] { (clienteA, ajena.Id), (clienteB, propia.Id) })
        {
            foreach (var id in new[] { idAjeno, Guid.NewGuid() })
            {
                using var respuesta = await EjecutarAsync(cliente, operacion, id, escenario.Edicion());
                await ProblemaAsync(respuesta, HttpStatusCode.NotFound);
            }
        }
        var persistida = await escenario.GuardadaAsync(ajena.Id);
        Assert.Equal(EstadoVacante.Borrador, persistida.Estado);
        Assert.Equal(ajena.Titulo, persistida.Titulo);
        Assert.Equal(escenario.Datos.UsuarioB.Id, persistida.CreadaPorId);
        Assert.Equal(escenario.Datos.CarreraB.Id, Assert.Single(persistida.Carreras).CarreraId);
        var propiaPersistida = await escenario.GuardadaAsync(propia.Id);
        Assert.Equal(EstadoVacante.Borrador, propiaPersistida.Estado);
        Assert.Equal(propia.Titulo, propiaPersistida.Titulo);
        Assert.Equal(escenario.Datos.UsuarioA.Id, propiaPersistida.CreadaPorId);
        Assert.Equal(escenario.Datos.CarreraA.Id, Assert.Single(propiaPersistida.Carreras).CarreraId);
    }

    [Theory]
    [InlineData(EstadoVacante.Borrador, Rol.Reclutador)]
    [InlineData(EstadoVacante.Pausada, Rol.AdminEmpresa)]
    public async Task Editar_reemplaza_campos_y_puentes_atomicamente_sin_cambiar_identidad_ni_estado(
        EstadoVacante estado, Rol rol)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api, rol: rol);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(estado);
        var anterior = await escenario.GuardadaAsync(vacante.Id);
        var solicitud = escenario.Edicion();
        var cuerpo = JsonSerializer.SerializeToNode(solicitud, Json)!.AsObject();
        cuerpo["empresaId"] = escenario.Datos.EmpresaB.Id.ToString();
        cuerpo["creadaPorId"] = escenario.Datos.UsuarioB.Id.ToString();
        cuerpo["estado"] = "Publicada";

        // Repetir la misma edición también verifica que no duplique las claves de las tablas puente.
        for (var i = 0; i < 2; i++)
        {
            using var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{vacante.Id}", cuerpo);
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var dto = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(solicitud.Titulo.Trim(), dto.GetProperty("titulo").GetString());
            Assert.Equal(solicitud.Descripcion.Trim(), dto.GetProperty("descripcion").GetString());
            Assert.Equal(solicitud.Ubicacion.Trim(), dto.GetProperty("ubicacion").GetString());
            Assert.Equal("Remota", dto.GetProperty("modalidad").GetString());
            Assert.Equal(solicitud.Cupos, dto.GetProperty("cupos").GetInt32());
            Assert.False(dto.GetProperty("remunerada").GetBoolean());
            Assert.Equal(estado.ToString(), dto.GetProperty("estado").GetString());
            var persistida = await escenario.GuardadaAsync(vacante.Id);
            Assert.Equal(persistida.IndiceMinimo, dto.GetProperty("indiceMinimo").GetDecimal());
            Assert.Equal(escenario.Datos.EmpresaA.Id, persistida.EmpresaId);
            Assert.Equal(escenario.Datos.UsuarioA.Id, persistida.CreadaPorId);
            Assert.Equal(anterior.FechaPublicacion, persistida.FechaPublicacion);
            Assert.Equal(estado, persistida.Estado);
            Assert.Equal(solicitud.FechaCierre, persistida.FechaCierre);
            Assert.Equal(escenario.CarreraNueva.Id, Assert.Single(persistida.Carreras).CarreraId);
            var habilidad = Assert.Single(persistida.Habilidades);
            Assert.Equal(escenario.Datos.Habilidades[1].Id, habilidad.HabilidadId);
            Assert.True(habilidad.EsRequerida);
        }
    }

    [Theory]
    [InlineData(EstadoVacante.Publicada, 10)]
    [InlineData(EstadoVacante.Publicada, -1)]
    [InlineData(EstadoVacante.Cerrada, 10)]
    public async Task Editar_publicada_o_cerrada_devuelve_422_sin_cambiar_la_vacante(EstadoVacante estado, int dias)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(estado, dias);

        using var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{vacante.Id}", escenario.Edicion(), Json);

        await ProblemaAsync(respuesta, HttpStatusCode.UnprocessableEntity, "TRANSICION_INVALIDA");
        Assert.Equal(vacante.Titulo, (await escenario.GuardadaAsync(vacante.Id)).Titulo);
    }

    [Theory]
    [InlineData(EstadoVacante.Borrador)]
    [InlineData(EstadoVacante.Pausada)]
    public async Task Borrador_y_pausada_vencidos_permiten_corregir_fecha_mediante_edicion(EstadoVacante estado)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(estado, diasCierre: -1);

        using var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{vacante.Id}", escenario.Edicion(), Json);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(estado, (await escenario.GuardadaAsync(vacante.Id)).Estado);
    }

    [Theory]
    [InlineData("campos", "titulo")]
    [InlineData("carreraInexistente", "carrerasIds")]
    [InlineData("carreraDuplicada", "carrerasIds")]
    [InlineData("habilidadInexistente", "habilidades")]
    [InlineData("habilidadDuplicada", "habilidades")]
    public async Task Edicion_invalida_devuelve_400_y_conserva_campos_y_relaciones(string caso, string campo)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(EstadoVacante.Pausada);
        var solicitud = escenario.Edicion();
        solicitud = caso switch
        {
            "campos" => solicitud with { Titulo = "x", Cupos = 0 },
            "carreraInexistente" => solicitud with { CarrerasIds = [Guid.NewGuid()] },
            "carreraDuplicada" => solicitud with { CarrerasIds = [escenario.CarreraNueva.Id, escenario.CarreraNueva.Id] },
            "habilidadInexistente" => solicitud with { Habilidades = [new(Guid.NewGuid(), false)] },
            "habilidadDuplicada" => solicitud with { Habilidades = [new(escenario.Datos.Habilidades[0].Id, true), new(escenario.Datos.Habilidades[0].Id, false)] },
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };

        using var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{vacante.Id}", solicitud, Json);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.True(problema.GetProperty("errors").TryGetProperty(campo, out _));
        var persistida = await escenario.GuardadaAsync(vacante.Id);
        Assert.Equal(vacante.Titulo, persistida.Titulo);
        Assert.Equal(escenario.Datos.CarreraA.Id, Assert.Single(persistida.Carreras).CarreraId);
        Assert.Equal(2, persistida.Habilidades.Count);
    }

    [Fact]
    public async Task Edicion_rechaza_carrera_de_otra_universidad_sin_aprobacion()
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();
        var solicitud = escenario.Edicion() with { CarrerasIds = [escenario.Datos.CarreraB.Id] };

        using var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{vacante.Id}", solicitud, Json);

        await ProblemaAsync(respuesta, HttpStatusCode.UnprocessableEntity, "CARRERA_NO_PERMITIDA");
        Assert.Equal(vacante.Titulo, (await escenario.GuardadaAsync(vacante.Id)).Titulo);
    }

    [Theory]
    [InlineData(EstadoVacante.Borrador, EstadoVacante.Publicada)]
    [InlineData(EstadoVacante.Publicada, EstadoVacante.Pausada)]
    [InlineData(EstadoVacante.Pausada, EstadoVacante.Publicada)]
    [InlineData(EstadoVacante.Publicada, EstadoVacante.Cerrada)]
    [InlineData(EstadoVacante.Pausada, EstadoVacante.Cerrada)]
    public async Task Transiciones_permitidas_devuelven_204_y_conservan_fecha_de_primera_publicacion(
        EstadoVacante origen, EstadoVacante destino)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(origen);
        var anterior = await escenario.GuardadaAsync(vacante.Id);
        var antes = DateTime.UtcNow;

        using var respuesta = await CambiarAsync(cliente, vacante.Id, destino);
        var despues = DateTime.UtcNow;

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Empty(await respuesta.Content.ReadAsStringAsync());
        var persistida = await escenario.GuardadaAsync(vacante.Id);
        Assert.Equal(destino, persistida.Estado);
        Assert.Equal(anterior.CreadaPorId, persistida.CreadaPorId);
        if (origen == EstadoVacante.Borrador)
        {
            Assert.NotNull(persistida.FechaPublicacion);
            Assert.Equal(DateTimeKind.Utc, persistida.FechaPublicacion.Value.Kind);
            Assert.InRange(persistida.FechaPublicacion.Value, antes, despues);
        }
        else Assert.Equal(anterior.FechaPublicacion, persistida.FechaPublicacion);
    }

    [Theory]
    [InlineData(EstadoVacante.Borrador, EstadoVacante.Pausada)]
    [InlineData(EstadoVacante.Borrador, EstadoVacante.Cerrada)]
    [InlineData(EstadoVacante.Publicada, EstadoVacante.Publicada)]
    [InlineData(EstadoVacante.Pausada, EstadoVacante.Borrador)]
    [InlineData(EstadoVacante.Cerrada, EstadoVacante.Publicada)]
    public async Task Saltos_no_permitidos_devuelven_422_y_no_mutan_estado(EstadoVacante origen, EstadoVacante destino)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(origen);

        using var respuesta = await CambiarAsync(cliente, vacante.Id, destino);

        await ProblemaAsync(respuesta, HttpStatusCode.UnprocessableEntity, "TRANSICION_INVALIDA");
        Assert.Equal(origen, (await escenario.GuardadaAsync(vacante.Id)).Estado);
    }

    [Fact]
    public async Task Publicada_vencida_no_se_puede_pausar_para_volver_a_abrirla()
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(EstadoVacante.Publicada, diasCierre: -1);

        using var respuesta = await CambiarAsync(cliente, vacante.Id, EstadoVacante.Pausada);

        await ProblemaAsync(respuesta, HttpStatusCode.UnprocessableEntity, "TRANSICION_INVALIDA");
        Assert.Equal(EstadoVacante.Publicada, (await escenario.GuardadaAsync(vacante.Id)).Estado);
    }

    [Theory]
    [InlineData(EstadoVacante.Borrador)]
    [InlineData(EstadoVacante.Pausada)]
    public async Task Publicar_o_reanudar_en_el_dia_de_cierre_es_valido(EstadoVacante estado)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(estado, diasCierre: 0);

        using var respuesta = await CambiarAsync(cliente, vacante.Id, EstadoVacante.Publicada);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("sinAprobacion", "EMPRESA_NO_HABILITADA")]
    [InlineData("carreraAjena", "CARRERA_NO_PERMITIDA")]
    [InlineData("vencida", "VACANTE_NO_DISPONIBLE")]
    public async Task Publicar_revalida_habilitacion_carreras_y_vigencia_sin_cambios_parciales(string caso, string codigo)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api,
            aprobacion: caso == "sinAprobacion" ? EstadoHabilitacion.Pendiente : EstadoHabilitacion.Aprobada);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync(EstadoVacante.Pausada, diasCierre: caso == "vencida" ? -1 : 10);
        if (caso == "carreraAjena")
        {
            await using var db = api.CrearContextoSinUsuario();
            await db.VacanteCarreras.IgnoreQueryFilters().Where(c => c.VacanteId == vacante.Id).ExecuteDeleteAsync();
            db.VacanteCarreras.Add(new() { VacanteId = vacante.Id, CarreraId = escenario.Datos.CarreraB.Id });
            await db.SaveChangesAsync();
        }
        var anterior = await escenario.GuardadaAsync(vacante.Id);

        using var respuesta = await CambiarAsync(cliente, vacante.Id, EstadoVacante.Publicada);

        await ProblemaAsync(respuesta, HttpStatusCode.UnprocessableEntity, codigo);
        var persistida = await escenario.GuardadaAsync(vacante.Id);
        Assert.Equal(EstadoVacante.Pausada, persistida.Estado);
        Assert.Equal(anterior.FechaPublicacion, persistida.FechaPublicacion);
    }

    [Fact]
    public async Task Publicar_rechaza_carrera_inactiva_con_error_de_campo()
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api, carreraActiva: false);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();

        using var respuesta = await CambiarAsync(cliente, vacante.Id, EstadoVacante.Publicada);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.True(problema.GetProperty("errors").TryGetProperty("carrerasIds", out _));
        Assert.Equal(EstadoVacante.Borrador, (await escenario.GuardadaAsync(vacante.Id)).Estado);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"nuevoEstado\":\"Desconocido\"}")]
    [InlineData("{\"nuevoEstado\":999}")]
    public async Task Cambio_con_estado_omitido_o_desconocido_devuelve_400(string cuerpo)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();
        using var contenido = new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json");

        using var respuesta = await cliente.PatchAsync($"{Ruta}/{vacante.Id}/estado", contenido);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.NotEmpty(problema.GetProperty("errors").EnumerateObject());
        Assert.Equal(EstadoVacante.Borrador, (await escenario.GuardadaAsync(vacante.Id)).Estado);
    }

    [Theory]
    [InlineData("?pagina=0", "pagina")]
    [InlineData("?tamanoPagina=51", "tamanoPagina")]
    [InlineData("?estado=999", "estado")]
    [InlineData("?estado=Desconocido", "estado")]
    public async Task Listado_invalido_devuelve_400_por_campo(string consulta, string campo)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();

        using var respuesta = await cliente.GetAsync(Ruta + consulta);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.True(problema.GetProperty("errors").TryGetProperty(campo, out _));
    }

    [Theory]
    [InlineData(Datos.EstudianteUnah)]
    [InlineData(Datos.CoordinadorUnah)]
    [InlineData(Datos.SuperAdmin)]
    public async Task Roles_ajenos_a_empresa_no_pueden_consultar_editar_ni_cambiar_estado(string correo)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await api.ClienteAutenticadoAsync(correo,
            correo == Datos.SuperAdmin ? ApiFixture.PasswordSuperAdmin : ApiFixture.PasswordDemo);
        var vacante = await escenario.VacanteAsync();

        foreach (var operacion in new[] { "LISTADO", "GET", "PUT", "PATCH" })
        {
            using var respuesta = await EjecutarAsync(cliente, operacion, vacante.Id, escenario.Edicion());
            await ProblemaAsync(respuesta, HttpStatusCode.Forbidden);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rutas_de_gestion_exigen_token_y_password_temporal_resuelto(bool passwordTemporal)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api, passwordTemporal: passwordTemporal);
        using var cliente = passwordTemporal ? await escenario.ClienteAsync() : api.CreateClient();
        var vacante = await escenario.VacanteAsync();
        foreach (var operacion in new[] { "LISTADO", "GET", "PUT", "PATCH" })
        {
            using var respuesta = await EjecutarAsync(cliente, operacion, vacante.Id, escenario.Edicion());
            await ProblemaAsync(respuesta,
                passwordTemporal ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized,
                passwordTemporal ? "CAMBIO_PASSWORD_REQUERIDO" : null);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Identidad_o_empresa_desactivada_no_pueden_operar_con_token_previo(bool suspenderEmpresa)
    {
        await using var escenario = await EscenarioGestionVacantes.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var vacante = await escenario.VacanteAsync();
        await using (var db = api.CrearContextoSinUsuario())
        {
            if (suspenderEmpresa)
                await db.Empresas.Where(e => e.Id == escenario.Datos.EmpresaA.Id).ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.Activa, false).SetProperty(e => e.MotivoSuspension, "Suspensión de prueba HU-E04."));
            else
                await db.Usuarios.Where(u => u.Id == escenario.Datos.UsuarioA.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false));
        }

        foreach (var operacion in new[] { "LISTADO", "GET", "PUT", "PATCH" })
        {
            using var respuesta = await EjecutarAsync(cliente, operacion, vacante.Id, escenario.Edicion());
            await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        }
        Assert.Equal(EstadoVacante.Borrador, (await escenario.GuardadaAsync(vacante.Id)).Estado);
    }

    [Fact]
    public async Task OpenApi_documenta_las_cuatro_operaciones_y_sus_contratos()
    {
        using var cliente = api.CreateClient();
        var documento = await ObtenerAsync(cliente, "/openapi/v1.json");
        var paths = documento.GetProperty("paths");
        var listado = paths.GetProperty(Ruta).GetProperty("get");
        var detalle = paths.GetProperty(Ruta + "/{id}").GetProperty("get");
        var editar = paths.GetProperty(Ruta + "/{id}").GetProperty("put");
        var cambiar = paths.GetProperty(Ruta + "/{id}/estado").GetProperty("patch");
        Assert.True(listado.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(listado.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(detalle.GetProperty("responses").TryGetProperty("404", out _));
        Assert.True(editar.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(editar.GetProperty("responses").TryGetProperty("422", out _));
        Assert.True(cambiar.GetProperty("responses").TryGetProperty("204", out _));
        Assert.True(cambiar.GetProperty("responses").TryGetProperty("422", out _));
        var schemas = documento.GetProperty("components").GetProperty("schemas");
        var actualizacion = schemas.GetProperty("ActualizarVacanteRequest");
        Assert.False(actualizacion.GetProperty("properties").TryGetProperty("publicar", out _));
        Assert.Contains("nuevoEstado", schemas.GetProperty("CambiarEstadoVacanteRequest").GetProperty("required")
            .EnumerateArray().Select(x => x.GetString()));
    }

    private static async Task<JsonElement> ObtenerAsync(HttpClient cliente, string ruta)
    {
        using var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static IEnumerable<Guid> Ids(JsonElement pagina) =>
        pagina.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid());

    private static Task<HttpResponseMessage> CambiarAsync(HttpClient cliente, Guid id, EstadoVacante destino) =>
        cliente.PatchAsJsonAsync($"{Ruta}/{id}/estado", new CambiarEstadoVacanteRequest(destino), Json);

    private static Task<HttpResponseMessage> EjecutarAsync(HttpClient cliente, string operacion, Guid id, ActualizarVacanteRequest edicion) =>
        operacion switch
        {
            "LISTADO" => cliente.GetAsync(Ruta),
            "GET" => cliente.GetAsync($"{Ruta}/{id}"),
            "PUT" => cliente.PutAsJsonAsync($"{Ruta}/{id}", edicion, Json),
            "PATCH" => CambiarAsync(cliente, id, EstadoVacante.Publicada),
            _ => throw new ArgumentOutOfRangeException(nameof(operacion)),
        };

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
