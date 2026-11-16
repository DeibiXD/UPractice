using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

[Collection(ColeccionApi.Nombre)]
public sealed class CrearVacanteTests(ApiFixture api)
{
    private const string Ruta = "/api/v1/empresa/vacantes";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Theory]
    [InlineData(false, Rol.Reclutador)]
    [InlineData(true, Rol.Reclutador)]
    [InlineData(true, Rol.AdminEmpresa)]
    public async Task Crea_borrador_o_publicacion_con_contrato_y_relaciones_correctos(bool publicar, Rol rol)
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api, rol: rol);
        using var cliente = await escenario.ClienteAsync();
        var solicitud = escenario.Solicitud(publicar);
        if (!publicar) solicitud = solicitud with { IndiceMinimo = null, Habilidades = [] };
        else solicitud = solicitud with { IndiceMinimo = 75.125m };
        var antes = DateTime.UtcNow;

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, solicitud, Json);
        var despues = DateTime.UtcNow;

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetGuid();
        Assert.Equal($"{Ruta}/{id}", respuesta.Headers.Location?.OriginalString);
        Assert.Equal(solicitud.Titulo, json.GetProperty("titulo").GetString());
        Assert.Equal(solicitud.Descripcion, json.GetProperty("descripcion").GetString());
        Assert.Equal("Hibrida", json.GetProperty("modalidad").GetString());
        Assert.Equal(solicitud.Ubicacion, json.GetProperty("ubicacion").GetString());
        Assert.Equal(solicitud.Cupos, json.GetProperty("cupos").GetInt32());
        Assert.Equal(solicitud.Remunerada, json.GetProperty("remunerada").GetBoolean());
        Assert.Equal(publicar ? "Publicada" : "Borrador", json.GetProperty("estado").GetString());
        Assert.Equal(solicitud.FechaCierre.ToString("yyyy-MM-dd"), json.GetProperty("fechaCierre").GetString());
        Assert.Equal(escenario.UsuarioA.NombreCompleto, json.GetProperty("creadaPor").GetString());
        Assert.Equal(0, json.GetProperty("totalPostulaciones").GetInt32());
        Assert.Empty(json.GetProperty("postulacionesPorEstado").EnumerateObject());

        var carrera = Assert.Single(json.GetProperty("carreras").EnumerateArray());
        Assert.Equal(escenario.CarreraA.Id, carrera.GetProperty("id").GetGuid());
        Assert.Equal(escenario.CarreraA.Nombre, carrera.GetProperty("nombre").GetString());
        Assert.Equal(escenario.FacultadA.Nombre, carrera.GetProperty("facultad").GetString());
        Assert.Equal(escenario.UniversidadA.Id, carrera.GetProperty("universidadId").GetGuid());

        await using var db = api.CrearContextoSinUsuario();
        var guardada = await db.Vacantes.IgnoreQueryFilters().AsNoTracking()
            .Include(v => v.Carreras).Include(v => v.Habilidades).AsSplitQuery().SingleAsync(v => v.Id == id);
        Assert.Equal(escenario.EmpresaA.Id, guardada.EmpresaId);
        Assert.Equal(escenario.UsuarioA.Id, guardada.CreadaPorId);
        Assert.Equal(solicitud.CarrerasIds[0], Assert.Single(guardada.Carreras).CarreraId);

        var habilidades = json.GetProperty("habilidades").EnumerateArray().ToArray();
        Assert.Equal(solicitud.Habilidades.Length, habilidades.Length);
        Assert.Equal(solicitud.Habilidades.Length, guardada.Habilidades.Count);
        foreach (var habilidad in solicitud.Habilidades)
        {
            var dto = Assert.Single(habilidades, h => h.GetProperty("id").GetGuid() == habilidad.HabilidadId);
            Assert.Equal(habilidad.EsRequerida, dto.GetProperty("esRequerida").GetBoolean());
            Assert.Equal(escenario.Habilidades.Single(h => h.Id == habilidad.HabilidadId).Nombre, dto.GetProperty("nombre").GetString());
            Assert.Equal(habilidad.EsRequerida, guardada.Habilidades.Single(h => h.HabilidadId == habilidad.HabilidadId).EsRequerida);
        }

        if (publicar)
        {
            var publicacion = json.GetProperty("fechaPublicacion").GetDateTime();
            Assert.Equal(DateTimeKind.Utc, publicacion.Kind);
            Assert.InRange(publicacion, antes, despues);
            Assert.NotNull(guardada.FechaPublicacion);
            // PostgreSQL conserva microsegundos; DateTime puede incluir décimas de microsegundo.
            Assert.InRange(Math.Abs((guardada.FechaPublicacion.GetValueOrDefault() - publicacion).Ticks), 0L, TimeSpan.TicksPerMillisecond);
            Assert.Equal(guardada.IndiceMinimo, json.GetProperty("indiceMinimo").GetDecimal());
        }
        else
        {
            Assert.Null(guardada.FechaPublicacion);
            Assert.Null(guardada.IndiceMinimo);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("fechaPublicacion").ValueKind);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("indiceMinimo").ValueKind);
        }
    }

    [Fact]
    public async Task Dos_empresas_crean_en_su_tenant_aunque_el_body_intente_asignar_otra_empresa_y_autor()
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api);
        using var clienteA = await escenario.ClienteAsync();
        using var clienteB = await escenario.ClienteAsync(empresaB: true);
        var solicitudA = escenario.Solicitud();
        var bodyA = JsonSerializer.SerializeToNode(solicitudA, Json)!.AsObject();
        bodyA["empresaId"] = escenario.EmpresaB.Id.ToString();
        bodyA["creadaPorId"] = escenario.UsuarioB.Id.ToString();

        using var respuestaA = await clienteA.PostAsJsonAsync(Ruta, bodyA);
        using var respuestaB = await clienteB.PostAsJsonAsync(Ruta, escenario.Solicitud(empresaB: true), Json);

        Assert.Equal(HttpStatusCode.Created, respuestaA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, respuestaB.StatusCode);
        var idA = (await respuestaA.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var idB = (await respuestaB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await using var db = api.CrearContextoSinUsuario();
        var creadas = await db.Vacantes.IgnoreQueryFilters().AsNoTracking()
            .Where(v => v.Id == idA || v.Id == idB).ToListAsync();
        Assert.Equal(escenario.EmpresaA.Id, creadas.Single(v => v.Id == idA).EmpresaId);
        Assert.Equal(escenario.UsuarioA.Id, creadas.Single(v => v.Id == idA).CreadaPorId);
        Assert.Equal(escenario.EmpresaB.Id, creadas.Single(v => v.Id == idB).EmpresaId);
        Assert.Equal(escenario.UsuarioB.Id, creadas.Single(v => v.Id == idB).CreadaPorId);
    }

    [Theory]
    [InlineData(true, "EMPRESA_NO_HABILITADA")]
    [InlineData(false, "CARRERA_NO_PERMITIDA")]
    public async Task Empresa_sin_aprobaciones_rechaza_publicacion_primero_y_tambien_restringe_carreras_del_borrador(bool publicar, string codigo)
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api, habilitacionA: EstadoHabilitacion.Pendiente);
        using var cliente = await escenario.ClienteAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, escenario.Solicitud(publicar), Json);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.UnprocessableEntity);
        Assert.Equal(codigo, problema.GetProperty("code").GetString());
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Aprobacion_de_otra_empresa_no_autoriza_carreras_de_otra_universidad(bool publicar)
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var solicitud = escenario.Solicitud(publicar) with { CarrerasIds = [escenario.CarreraB.Id] };

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, solicitud, Json);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("CARRERA_NO_PERMITIDA", problema.GetProperty("code").GetString());
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Theory]
    [InlineData("carreraInexistente", "carrerasIds", "Seleccione carreras válidas y activas.")]
    [InlineData("carreraInactiva", "carrerasIds", "Seleccione carreras válidas y activas.")]
    [InlineData("universidadInactiva", "carrerasIds", "Seleccione carreras válidas y activas.")]
    [InlineData("habilidadInexistente", "habilidades", "Seleccione habilidades válidas.")]
    [InlineData("carrerasDuplicadas", "carrerasIds", "No repita carreras.")]
    [InlineData("habilidadesDuplicadas", "habilidades", "No repita habilidades.")]
    public async Task Selecciones_invalidas_devuelven_400_por_campo_sin_creacion_parcial(string caso, string campo, string mensaje)
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api,
            carreraActiva: caso != "carreraInactiva", universidadActiva: caso != "universidadInactiva");
        using var cliente = await escenario.ClienteAsync();
        var solicitud = escenario.Solicitud(publicar: true);
        solicitud = caso switch
        {
            "carreraInexistente" => solicitud with { CarrerasIds = [escenario.CarreraA.Id, Guid.NewGuid()] },
            "habilidadInexistente" => solicitud with { Habilidades = [new(escenario.Habilidades[0].Id, true), new(Guid.NewGuid(), false)] },
            "carrerasDuplicadas" => solicitud with { CarrerasIds = [escenario.CarreraA.Id, escenario.CarreraA.Id] },
            "habilidadesDuplicadas" => solicitud with { Habilidades = [new(escenario.Habilidades[0].Id, true), new(escenario.Habilidades[0].Id, false)] },
            _ => solicitud,
        };

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, solicitud, Json);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.Contains(mensaje, problema.GetProperty("errors").GetProperty(campo).EnumerateArray().Select(e => e.GetString()));
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Fact]
    public async Task Formulario_invalido_devuelve_errores_camelCase_y_no_guarda()
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var solicitud = escenario.Solicitud() with { Titulo = "x", Cupos = 0 };

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, solicitud, Json);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        var errores = problema.GetProperty("errors");
        Assert.Equal("El título debe tener entre 5 y 120 caracteres.", errores.GetProperty("titulo")[0].GetString());
        Assert.Equal("Los cupos deben estar entre 1 y 50.", errores.GetProperty("cupos")[0].GetString());
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Theory]
    [InlineData("modalidad")]
    [InlineData("remunerada")]
    [InlineData("publicar")]
    [InlineData("esRequerida")]
    public async Task Omitir_campos_obligatorios_no_adopta_cero_o_false_silenciosamente(string campo)
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var body = JsonSerializer.SerializeToNode(escenario.Solicitud(), Json)!.AsObject();
        if (campo == "esRequerida") body["habilidades"]![0]!.AsObject().Remove(campo);
        else body.Remove(campo);

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, body);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.NotEmpty(problema.GetProperty("errors").EnumerateObject());
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Theory]
    [InlineData(Datos.EstudianteUnah)]
    [InlineData(Datos.CoordinadorUnah)]
    [InlineData(Datos.SuperAdmin)]
    public async Task Roles_ajenos_a_empresa_no_pueden_crear_vacantes(string correo)
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api);
        using var cliente = await api.ClienteAutenticadoAsync(correo,
            correo == Datos.SuperAdmin ? ApiFixture.PasswordSuperAdmin : ApiFixture.PasswordDemo);

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, escenario.Solicitud(), Json);

        await ProblemaAsync(respuesta, HttpStatusCode.Forbidden);
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Fact]
    public async Task Sin_token_no_puede_crear_vacantes()
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api);
        using var cliente = api.CreateClient();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, escenario.Solicitud(), Json);

        await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Fact]
    public async Task Password_temporal_pendiente_bloquea_la_creacion()
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api, passwordTemporal: true);
        using var cliente = await escenario.ClienteAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, escenario.Solicitud(), Json);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.Forbidden);
        Assert.Equal("CAMBIO_PASSWORD_REQUERIDO", problema.GetProperty("code").GetString());
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Identidad_desactivada_despues_del_login_no_puede_crear(bool suspenderEmpresa)
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        await using (var db = api.CrearContextoSinUsuario())
        {
            if (suspenderEmpresa)
                await db.Empresas.Where(e => e.Id == escenario.EmpresaA.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Activa, false)
                        .SetProperty(e => e.MotivoSuspension, "Suspensión propia del escenario de prueba."));
            else
                await db.Usuarios.Where(u => u.Id == escenario.UsuarioA.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false));
        }

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, escenario.Solicitud(), Json);

        await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Fact]
    public async Task Token_valido_de_usuario_que_ya_no_existe_no_crea_vacante()
    {
        await using var escenario = await EscenarioVacante.CrearAsync(api);
        var inexistente = new Usuario
        {
            Id = Guid.NewGuid(), EmpresaId = escenario.EmpresaA.Id, Rol = Rol.Reclutador,
            Correo = "inexistente@vacantes.example.test", Nombre1 = "Usuario", Apellido1 = "Inexistente",
        };
        var token = api.Servicio<IGeneradorToken>().Generar(inexistente);
        using var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, escenario.Solicitud(), Json);

        await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        Assert.False(await escenario.TieneVacantesAsync());
    }

    [Fact]
    public async Task OpenApi_documenta_creacion_respuestas_y_campos_obligatorios()
    {
        using var cliente = api.CreateClient();
        using var respuesta = await cliente.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var documento = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var operacion = documento.GetProperty("paths").GetProperty(Ruta).GetProperty("post");
        foreach (var codigo in new[] { "201", "400", "401", "403", "422" })
            Assert.True(operacion.GetProperty("responses").TryGetProperty(codigo, out _));
        var esquemas = documento.GetProperty("components").GetProperty("schemas");
        var requeridos = esquemas.GetProperty("CrearVacanteRequest").GetProperty("required")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("modalidad", requeridos);
        Assert.Contains("remunerada", requeridos);
        Assert.Contains("publicar", requeridos);
        Assert.Contains("esRequerida", esquemas.GetProperty("HabilidadVacanteRequest").GetProperty("required")
            .EnumerateArray().Select(x => x.GetString()));
    }

    private static async Task<JsonElement> ProblemaAsync(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)esperado, problema.GetProperty("status").GetInt32());
        Assert.Equal(Ruta, problema.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("traceId").GetString()));
        return problema;
    }
}
