using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>HU-02: perfil académico, habilidades y privacidad; y los catálogos que usa el formulario.</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class PerfilEstudianteTests(ApiFixture api)
{
    private const string Perfil = "/api/v1/estudiantes/me/perfil";
    private const string Academico = "/api/v1/estudiantes/me/perfil/academico";

    [Fact]
    public async Task Sin_perfil_devuelve_datos_academicos_nulos_y_estado_pendiente()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        using var cliente = await Cliente(estudiante.Correo);

        var json = await (await cliente.GetAsync(Perfil)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(estudiante.Id, json.GetProperty("usuarioId").GetGuid());
        Assert.Equal("Universidad Nacional Autónoma de Honduras", json.GetProperty("universidad").GetProperty("nombre").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("numeroCuenta").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("carrera").ValueKind);
        Assert.Equal("Pendiente", json.GetProperty("estadoVerificacion").GetString());
        Assert.False(json.GetProperty("privacidad").GetProperty("mostrarIndice").GetBoolean());
        Assert.Equal(0, json.GetProperty("habilidades").GetArrayLength());
    }

    [Fact]
    public async Task Completar_el_perfil_lo_deja_pendiente_con_la_carrera_de_su_universidad()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        var carreraId = await escenario.CarreraAsync(await escenario.UniversidadAsync("unah.hn"));
        using var cliente = await Cliente(estudiante.Correo);

        var respuesta = await cliente.PutAsJsonAsync(Academico, new { numeroCuenta = " 20221234567 ", carreraId, indiceAcademico = 85.25m });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("20221234567", json.GetProperty("numeroCuenta").GetString());
        Assert.Equal(carreraId, json.GetProperty("carrera").GetProperty("id").GetGuid());
        Assert.Equal(85.25m, json.GetProperty("indiceAcademico").GetDecimal());
        Assert.Equal("Pendiente", json.GetProperty("estadoVerificacion").GetString());
    }

    [Fact]
    public async Task Carrera_de_otra_universidad_o_numero_de_cuenta_repetido_dan_400_en_el_campo()
    {
        await using var escenario = new EscenarioPerfil(api);
        await escenario.CrearEstudianteAsync(EstadoVerificacion.Pendiente, numeroCuenta: "55500011122");
        var estudiante = await escenario.CrearEstudianteAsync();
        var carreraUnitec = await escenario.CarreraAsync(await escenario.UniversidadAsync("unitec.edu"));
        var carreraUnah = await escenario.CarreraAsync(await escenario.UniversidadAsync("unah.hn"));
        using var cliente = await Cliente(estudiante.Correo);

        var otraUniversidad = await cliente.PutAsJsonAsync(Academico, new { numeroCuenta = "20221234567", carreraId = carreraUnitec, indiceAcademico = 80 });
        var cuentaRepetida = await cliente.PutAsJsonAsync(Academico, new { numeroCuenta = "55500011122", carreraId = carreraUnah, indiceAcademico = 80 });

        Assert.Equal("Seleccione una carrera válida.", await ErrorCampoAsync(otraUniversidad, "carreraId"));
        Assert.Equal("El número de cuenta ya está registrado en su universidad.", await ErrorCampoAsync(cuentaRepetida, "numeroCuenta"));
        Assert.Null(await escenario.PerfilAsync(estudiante.Id));
    }

    [Fact]
    public async Task Validacion_sintactica_con_los_mensajes_del_contrato()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        using var cliente = await Cliente(estudiante.Correo);

        var respuesta = await cliente.PutAsJsonAsync(Academico, new { numeroCuenta = "12A", carreraId = Guid.Empty, indiceAcademico = 100.5m });

        var errores = (await ProblemaAsync(respuesta, HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.Equal("El número de cuenta debe tener solo dígitos (6 a 20).", errores.GetProperty("numeroCuenta")[0].GetString());
        Assert.Equal("Seleccione una carrera válida.", errores.GetProperty("carreraId")[0].GetString());
        Assert.Equal("El índice debe estar entre 0 y 100.", errores.GetProperty("indiceAcademico")[0].GetString());

        var sinIndice = await cliente.PutAsJsonAsync(Academico, new { numeroCuenta = "20221234567", carreraId = Guid.NewGuid() });
        Assert.Equal("El índice debe estar entre 0 y 100.", await ErrorCampoAsync(sinIndice, "indiceAcademico"));
    }

    [Fact]
    public async Task Verificado_bloquea_los_datos_academicos_pero_no_las_habilidades()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync(EstadoVerificacion.Verificado);
        var carreraId = await escenario.CarreraAsync(await escenario.UniversidadAsync("unah.hn"));
        using var cliente = await Cliente(estudiante.Correo);

        var academico = await cliente.PutAsJsonAsync(Academico, new { numeroCuenta = "20229999999", carreraId, indiceAcademico = 99 });
        await ProblemaAsync(academico, HttpStatusCode.UnprocessableEntity, "PERFIL_VERIFICADO_INMUTABLE");

        await using var db = api.CrearContextoSinUsuario();
        var habilidades = await db.Habilidades.OrderBy(h => h.Nombre).Select(h => h.Id).Take(2).ToListAsync();
        var respuesta = await cliente.PutAsJsonAsync("/api/v1/estudiantes/me/habilidades", new { habilidadesIds = habilidades });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("habilidades").GetArrayLength());
        Assert.Equal("Verificado", json.GetProperty("estadoVerificacion").GetString());
    }

    [Fact]
    public async Task Corregir_tras_un_rechazo_vuelve_a_pendiente_y_limpia_la_observacion()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync(EstadoVerificacion.Rechazado, observacion: "El índice no coincide con el historial.");
        var carreraId = await escenario.CarreraAsync(await escenario.UniversidadAsync("unah.hn"));
        using var cliente = await Cliente(estudiante.Correo);

        var antes = await (await cliente.GetAsync(Perfil)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("El índice no coincide con el historial.", antes.GetProperty("observacionVerificacion").GetString());

        var despues = await (await cliente.PutAsJsonAsync(Academico, new { numeroCuenta = "20228888888", carreraId, indiceAcademico = 78.5m }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Pendiente", despues.GetProperty("estadoVerificacion").GetString());
        Assert.Equal(JsonValueKind.Null, despues.GetProperty("observacionVerificacion").ValueKind);
    }

    [Fact]
    public async Task Habilidades_invalidas_o_repetidas_dan_400()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        using var cliente = await Cliente(estudiante.Correo);
        await using var db = api.CrearContextoSinUsuario();
        var habilidad = await db.Habilidades.Select(h => h.Id).FirstAsync();

        var repetidas = await cliente.PutAsJsonAsync("/api/v1/estudiantes/me/habilidades", new { habilidadesIds = new[] { habilidad, habilidad } });
        var inexistente = await cliente.PutAsJsonAsync("/api/v1/estudiantes/me/habilidades", new { habilidadesIds = new[] { Guid.NewGuid() } });

        Assert.Equal("No repita habilidades.", await ErrorCampoAsync(repetidas, "habilidadesIds"));
        Assert.Equal("Seleccione habilidades válidas.", await ErrorCampoAsync(inexistente, "habilidadesIds"));
    }

    [Fact]
    public async Task Privacidad_exige_perfil_y_se_refleja_en_el_perfil()
    {
        await using var escenario = new EscenarioPerfil(api);
        var sinPerfil = await escenario.CrearEstudianteAsync();
        var conPerfil = await escenario.CrearEstudianteAsync(EstadoVerificacion.Pendiente);

        using (var cliente = await Cliente(sinPerfil.Correo))
            await ProblemaAsync(await cliente.PutAsJsonAsync("/api/v1/estudiantes/me/privacidad", new { mostrarIndice = true }),
                HttpStatusCode.UnprocessableEntity, "PERFIL_INCOMPLETO");

        using var otro = await Cliente(conPerfil.Correo);
        Assert.Equal(HttpStatusCode.NoContent, (await otro.PutAsJsonAsync("/api/v1/estudiantes/me/privacidad", new { mostrarIndice = true })).StatusCode);
        var json = await (await otro.GetAsync(Perfil)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("privacidad").GetProperty("mostrarIndice").GetBoolean());

        // El booleano es obligatorio: omitirlo no lo apaga en silencio.
        Assert.Equal(HttpStatusCode.BadRequest, (await otro.PutAsJsonAsync("/api/v1/estudiantes/me/privacidad", new { })).StatusCode);
    }

    [Fact]
    public async Task Solo_el_estudiante_accede_a_su_perfil()
    {
        using var reclutador = await Cliente(Datos.ReclutadorDigital);
        using var coordinador = await Cliente(Datos.CoordinadorUnah);

        Assert.Equal(HttpStatusCode.Forbidden, (await reclutador.GetAsync(Perfil)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordinador.GetAsync(Perfil)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync(Perfil)).StatusCode);
    }

    [Fact]
    public async Task Catalogos_de_carreras_por_universidad_y_de_habilidades()
    {
        await using var escenario = new EscenarioPerfil(api);
        var unah = await escenario.UniversidadAsync("unah.hn");
        using var cliente = await Cliente(Datos.EstudianteUnah);

        var carreras = await cliente.GetFromJsonAsync<JsonElement>($"/api/v1/catalogos/carreras?universidadId={unah}");
        var habilidades = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/catalogos/habilidades");

        Assert.True(carreras.GetArrayLength() > 0);
        Assert.All(carreras.EnumerateArray(), c => Assert.Equal(unah, c.GetProperty("universidadId").GetGuid()));
        Assert.Contains(carreras.EnumerateArray(), c => c.GetProperty("nombre").GetString() == "Ingeniería en Sistemas");
        Assert.Contains(habilidades.EnumerateArray(), h => h.GetProperty("nombre").GetString() == "C#");
    }

    private Task<HttpClient> Cliente(string correo) => api.ClienteAutenticadoAsync(correo, ApiFixture.PasswordDemo);

    internal static async Task<JsonElement> ProblemaAsync(HttpResponseMessage respuesta, HttpStatusCode status, string? code = null)
    {
        Assert.Equal(status, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        if (code is not null) Assert.Equal(code, json.GetProperty("code").GetString());
        return json;
    }

    internal static async Task<string?> ErrorCampoAsync(HttpResponseMessage respuesta, string campo) =>
        (await ProblemaAsync(respuesta, HttpStatusCode.BadRequest)).GetProperty("errors").GetProperty(campo)[0].GetString();
}
