using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>
/// Prueba OBLIGATORIA de aislamiento (S3.3, T-03): con dos empresas en la base, un usuario de la empresa A
/// no puede listar ni leer datos de la empresa B. También comprueba el alcance de estudiantes y coordinadores.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public sealed class AislamientoTests(ApiFixture api)
{
    // ---------- Por HTTP, con token real ----------

    [Fact]
    public async Task Reclutador_solo_lista_las_vacantes_de_su_empresa()
    {
        var reclutador = await Datos.UsuarioAsync(api, Datos.ReclutadorDigital);
        var propias = (await Datos.VacantesAsync(api)).Where(v => v.EmpresaId == reclutador.EmpresaId).Select(v => v.Id).ToHashSet();
        var cliente = await api.ClienteAutenticadoAsync(Datos.ReclutadorDigital, ApiFixture.PasswordDemo);

        var visibles = await cliente.GetFromJsonAsync<List<Guid>>("/api/v1/pruebas/vacantes");

        Assert.NotEmpty(visibles!);
        Assert.True(propias.SetEquals(visibles!), "El reclutador debe ver exactamente las vacantes de su empresa, incluidos los borradores.");
    }

    [Fact]
    public async Task Pedir_por_id_una_vacante_de_otra_empresa_responde_404()
    {
        var reclutador = await Datos.UsuarioAsync(api, Datos.ReclutadorDigital);
        var ajena = (await Datos.VacantesAsync(api)).First(v => v.EmpresaId != reclutador.EmpresaId);
        var cliente = await api.ClienteAutenticadoAsync(Datos.ReclutadorDigital, ApiFixture.PasswordDemo);

        var respuesta = await cliente.GetAsync($"/api/v1/pruebas/vacantes/{ajena.Id}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode); // 404, no 403: no revela que existe
    }

    [Fact]
    public async Task Empresa_sin_postulantes_no_ve_las_postulaciones_de_otra()
    {
        var finanzas = await api.ClienteAutenticadoAsync(Datos.ReclutadorFinanzas, ApiFixture.PasswordDemo);
        var digital = await api.ClienteAutenticadoAsync(Datos.ReclutadorDigital, ApiFixture.PasswordDemo);

        Assert.Empty((await finanzas.GetFromJsonAsync<List<Guid>>("/api/v1/pruebas/postulaciones"))!);
        Assert.Single((await digital.GetFromJsonAsync<List<Guid>>("/api/v1/pruebas/postulaciones"))!);
    }

    [Fact]
    public async Task Estudiante_solo_ve_vacantes_publicadas_de_empresas_aprobadas_en_su_universidad()
    {
        var cliente = await api.ClienteAutenticadoAsync(Datos.EstudianteUnah, ApiFixture.PasswordDemo);
        var visibles = (await cliente.GetFromJsonAsync<List<Guid>>("/api/v1/pruebas/vacantes"))!;
        var todas = await Datos.VacantesAsync(api);

        Assert.NotEmpty(visibles);
        Assert.All(visibles, id => Assert.Equal(EstadoVacante.Publicada, todas.Single(v => v.Id == id).Estado));
        Assert.DoesNotContain(todas.Single(v => v.Estado == EstadoVacante.Borrador).Id, visibles);
    }

    // ---------- Directo sobre el DbContext, para cada rol ----------

    [Fact]
    public async Task Sin_usuario_los_filtros_no_devuelven_filas()
    {
        await using var db = api.CrearContextoSinUsuario();

        Assert.Equal(0, await db.Vacantes.CountAsync());
        Assert.Equal(0, await db.Postulaciones.CountAsync());
        Assert.Equal(0, await db.PerfilesEstudiante.CountAsync());
        Assert.Equal(0, await db.Curriculums.CountAsync());
        Assert.Equal(0, await db.HabilitacionesEmpresa.CountAsync());
    }

    [Fact]
    public async Task Empresa_solo_ve_perfiles_y_CV_de_quienes_se_le_postularon()
    {
        var digital = await Datos.UsuarioAsync(api, Datos.ReclutadorDigital);
        var finanzas = await Datos.UsuarioAsync(api, Datos.ReclutadorFinanzas);
        var ana = await Datos.UsuarioAsync(api, Datos.EstudianteUnah);

        await using (var db = api.CrearContexto(new UsuarioDePrueba(digital.Id, Rol.Reclutador, digital.EmpresaId)))
        {
            Assert.Equal([ana.Id], await db.PerfilesEstudiante.Select(p => p.UsuarioId).ToListAsync());
            Assert.Equal(1, await db.Curriculums.CountAsync());
            Assert.Equal(2, await db.HistorialPostulacion.CountAsync());
        }
        await using (var db = api.CrearContexto(new UsuarioDePrueba(finanzas.Id, Rol.Reclutador, finanzas.EmpresaId)))
        {
            Assert.Equal(0, await db.PerfilesEstudiante.CountAsync());
            Assert.Equal(0, await db.Curriculums.CountAsync());
            Assert.Equal(0, await db.HistorialPostulacion.CountAsync());
        }
    }

    [Fact]
    public async Task Coordinador_solo_ve_lo_de_su_universidad()
    {
        var unah = await Datos.UsuarioAsync(api, Datos.CoordinadorUnah);
        var unitec = await Datos.UsuarioAsync(api, Datos.CoordinadorUnitec);

        await using (var db = api.CrearContexto(new UsuarioDePrueba(unah.Id, Rol.Coordinador, UniversidadId: unah.UniversidadId)))
        {
            Assert.Equal(1, await db.Postulaciones.CountAsync());
            Assert.All(await db.PerfilesEstudiante.Include(p => p.Usuario).ToListAsync(),
                p => Assert.Equal(unah.UniversidadId, p.Usuario.UniversidadId));
            Assert.All(await db.HabilitacionesEmpresa.ToListAsync(), h => Assert.Equal(unah.UniversidadId, h.UniversidadId));
        }
        await using (var db = api.CrearContexto(new UsuarioDePrueba(unitec.Id, Rol.Coordinador, UniversidadId: unitec.UniversidadId)))
            Assert.Equal(0, await db.Postulaciones.CountAsync());
    }

    [Fact]
    public async Task SuperAdmin_supervisa_habilitaciones_pero_no_ve_datos_de_negocio()
    {
        var admin = await Datos.UsuarioAsync(api, Datos.SuperAdmin);
        await using var db = api.CrearContexto(new UsuarioDePrueba(admin.Id, Rol.SuperAdmin));

        Assert.True(await db.HabilitacionesEmpresa.CountAsync() >= 4);
        Assert.Equal(0, await db.Vacantes.CountAsync());
        Assert.Equal(0, await db.Postulaciones.CountAsync());
        Assert.Equal(0, await db.PerfilesEstudiante.CountAsync());
    }

    [Fact]
    public async Task Estudiante_solo_ve_sus_propias_postulaciones_y_CV()
    {
        var ana = await Datos.UsuarioAsync(api, Datos.EstudianteUnah);
        var diego = await Datos.UsuarioAsync(api, "diego.ramos@unah.hn");

        await using (var db = api.CrearContexto(new UsuarioDePrueba(ana.Id, Rol.Estudiante, UniversidadId: ana.UniversidadId)))
        {
            Assert.Equal(1, await db.Postulaciones.CountAsync());
            Assert.Equal(1, await db.Curriculums.CountAsync());
        }
        await using (var db = api.CrearContexto(new UsuarioDePrueba(diego.Id, Rol.Estudiante, UniversidadId: diego.UniversidadId)))
        {
            Assert.Equal(0, await db.Postulaciones.CountAsync());
            Assert.Equal(0, await db.Curriculums.CountAsync());
            Assert.Equal([diego.Id], await db.PerfilesEstudiante.Select(p => p.UsuarioId).ToListAsync());
        }
    }
}
