using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>HU-A03: el SuperAdmin crea, desactiva y restablece cuentas de coordinador.</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class AdminCoordinadoresTests(ApiFixture api)
{
    private const string Ruta = "/api/v1/admin/coordinadores";

    [Fact]
    public async Task Crear_devuelve_password_temporal_y_obliga_a_cambiarla()
    {
        await using var escenario = new EscenarioAdmin(api);
        using var admin = await escenario.ClienteSuperAdminAsync();
        var unah = await escenario.UniversidadAsync("unah.hn");

        var respuesta = await admin.PostAsJsonAsync(Ruta, new
        {
            universidadId = unah, nombre1 = " Rosa ", nombre2 = "", apellido1 = "Meléndez", apellido2 = (string?)null,
            correo = $"  Coord.{Guid.NewGuid():N}@UNAH.hn ",
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuenta = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var id = cuenta.GetProperty("usuarioId").GetGuid();
        escenario.Registrar(id);
        var correo = cuenta.GetProperty("correo").GetString()!;
        var temporal = cuenta.GetProperty("passwordTemporal").GetString()!;
        Assert.Equal(correo.Trim().ToLowerInvariant(), correo);
        Assert.True(temporal.Length >= 12);

        var guardado = await escenario.UsuarioAsync(id);
        Assert.Equal("Rosa", guardado.Nombre1);
        Assert.Null(guardado.Nombre2);
        Assert.True(guardado.DebeCambiarPassword);
        Assert.NotEqual(temporal, guardado.PasswordHash);

        // Primer ingreso: solo puede consultar su sesión y cambiar la contraseña (D-17); todo lo demás da 403.
        using var coordinador = await api.ClienteAutenticadoAsync(correo, temporal);
        var me = await coordinador.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.Equal("Coordinador", me.GetProperty("rol").GetString());
        Assert.True(me.GetProperty("debeCambiarPassword").GetBoolean());
        await RespuestasApi.ProblemaAsync(await coordinador.GetAsync("/api/v1/catalogos/universidades"),
            HttpStatusCode.Forbidden, "CAMBIO_PASSWORD_REQUERIDO");

        var lista = await admin.GetFromJsonAsync<JsonElement>($"{Ruta}?universidadId={unah}");
        var fila = lista.EnumerateArray().Single(c => c.GetProperty("id").GetGuid() == id);
        Assert.Equal("Rosa Meléndez", fila.GetProperty("nombreCompleto").GetString());
        Assert.All(lista.EnumerateArray(), c => Assert.Equal(unah, c.GetProperty("universidad").GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task Desactivar_impide_el_login_y_reactivar_lo_permite()
    {
        await using var escenario = new EscenarioAdmin(api);
        using var admin = await escenario.ClienteSuperAdminAsync();
        var (id, correo, temporal) = await CrearAsync(admin, escenario);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"{Ruta}/{id}/estado", new { activo = false })).StatusCode);
        var login = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { correo, password = temporal });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"{Ruta}/{id}/estado", new { activo = true })).StatusCode);
        login = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { correo, password = temporal });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // El booleano es obligatorio: omitirlo no desactiva en silencio.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PatchAsJsonAsync($"{Ruta}/{id}/estado", new { })).StatusCode);
    }

    [Fact]
    public async Task Restablecer_genera_otra_temporal_y_la_anterior_deja_de_servir()
    {
        await using var escenario = new EscenarioAdmin(api);
        using var admin = await escenario.ClienteSuperAdminAsync();
        var (id, correo, temporal) = await CrearAsync(admin, escenario);

        var respuesta = await admin.PostAsync($"{Ruta}/{id}/restablecer-password", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var nueva = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("passwordTemporal").GetString()!;
        Assert.NotEqual(temporal, nueva);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { correo, password = temporal })).StatusCode);
        var login = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { correo, password = nueva });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True((await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuario").GetProperty("debeCambiarPassword").GetBoolean());
    }

    [Fact]
    public async Task Validaciones_correo_repetido_y_universidad_inactiva()
    {
        await using var escenario = new EscenarioAdmin(api);
        using var admin = await escenario.ClienteSuperAdminAsync();
        var unah = await escenario.UniversidadAsync("unah.hn");

        var invalido = await admin.PostAsJsonAsync(Ruta, new { universidadId = Guid.Empty, nombre1 = "R", apellido1 = "", correo = "no-es-correo" });
        var errores = (await RespuestasApi.ProblemaAsync(invalido, HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.Equal("Seleccione una universidad.", errores.GetProperty("universidadId")[0].GetString());
        Assert.Equal("Debe tener entre 2 y 50 letras.", errores.GetProperty("nombre1")[0].GetString());
        Assert.Equal("Debe tener entre 2 y 50 letras.", errores.GetProperty("apellido1")[0].GetString());
        Assert.Equal("Ingrese un correo válido.", errores.GetProperty("correo")[0].GetString());

        var repetido = await admin.PostAsJsonAsync(Ruta, new { universidadId = unah, nombre1 = "Rosa", apellido1 = "Meléndez", correo = "coordinacion@unah.hn" });
        await RespuestasApi.ProblemaAsync(repetido, HttpStatusCode.Conflict, "CORREO_EN_USO");

        var inactiva = await escenario.UniversidadInactivaAsync();
        var noValida = await admin.PostAsJsonAsync(Ruta, new { universidadId = inactiva, nombre1 = "Rosa", apellido1 = "Meléndez", correo = $"c.{Guid.NewGuid():N}@x.hn" });
        Assert.Equal("Seleccione una universidad activa.", await RespuestasApi.ErrorCampoAsync(noValida, "universidadId"));
    }

    [Fact]
    public async Task Solo_coordinadores_y_solo_SuperAdmin()
    {
        await using var escenario = new EscenarioAdmin(api);
        using var admin = await escenario.ClienteSuperAdminAsync();
        var ana = await Datos.UsuarioAsync(api, Datos.EstudianteUnah);

        // Un id que no es de coordinador (una estudiante) no existe para esta HU.
        await RespuestasApi.ProblemaAsync(await admin.PatchAsJsonAsync($"{Ruta}/{ana.Id}/estado", new { activo = false }), HttpStatusCode.NotFound);
        await RespuestasApi.ProblemaAsync(await admin.PostAsync($"{Ruta}/{ana.Id}/restablecer-password", null), HttpStatusCode.NotFound);
        Assert.True((await escenario.UsuarioAsync(ana.Id)).Activo);

        using var coordinador = await api.ClienteAutenticadoAsync(Datos.CoordinadorUnah, ApiFixture.PasswordDemo);
        using var reclutador = await api.ClienteAutenticadoAsync(Datos.ReclutadorDigital, ApiFixture.PasswordDemo);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordinador.GetAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reclutador.GetAsync(Ruta)).StatusCode);
    }

    private static async Task<(Guid Id, string Correo, string Temporal)> CrearAsync(HttpClient admin, EscenarioAdmin escenario)
    {
        var unah = await escenario.UniversidadAsync("unah.hn");
        var respuesta = await admin.PostAsJsonAsync(Ruta, new
        {
            universidadId = unah, nombre1 = "Carla", apellido1 = "Prueba", correo = $"coord.{Guid.NewGuid():N}@unah.hn",
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuenta = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var id = cuenta.GetProperty("usuarioId").GetGuid();
        escenario.Registrar(id);
        return (id, cuenta.GetProperty("correo").GetString()!, cuenta.GetProperty("passwordTemporal").GetString()!);
    }
}
