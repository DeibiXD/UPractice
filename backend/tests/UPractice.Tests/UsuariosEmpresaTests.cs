using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Empresa.Usuarios;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

[Collection(ColeccionApi.Nombre)]
public sealed class UsuariosEmpresaTests(ApiFixture api)
{
    private const string Ruta = "/api/v1/empresa/usuarios";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Lista_solo_su_empresa_incluyendo_actor_e_inactivos_sin_exponer_secretos()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var clienteA = await escenario.ClienteAsync();
        using var clienteB = await escenario.ClienteAsync(empresaB: true);

        var listaA = await ObtenerAsync(clienteA, Ruta);
        var listaB = await ObtenerAsync(clienteB, Ruta);

        Assert.Equal(JsonValueKind.Array, listaA.ValueKind);
        var cuentasA = listaA.EnumerateArray().ToArray();
        Assert.Equal(new[] { escenario.Datos.UsuarioA.Id, escenario.ReclutadorA.Id, escenario.InactivoA.Id }.Order(),
            cuentasA.Select(u => u.GetProperty("id").GetGuid()).Order());
        Assert.Equal(escenario.Datos.UsuarioB.Id, Assert.Single(listaB.EnumerateArray()).GetProperty("id").GetGuid());
        var inactiva = Assert.Single(cuentasA, u => u.GetProperty("id").GetGuid() == escenario.InactivoA.Id);
        Assert.False(inactiva.GetProperty("activo").GetBoolean());
        var reclutador = Assert.Single(cuentasA, u => u.GetProperty("id").GetGuid() == escenario.ReclutadorA.Id);
        Assert.Equal("María José O'Connor López", reclutador.GetProperty("nombreCompleto").GetString());
        Assert.Equal("Reclutador", reclutador.GetProperty("rol").GetString());
        Assert.Equal(escenario.ReclutadorA.Correo, reclutador.GetProperty("correo").GetString());
        foreach (var cuenta in cuentasA.Concat(listaB.EnumerateArray()))
            Assert.Equal(new[] { "id", "nombreCompleto", "correo", "rol", "activo" }.Order(),
                cuenta.EnumerateObject().Select(p => p.Name).Order());
    }

    [Theory]
    [InlineData(Rol.Reclutador)]
    [InlineData(Rol.AdminEmpresa)]
    public async Task Crea_cuenta_con_tenant_del_actor_password_del_servidor_y_nombres_normalizados(Rol rol)
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var solicitud = escenario.Solicitud(rol);
        var cuerpo = JsonSerializer.SerializeToNode(solicitud, Json)!.AsObject();
        cuerpo["empresaId"] = escenario.Datos.EmpresaB.Id.ToString();
        cuerpo["universidadId"] = escenario.Datos.UniversidadB.Id.ToString();
        cuerpo["password"] = "Password-Cliente-2026";
        cuerpo["passwordTemporal"] = "Password-Cliente-2026";
        cuerpo["activo"] = false;
        cuerpo["debeCambiarPassword"] = false;

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, cuerpo);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(Ruta, respuesta.Headers.Location?.OriginalString);
        var cuenta = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "usuarioId", "correo", "passwordTemporal" }.Order(),
            cuenta.EnumerateObject().Select(p => p.Name).Order());
        var usuarioId = cuenta.GetProperty("usuarioId").GetGuid();
        var correo = cuenta.GetProperty("correo").GetString()!;
        var password = cuenta.GetProperty("passwordTemporal").GetString()!;
        Assert.Equal(solicitud.Correo.Trim().ToLowerInvariant(), correo);
        Assert.Equal(20, password.Length);
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsDigit);
        Assert.Contains(password, c => !char.IsLetterOrDigit(c));
        var guardada = await escenario.CuentaAsync(usuarioId);
        Assert.Equal(escenario.Datos.EmpresaA.Id, guardada.EmpresaId);
        Assert.Null(guardada.UniversidadId);
        Assert.Equal(rol, guardada.Rol);
        Assert.True(guardada.Activo);
        Assert.True(guardada.DebeCambiarPassword);
        Assert.Equal("Ana-María", guardada.Nombre1);
        Assert.Equal("Lucía", guardada.Nombre2);
        Assert.Equal("O'Connor", guardada.Apellido1);
        Assert.Equal("López", guardada.Apellido2);
        Assert.NotEqual(password, guardada.PasswordHash);
        Assert.True(api.Servicio<IHasherPassword>().Verificar(guardada.PasswordHash, password));
        using var clienteLogin = api.CreateClient();
        using var login = await clienteLogin.PostAsJsonAsync("/api/v1/auth/login", new { correo, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginCliente = await clienteLogin.PostAsJsonAsync("/api/v1/auth/login",
            new { correo, password = "Password-Cliente-2026" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginCliente.StatusCode);
        var listado = await ObtenerAsync(cliente, Ruta);
        var dto = Assert.Single(listado.EnumerateArray(), u => u.GetProperty("id").GetGuid() == usuarioId);
        Assert.Equal("Ana-María Lucía O'Connor López", dto.GetProperty("nombreCompleto").GetString());
        Assert.False(dto.TryGetProperty("passwordTemporal", out _));
        Assert.False(dto.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task Crear_no_exige_habilitacion_y_convierte_nombres_opcionales_en_blanco_a_null()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api, habilitada: false);
        using var cliente = await escenario.ClienteAsync();
        var solicitud = escenario.Solicitud() with { Nombre2 = "   ", Apellido2 = "" };

        var creada = await CrearAsync(cliente, solicitud);

        var usuario = await escenario.CuentaAsync(creada.GetProperty("usuarioId").GetGuid());
        Assert.Null(usuario.Nombre2);
        Assert.Null(usuario.Apellido2);
        Assert.Equal("Ana-María O'Connor", usuario.NombreCompleto);
    }

    [Fact]
    public async Task Cada_creacion_entrega_una_password_distinta_y_no_la_devuelve_en_consultas_posteriores()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();

        var primera = await CrearAsync(cliente, escenario.Solicitud());
        var segunda = await CrearAsync(cliente, escenario.Solicitud());
        var listado = await ObtenerAsync(cliente, Ruta);

        Assert.NotEqual(primera.GetProperty("passwordTemporal").GetString(), segunda.GetProperty("passwordTemporal").GetString());
        Assert.Equal(5, listado.GetArrayLength());
        Assert.All(listado.EnumerateArray(), cuenta => Assert.Equal(
            new[] { "id", "nombreCompleto", "correo", "rol", "activo" }.Order(),
            cuenta.EnumerateObject().Select(p => p.Name).Order()));
    }

    [Theory]
    [InlineData("mismaEmpresa")]
    [InlineData("otraEmpresa")]
    [InlineData("inactivo")]
    [InlineData("estudiante")]
    public async Task Correo_duplicado_es_global_incluso_con_espacios_y_mayusculas(string caso)
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var correo = caso switch
        {
            "mismaEmpresa" => escenario.ReclutadorA.Correo,
            "otraEmpresa" => escenario.Datos.UsuarioB.Correo,
            "inactivo" => escenario.InactivoA.Correo,
            "estudiante" => escenario.Estudiante.Correo,
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };
        var total = await escenario.TotalCuentasAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta,
            escenario.Solicitud(correo: "  " + correo.ToUpperInvariant() + "  "), Json);

        await ProblemaAsync(respuesta, HttpStatusCode.Conflict, "CORREO_EN_USO");
        Assert.Equal(total, await escenario.TotalCuentasAsync());
    }

    [Fact]
    public async Task Dos_creaciones_concurrentes_del_mismo_correo_producen_un_201_y_un_409()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var solicitud = escenario.Solicitud();
        var total = await escenario.TotalCuentasAsync();

        var primeraPendiente = cliente.PostAsJsonAsync(Ruta, solicitud, Json);
        var segundaPendiente = cliente.PostAsJsonAsync(Ruta, solicitud, Json);
        using var primera = await primeraPendiente;
        using var segunda = await segundaPendiente;

        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }.Order(),
            new[] { primera.StatusCode, segunda.StatusCode }.Order());
        await ProblemaAsync(primera.StatusCode == HttpStatusCode.Conflict ? primera : segunda,
            HttpStatusCode.Conflict, "CORREO_EN_USO");
        Assert.Equal(total + 1, await escenario.TotalCuentasAsync());
        await using var db = api.CrearContextoSinUsuario();
        Assert.Equal(1, await db.Usuarios.CountAsync(u => u.Correo == solicitud.Correo.Trim().ToLowerInvariant()));
    }

    [Theory]
    [InlineData("nombre", "nombre1")]
    [InlineData("correo", "correo")]
    [InlineData("rolEstudiante", "rol")]
    [InlineData("rolCoordinador", "rol")]
    [InlineData("rolSuperAdmin", "rol")]
    [InlineData("rolDesconocido", "rol")]
    public async Task Solicitud_invalida_devuelve_400_sin_crear_cuentas(string caso, string campo)
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var solicitud = escenario.Solicitud();
        solicitud = caso switch
        {
            "nombre" => solicitud with { Nombre1 = " A " },
            "correo" => solicitud with { Correo = "invalido" },
            "rolEstudiante" => solicitud with { Rol = Rol.Estudiante },
            "rolCoordinador" => solicitud with { Rol = Rol.Coordinador },
            "rolSuperAdmin" => solicitud with { Rol = Rol.SuperAdmin },
            "rolDesconocido" => solicitud with { Rol = (Rol)999 },
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };
        var total = await escenario.TotalCuentasAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, solicitud, Json);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.True(problema.GetProperty("errors").TryGetProperty(campo, out _));
        Assert.Equal(total, await escenario.TotalCuentasAsync());
    }

    [Fact]
    public async Task Omitir_rol_no_adopta_un_valor_de_enum_predeterminado()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var cuerpo = JsonSerializer.SerializeToNode(escenario.Solicitud(), Json)!.AsObject();
        cuerpo.Remove("rol");
        var total = await escenario.TotalCuentasAsync();

        using var respuesta = await cliente.PostAsJsonAsync(Ruta, cuerpo);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.NotEmpty(problema.GetProperty("errors").EnumerateObject());
        Assert.Equal(total, await escenario.TotalCuentasAsync());
    }

    [Fact]
    public async Task Cambiar_estado_de_otra_empresa_o_id_inexistente_devuelve_404_en_ambas_direcciones()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var clienteA = await escenario.ClienteAsync();
        using var clienteB = await escenario.ClienteAsync(empresaB: true);
        foreach (var (cliente, ajeno) in new[] { (clienteA, escenario.Datos.UsuarioB.Id), (clienteB, escenario.ReclutadorA.Id) })
        {
            foreach (var id in new[] { ajeno, Guid.NewGuid() })
            {
                using var respuesta = await CambiarAsync(cliente, id, false);
                await ProblemaAsync(respuesta, HttpStatusCode.NotFound);
            }
        }
        Assert.True((await escenario.CuentaAsync(escenario.Datos.UsuarioB.Id)).Activo);
        Assert.True((await escenario.CuentaAsync(escenario.ReclutadorA.Id)).Activo);
    }

    [Fact]
    public async Task No_puede_desactivarse_pero_activar_su_cuenta_es_idempotente()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();

        using var desactivar = await CambiarAsync(cliente, escenario.Datos.UsuarioA.Id, false);

        var problema = await ProblemaAsync(desactivar, HttpStatusCode.UnprocessableEntity, "TRANSICION_INVALIDA");
        Assert.Equal("No puede desactivar su propia cuenta.", problema.GetProperty("detail").GetString());
        Assert.True((await escenario.CuentaAsync(escenario.Datos.UsuarioA.Id)).Activo);
        for (var i = 0; i < 2; i++)
        {
            using var activar = await CambiarAsync(cliente, escenario.Datos.UsuarioA.Id, true);
            Assert.Equal(HttpStatusCode.NoContent, activar.StatusCode);
            Assert.Empty(await activar.Content.ReadAsStringAsync());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Desactivar_y_reactivar_conserva_password_y_marca_temporal_y_controla_el_login(bool temporal)
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        using var clienteLogin = api.CreateClient();
        if (temporal)
        {
            await using var db = api.CrearContextoSinUsuario();
            await db.Usuarios.Where(u => u.Id == escenario.ReclutadorA.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.DebeCambiarPassword, true));
        }
        var anterior = await escenario.CuentaAsync(escenario.ReclutadorA.Id);
        for (var i = 0; i < 2; i++)
        {
            using var respuesta = await CambiarAsync(cliente, anterior.Id, false);
            Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        }
        using (var loginInactivo = await clienteLogin.PostAsJsonAsync("/api/v1/auth/login",
            new { correo = anterior.Correo, password = ApiFixture.PasswordDemo }))
            Assert.Equal(HttpStatusCode.Unauthorized, loginInactivo.StatusCode);
        var desactivada = await escenario.CuentaAsync(anterior.Id);
        Assert.False(desactivada.Activo);
        Assert.Equal(anterior.PasswordHash, desactivada.PasswordHash);
        Assert.Equal(temporal, desactivada.DebeCambiarPassword);

        using var reactivar = await CambiarAsync(cliente, anterior.Id, true);

        Assert.Equal(HttpStatusCode.NoContent, reactivar.StatusCode);
        var reactivada = await escenario.CuentaAsync(anterior.Id);
        Assert.True(reactivada.Activo);
        Assert.Equal(anterior.PasswordHash, reactivada.PasswordHash);
        Assert.Equal(temporal, reactivada.DebeCambiarPassword);
        using var login = await clienteLogin.PostAsJsonAsync("/api/v1/auth/login",
            new { correo = anterior.Correo, password = ApiFixture.PasswordDemo });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"activo\":null}")]
    [InlineData("{\"activo\":\"incorrecto\"}")]
    public async Task Estado_omitido_o_mal_formado_no_desactiva_la_cuenta(string cuerpo)
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        using var contenido = new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json");

        using var respuesta = await cliente.PatchAsync($"{Ruta}/{escenario.ReclutadorA.Id}/estado", contenido);

        var problema = await ProblemaAsync(respuesta, HttpStatusCode.BadRequest);
        Assert.NotEmpty(problema.GetProperty("errors").EnumerateObject());
        Assert.True((await escenario.CuentaAsync(escenario.ReclutadorA.Id)).Activo);
    }

    [Theory]
    [InlineData(Rol.Reclutador)]
    [InlineData(Rol.AdminEmpresa)]
    public async Task Cuenta_nueva_cumple_flujo_de_password_temporal_y_permisos_del_rol(Rol rol)
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var administrador = await escenario.ClienteAsync();
        var creada = await CrearAsync(administrador, escenario.Solicitud(rol));
        var correo = creada.GetProperty("correo").GetString()!;
        var password = creada.GetProperty("passwordTemporal").GetString()!;
        using var nuevaCuenta = await api.ClienteAutenticadoAsync(correo, password);
        using var clienteLogin = api.CreateClient();
        using (var bloqueada = await nuevaCuenta.GetAsync(Ruta))
            await ProblemaAsync(bloqueada, HttpStatusCode.Forbidden,
                rol == Rol.AdminEmpresa ? "CAMBIO_PASSWORD_REQUERIDO" : null);

        const string nuevaPassword = "Nueva-Password-Pruebas-2026";
        using var cambio = await nuevaCuenta.PutAsJsonAsync("/api/v1/auth/password", new
        {
            passwordActual = password, passwordNuevo = nuevaPassword, confirmarPassword = nuevaPassword,
        });
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        var sesion = await cambio.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(sesion.GetProperty("usuario").GetProperty("debeCambiarPassword").GetBoolean());
        nuevaCuenta.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            sesion.GetProperty("accessToken").GetString());
        using var consulta = await nuevaCuenta.GetAsync(Ruta);
        Assert.Equal(rol == Rol.AdminEmpresa ? HttpStatusCode.OK : HttpStatusCode.Forbidden, consulta.StatusCode);
        var persistida = await escenario.CuentaAsync(creada.GetProperty("usuarioId").GetGuid());
        Assert.False(persistida.DebeCambiarPassword);
        Assert.True(api.Servicio<IHasherPassword>().Verificar(persistida.PasswordHash, nuevaPassword));
        using var loginViejo = await clienteLogin.PostAsJsonAsync("/api/v1/auth/login", new { correo, password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginViejo.StatusCode);
    }

    [Fact]
    public async Task Reclutador_creado_no_puede_leer_vacantes_ni_postulaciones_ajenas_en_endpoints_reales()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var administrador = await escenario.ClienteAsync();
        var creada = await CrearAsync(administrador, escenario.Solicitud());
        using var reclutador = await api.ClienteAutenticadoAsync(
            creada.GetProperty("correo").GetString()!, creada.GetProperty("passwordTemporal").GetString()!);
        using (var cambiar = await reclutador.PutAsJsonAsync("/api/v1/auth/password", new
        {
            passwordActual = creada.GetProperty("passwordTemporal").GetString(),
            passwordNuevo = "Reclutador-Pruebas-2026", confirmarPassword = "Reclutador-Pruebas-2026",
        }))
        {
            Assert.Equal(HttpStatusCode.OK, cambiar.StatusCode);
            reclutador.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await cambiar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        var propia = await escenario.RecursoAsync();
        var ajena = await escenario.RecursoAsync(empresaB: true);

        using (var listadoPropio = await reclutador.GetAsync($"/api/v1/empresa/vacantes/{propia.VacanteId}/postulaciones"))
            Assert.Equal(HttpStatusCode.OK, listadoPropio.StatusCode);
        using (var detallePropio = await reclutador.GetAsync($"/api/v1/empresa/postulaciones/{propia.PostulacionId}"))
            Assert.Equal(HttpStatusCode.OK, detallePropio.StatusCode);
        foreach (var rutaAjena in new[]
        {
            $"/api/v1/empresa/vacantes/{ajena.VacanteId}/postulaciones",
            $"/api/v1/empresa/postulaciones/{ajena.PostulacionId}",
        })
        {
            using var respuesta = await reclutador.GetAsync(rutaAjena);
            await ProblemaAsync(respuesta, HttpStatusCode.NotFound);
        }
    }

    [Theory]
    [InlineData(Datos.ReclutadorDigital)]
    [InlineData(Datos.EstudianteUnah)]
    [InlineData(Datos.CoordinadorUnah)]
    [InlineData(Datos.SuperAdmin)]
    public async Task Otros_roles_no_pueden_listar_crear_ni_cambiar_estado(string correo)
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await api.ClienteAutenticadoAsync(correo,
            correo == Datos.SuperAdmin ? ApiFixture.PasswordSuperAdmin : ApiFixture.PasswordDemo);
        var total = await escenario.TotalCuentasAsync();
        foreach (var operacion in new[] { "GET", "POST", "PATCH" })
        {
            using var respuesta = await EjecutarAsync(cliente, operacion, escenario);
            await ProblemaAsync(respuesta, HttpStatusCode.Forbidden);
        }
        Assert.Equal(total, await escenario.TotalCuentasAsync());
        Assert.True((await escenario.CuentaAsync(escenario.ReclutadorA.Id)).Activo);
    }

    [Fact]
    public async Task Las_tres_operaciones_exigen_token()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = api.CreateClient();
        foreach (var operacion in new[] { "GET", "POST", "PATCH" })
        {
            using var respuesta = await EjecutarAsync(cliente, operacion, escenario);
            await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Administrador_con_password_temporal_no_puede_gestionar_cuentas()
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api, passwordTemporal: true);
        using var cliente = await escenario.ClienteAsync();
        foreach (var operacion in new[] { "GET", "POST", "PATCH" })
        {
            using var respuesta = await EjecutarAsync(cliente, operacion, escenario);
            await ProblemaAsync(respuesta, HttpStatusCode.Forbidden, "CAMBIO_PASSWORD_REQUERIDO");
        }
    }

    [Theory]
    [InlineData("inactivo")]
    [InlineData("rol")]
    [InlineData("empresa")]
    [InlineData("empresaSuspendida")]
    public async Task Administrador_que_cambio_despues_del_login_no_puede_operar_con_claims_antiguos(string caso)
    {
        await using var escenario = await EscenarioUsuariosEmpresa.CrearAsync(api);
        using var cliente = await escenario.ClienteAsync();
        var total = await escenario.TotalCuentasAsync();
        await using (var db = api.CrearContextoSinUsuario())
        {
            var actor = db.Usuarios.Where(u => u.Id == escenario.Datos.UsuarioA.Id);
            if (caso == "inactivo") await actor.ExecuteUpdateAsync(s => s.SetProperty(u => u.Activo, false));
            else if (caso == "rol") await actor.ExecuteUpdateAsync(s => s.SetProperty(u => u.Rol, Rol.Reclutador));
            else if (caso == "empresa")
                await actor.ExecuteUpdateAsync(s => s.SetProperty(u => u.EmpresaId, escenario.Datos.EmpresaB.Id));
            else await db.Empresas.Where(e => e.Id == escenario.Datos.EmpresaA.Id).ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Activa, false).SetProperty(e => e.MotivoSuspension, "Suspensión propia de prueba."));
        }
        foreach (var operacion in new[] { "GET", "POST", "PATCH" })
        {
            using var respuesta = await EjecutarAsync(cliente, operacion, escenario);
            await ProblemaAsync(respuesta, HttpStatusCode.Unauthorized);
        }
        Assert.Equal(total, await escenario.TotalCuentasAsync());
        Assert.True((await escenario.CuentaAsync(escenario.ReclutadorA.Id)).Activo);
    }

    [Fact]
    public async Task OpenApi_documenta_operaciones_respuestas_y_campos_obligatorios()
    {
        using var cliente = api.CreateClient();
        var documento = await ObtenerAsync(cliente, "/openapi/v1.json");
        var paths = documento.GetProperty("paths");
        var listado = paths.GetProperty(Ruta).GetProperty("get");
        var crear = paths.GetProperty(Ruta).GetProperty("post");
        var cambiar = paths.GetProperty(Ruta + "/{id}/estado").GetProperty("patch");
        Assert.True(listado.GetProperty("responses").TryGetProperty("200", out _));
        foreach (var codigo in new[] { "201", "400", "401", "403", "409" })
            Assert.True(crear.GetProperty("responses").TryGetProperty(codigo, out _));
        foreach (var codigo in new[] { "204", "400", "401", "403", "404", "422" })
            Assert.True(cambiar.GetProperty("responses").TryGetProperty(codigo, out _));
        var schemas = documento.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("UsuarioEmpresaDto", out _));
        Assert.True(schemas.TryGetProperty("CuentaCreadaDto", out _));
        Assert.Contains("rol", schemas.GetProperty("CrearUsuarioEmpresaRequest").GetProperty("required")
            .EnumerateArray().Select(p => p.GetString()));
        Assert.Contains("activo", schemas.GetProperty("CambiarEstadoUsuarioRequest").GetProperty("required")
            .EnumerateArray().Select(p => p.GetString()));
    }

    private static Task<HttpResponseMessage> CambiarAsync(HttpClient cliente, Guid id, bool activo) =>
        cliente.PatchAsJsonAsync($"{Ruta}/{id}/estado", new CambiarEstadoUsuarioRequest(activo));

    private static Task<HttpResponseMessage> EjecutarAsync(HttpClient cliente, string operacion, EscenarioUsuariosEmpresa escenario) =>
        operacion switch
        {
            "GET" => cliente.GetAsync(Ruta),
            "POST" => cliente.PostAsJsonAsync(Ruta, escenario.Solicitud(), Json),
            "PATCH" => CambiarAsync(cliente, escenario.ReclutadorA.Id, false),
            _ => throw new ArgumentOutOfRangeException(nameof(operacion)),
        };

    private static async Task<JsonElement> CrearAsync(HttpClient cliente, CrearUsuarioEmpresaRequest solicitud)
    {
        using var respuesta = await cliente.PostAsJsonAsync(Ruta, solicitud, Json);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

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
