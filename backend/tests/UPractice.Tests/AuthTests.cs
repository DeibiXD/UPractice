using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Infrastructure.Auth;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>T-03: login, token cifrado (JWE) y contraseña temporal (D-15, D-17).</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class AuthTests(ApiFixture api)
{
    [Fact]
    public async Task Registro_crea_estudiante_asociado_y_rechaza_correo_duplicado()
    {
        var correo = $"registro.{Guid.NewGuid():N}@unah.hn";
        var request = new
        {
            nombre1 = "Elena",
            nombre2 = (string?)null,
            apellido1 = "Prueba",
            apellido2 = (string?)null,
            correo,
            password = "Estudiante-2026",
            confirmarPassword = "Estudiante-2026",
        };
        var cliente = api.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/auth/registro/estudiante", request);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = json.GetProperty("accessToken").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Equal("Estudiante", json.GetProperty("usuario").GetProperty("rol").GetString());
        Assert.Equal("UNAH", json.GetProperty("usuario").GetProperty("universidad").GetProperty("nombre").GetString());
        var minutosRestantes = (json.GetProperty("expiraEn").GetDateTime() - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(minutosRestantes, 59, 60);

        var jwt = new JsonWebTokenHandler { MapInboundClaims = false };
        var opciones = api.Servicio<OpcionesJwt>();
        var validacion = await jwt.ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = opciones.Issuer,
            ValidAudience = opciones.Audience,
            IssuerSigningKey = opciones.ClaveFirma,
            TokenDecryptionKey = opciones.ClaveCifrado,
            ClockSkew = TimeSpan.Zero,
        });
        Assert.True(validacion.IsValid, validacion.Exception?.Message);
        Assert.Equal(
            new[] { "role", "sub", "universidad_id" },
            validacion.ClaimsIdentity.Claims
                .Select(claim => claim.Type)
                .Where(tipo => tipo is not ("aud" or "exp" or "iat" or "iss" or "nbf"))
                .OrderBy(tipo => tipo));

        var duplicado = await cliente.PostAsJsonAsync("/api/v1/auth/registro/estudiante", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Equal("CORREO_EN_USO",
            (await duplicado.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("estudiante@noinstitucional.test")]
    [InlineData("estudiante@inactiva.test")]
    public async Task Registro_rechaza_dominio_no_institucional_o_inactivo(string correo)
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/registro/estudiante", new
        {
            nombre1 = "Elena",
            apellido1 = "Prueba",
            correo,
            password = "Estudiante-2026",
            confirmarPassword = "Estudiante-2026",
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("CORREO_NO_INSTITUCIONAL",
            (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Registro_valida_confirmacion_de_contrasena()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/registro/estudiante", new
        {
            nombre1 = "Elena",
            apellido1 = "Prueba",
            correo = $"confirmacion.{Guid.NewGuid():N}@unah.hn",
            password = "Estudiante-2026",
            confirmarPassword = "Otra-2026",
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal("Las contraseñas no coinciden.", errores.GetProperty("confirmarPassword")[0].GetString());
    }

    [Fact]
    public async Task Catalogo_publico_solo_devuelve_universidades_activas()
    {
        var respuesta = await api.CreateClient().GetAsync("/api/v1/catalogos/universidades");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var universidades = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(universidades.EnumerateArray(),
            universidad => universidad.GetProperty("dominioCorreo").GetString() == "unah.hn");
        Assert.DoesNotContain(universidades.EnumerateArray(),
            universidad => universidad.GetProperty("dominioCorreo").GetString() == "inactiva.test");
    }

    [Fact]
    public async Task Me_sin_token_devuelve_401()
    {
        var respuesta = await api.CreateClient().GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task OpenApi_publica_el_esquema_de_autorizacion_bearer()
    {
        var json = await api.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var bearer = json.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.NotEmpty(json.GetProperty("security").EnumerateArray());
    }

    [Fact]
    public async Task Login_devuelve_token_cifrado_que_no_se_puede_leer()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new { correo = Datos.ReclutadorDigital, password = ApiFixture.PasswordDemo });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("accessToken").GetString()!;

        // JWE compacto: 5 partes (encabezado, clave cifrada, IV, texto cifrado, etiqueta). Un JWT normal tiene 3.
        var partes = token.Split('.');
        Assert.Equal(5, partes.Length);
        var encabezado = Encoding.UTF8.GetString(Base64UrlDecode(partes[0]));
        Assert.Contains("\"alg\":\"A256KW\"", encabezado);
        Assert.Contains("\"enc\":\"A256CBC-HS512\"", encabezado);

        // Ninguna parte del token deja ver los claims: el EmpresaId viaja encriptado (lineamiento Fase 2).
        var empresaId = json.GetProperty("usuario").GetProperty("empresa").GetProperty("id").GetString()!;
        foreach (var parte in partes)
        {
            var texto = Encoding.UTF8.GetString(Base64UrlDecode(parte));
            Assert.DoesNotContain(empresaId, texto);
            Assert.DoesNotContain("empresa_id", texto);
        }

        var usuario = json.GetProperty("usuario");
        Assert.Equal("Reclutador", usuario.GetProperty("rol").GetString());
        Assert.Equal("Andrea Zelaya Flores", usuario.GetProperty("nombreCompleto").GetString());
        Assert.False(usuario.GetProperty("debeCambiarPassword").GetBoolean());
    }

    [Theory]
    [InlineData(Datos.ReclutadorDigital, "Incorrecta-123")]
    [InlineData("nadie@ejemplo.com", "Incorrecta-123")]
    public async Task Credenciales_incorrectas_dan_401_con_el_mismo_mensaje(string correo, string password)
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { correo, password });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Correo o contraseña incorrectos.", json.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Me_devuelve_los_datos_del_token()
    {
        var cliente = await api.ClienteAutenticadoAsync(Datos.EstudianteUnah, ApiFixture.PasswordDemo);
        var json = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");

        Assert.Equal(Datos.EstudianteUnah, json.GetProperty("correo").GetString());
        Assert.Equal("Estudiante", json.GetProperty("rol").GetString());
        Assert.Equal("UNAH", json.GetProperty("universidad").GetProperty("nombre").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("empresa").ValueKind);
    }

    [Fact]
    public async Task Token_alterado_da_401()
    {
        var cliente = await api.ClienteAutenticadoAsync(Datos.EstudianteUnah, ApiFixture.PasswordDemo);
        var token = cliente.DefaultRequestHeaders.Authorization!.Parameter!;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token[..^4] + "AAAA");

        var respuesta = await cliente.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Contrasena_temporal_bloquea_todo_salvo_me_y_cambio_de_password()
    {
        var correo = $"temporal.{Guid.NewGuid():N}@digitales.example.com";
        await CrearReclutadorConPasswordTemporalAsync(correo, "Temporal-123");
        var cliente = await api.ClienteAutenticadoAsync(correo, "Temporal-123");

        // Con contraseña temporal: /auth/me sí, cualquier otra ruta no.
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/auth/me")).StatusCode);
        var bloqueada = await cliente.GetAsync("/api/v1/pruebas/vacantes");
        Assert.Equal(HttpStatusCode.Forbidden, bloqueada.StatusCode);
        var problema = await bloqueada.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CAMBIO_PASSWORD_REQUERIDO", problema.GetProperty("code").GetString());

        // La contraseña actual incorrecta se reporta en su campo.
        var malActual = await cliente.PutAsJsonAsync("/api/v1/auth/password",
            new { passwordActual = "Otra-1234", passwordNuevo = "Nueva-12345", confirmarPassword = "Nueva-12345" });
        Assert.Equal(HttpStatusCode.BadRequest, malActual.StatusCode);
        var errores = (await malActual.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal("La contraseña actual no es correcta.", errores.GetProperty("passwordActual")[0].GetString());

        // Cambio correcto: token nuevo sin la marca y acceso normal.
        var cambio = await cliente.PutAsJsonAsync("/api/v1/auth/password",
            new { passwordActual = "Temporal-123", passwordNuevo = "Nueva-12345", confirmarPassword = "Nueva-12345" });
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        var nuevo = await cambio.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(nuevo.GetProperty("usuario").GetProperty("debeCambiarPassword").GetBoolean());

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nuevo.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/pruebas/vacantes")).StatusCode);
    }

    [Fact]
    public async Task Usuario_de_empresa_suspendida_no_puede_iniciar_sesion()
    {
        var correo = $"suspendida.{Guid.NewGuid():N}@ejemplo.com";
        await using (var db = api.CrearContextoSinUsuario())
        {
            var empresa = new Empresa
            {
                NombreComercial = "Suspendida", RazonSocial = "Suspendida S.A.", Sector = "Pruebas",
                Rtn = Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString(),
                Activa = false, MotivoSuspension = "Incumplimiento de las condiciones de uso.",
            };
            db.Empresas.Add(empresa);
            db.Usuarios.Add(new Usuario
            {
                Correo = correo, PasswordHash = api.Servicio<IHasherPassword>().Hashear("Clave-12345"),
                Nombre1 = "Ana", Apellido1 = "Prueba", Rol = Rol.AdminEmpresa, Empresa = empresa,
            });
            await db.SaveChangesAsync();
        }

        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { correo, password = "Clave-12345" });
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private async Task CrearReclutadorConPasswordTemporalAsync(string correo, string password)
    {
        var empresa = (await Datos.UsuarioAsync(api, Datos.ReclutadorDigital)).EmpresaId;
        await using var db = api.CrearContextoSinUsuario();
        db.Usuarios.Add(new Usuario
        {
            Correo = correo, PasswordHash = api.Servicio<IHasherPassword>().Hashear(password),
            Nombre1 = "Temporal", Apellido1 = "Prueba", Rol = Rol.Reclutador, EmpresaId = empresa,
            DebeCambiarPassword = true,
        });
        await db.SaveChangesAsync();
    }

    private static byte[] Base64UrlDecode(string texto)
    {
        var base64 = texto.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }
}
