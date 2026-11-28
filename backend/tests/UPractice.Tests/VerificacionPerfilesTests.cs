using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;
using static UPractice.Tests.PerfilEstudianteTests;

namespace UPractice.Tests;

/// <summary>HU-C02: la coordinación verifica o rechaza los perfiles de SU universidad.</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class VerificacionPerfilesTests(ApiFixture api)
{
    private const string Estudiantes = "/api/v1/coordinacion/estudiantes";

    [Fact]
    public async Task Lista_solo_perfiles_de_su_universidad_filtrados_por_estado()
    {
        await using var escenario = new EscenarioPerfil(api);
        var unah = await escenario.CrearEstudianteAsync(EstadoVerificacion.Pendiente);
        var unitec = await escenario.CrearEstudianteAsync(EstadoVerificacion.Pendiente, unitec: true);
        using var coordinador = await Cliente(Datos.CoordinadorUnah);

        var json = await coordinador.GetFromJsonAsync<JsonElement>($"{Estudiantes}?estadoVerificacion=Pendiente&tamanoPagina=50");

        var items = json.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, i => i.GetProperty("usuarioId").GetGuid() == unah.Id);
        Assert.DoesNotContain(items, i => i.GetProperty("usuarioId").GetGuid() == unitec.Id);
        Assert.All(items, i => Assert.Equal("Pendiente", i.GetProperty("estadoVerificacion").GetString()));
        var fila = items.Single(i => i.GetProperty("usuarioId").GetGuid() == unah.Id);
        Assert.False(string.IsNullOrEmpty(fila.GetProperty("numeroCuenta").GetString()));
        Assert.False(string.IsNullOrEmpty(fila.GetProperty("carrera").GetString()));
        Assert.Equal(50, json.GetProperty("tamanoPagina").GetInt32());
    }

    [Fact]
    public async Task Verificar_registra_quien_y_cuando_y_bloquea_al_estudiante()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync(EstadoVerificacion.Pendiente);
        var coordinadorUnah = await Datos.UsuarioAsync(api, Datos.CoordinadorUnah);
        using var coordinador = await Cliente(Datos.CoordinadorUnah);

        var respuesta = await coordinador.PatchAsJsonAsync($"{Estudiantes}/{estudiante.Id}/verificacion", new { aprobar = true });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var perfil = await escenario.PerfilAsync(estudiante.Id);
        Assert.Equal(EstadoVerificacion.Verificado, perfil!.EstadoVerificacion);
        Assert.Equal(coordinadorUnah.Id, perfil.VerificadoPorId);
        Assert.NotNull(perfil.FechaVerificacion);

        // Ya no es Pendiente: resolver otra vez es una transición inválida.
        await ProblemaAsync(await coordinador.PatchAsJsonAsync($"{Estudiantes}/{estudiante.Id}/verificacion", new { aprobar = false, observacion = "Ya no se puede rechazar." }),
            HttpStatusCode.UnprocessableEntity, "TRANSICION_INVALIDA");

        // HU-02: los datos académicos quedan bloqueados para el estudiante.
        using var cliente = await Cliente(estudiante.Correo);
        var carreraId = await escenario.CarreraAsync(await escenario.UniversidadAsync("unah.hn"));
        await ProblemaAsync(await cliente.PutAsJsonAsync("/api/v1/estudiantes/me/perfil/academico", new { numeroCuenta = "20220000001", carreraId, indiceAcademico = 90 }),
            HttpStatusCode.UnprocessableEntity, "PERFIL_VERIFICADO_INMUTABLE");
    }

    [Fact]
    public async Task Rechazar_exige_observacion_y_el_estudiante_la_ve()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync(EstadoVerificacion.Pendiente);
        using var coordinador = await Cliente(Datos.CoordinadorUnah);
        var ruta = $"{Estudiantes}/{estudiante.Id}/verificacion";

        Assert.Equal("Indique la observación (10 a 500 caracteres).", await ErrorCampoAsync(await coordinador.PatchAsJsonAsync(ruta, new { aprobar = false, observacion = "corta" }), "observacion"));
        Assert.Equal("Indique si verifica o rechaza el perfil.", await ErrorCampoAsync(await coordinador.PatchAsJsonAsync(ruta, new { }), "aprobar"));

        var respuesta = await coordinador.PatchAsJsonAsync(ruta, new { aprobar = false, observacion = "  El número de cuenta no coincide con registro.  " });
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);

        using var cliente = await Cliente(estudiante.Correo);
        var perfil = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/estudiantes/me/perfil");
        Assert.Equal("Rechazado", perfil.GetProperty("estadoVerificacion").GetString());
        Assert.Equal("El número de cuenta no coincide con registro.", perfil.GetProperty("observacionVerificacion").GetString());
    }

    [Fact]
    public async Task Coordinador_de_otra_universidad_recibe_404_y_otros_roles_403()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync(EstadoVerificacion.Pendiente);
        var ruta = $"{Estudiantes}/{estudiante.Id}/verificacion";

        using (var unitec = await Cliente(Datos.CoordinadorUnitec))
            await ProblemaAsync(await unitec.PatchAsJsonAsync(ruta, new { aprobar = true }), HttpStatusCode.NotFound);
        using (var reclutador = await Cliente(Datos.ReclutadorDigital))
            Assert.Equal(HttpStatusCode.Forbidden, (await reclutador.GetAsync(Estudiantes)).StatusCode);
        using (var alumno = await Cliente(Datos.EstudianteUnah))
            Assert.Equal(HttpStatusCode.Forbidden, (await alumno.PatchAsJsonAsync(ruta, new { aprobar = true })).StatusCode);

        Assert.Equal(EstadoVerificacion.Pendiente, (await escenario.PerfilAsync(estudiante.Id))!.EstadoVerificacion);
    }

    [Fact]
    public async Task Paginacion_invalida_da_400()
    {
        using var coordinador = await Cliente(Datos.CoordinadorUnah);

        var respuesta = await coordinador.GetAsync($"{Estudiantes}?pagina=0&tamanoPagina=99");

        var errores = (await ProblemaAsync(respuesta, HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.Equal("La página debe ser mayor o igual a 1.", errores.GetProperty("pagina")[0].GetString());
        Assert.Equal("El tamaño de página debe estar entre 1 y 50.", errores.GetProperty("tamanoPagina")[0].GetString());
    }

    private Task<HttpClient> Cliente(string correo) => api.ClienteAutenticadoAsync(correo, ApiFixture.PasswordDemo);
}
