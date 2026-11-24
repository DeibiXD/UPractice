using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>
/// Paquete 4 (HU-05, HU-06, HU-E05, HU-E06, HU-E07). Cada prueba crea sus propios datos (EscenarioPostulaciones)
/// y los borra al terminar. Incluye la prueba de aislamiento entre empresas que exige la Definition of Done.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public sealed class PostulacionesTests(ApiFixture api)
{
    // ---------- Recorrido de la demostración + aislamiento entre empresas ----------

    [Fact]
    public async Task Recorrido_completo_y_otra_empresa_recibe_404()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync();
        var est = await e.CrearEstudianteAsync();
        var estudiante = await Cliente(est.Correo);
        var digital = await Cliente(Datos.ReclutadorDigital);
        var finanzas = await Cliente(Datos.ReclutadorFinanzas);

        // HU-05: el estudiante se postula.
        var crear = await estudiante.PostAsJsonAsync($"/api/v1/vacantes/{vacanteId}/postulaciones", new { curriculumId = est.CurriculumId });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var creada = await Json(crear);
        var id = creada.GetProperty("id").GetGuid();
        Assert.Equal("Enviada", creada.GetProperty("estado").GetString());
        Assert.Contains("Retirar", Acciones(creada));

        // HU-E05: la empresa dueña de la vacante ve al postulante.
        var lista = await Json(await digital.GetAsync($"/api/v1/empresa/vacantes/{vacanteId}/postulaciones"));
        Assert.Single(lista.EnumerateArray());
        Assert.Equal(id, lista[0].GetProperty("postulacionId").GetGuid());

        // Otra empresa: 404 (no 403) en la lista, el detalle y el cambio de estado.
        Assert.Equal(HttpStatusCode.NotFound, (await finanzas.GetAsync($"/api/v1/empresa/vacantes/{vacanteId}/postulaciones")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await finanzas.GetAsync($"/api/v1/empresa/postulaciones/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CambiarEstado(finanzas, id, "EnRevision")).StatusCode);

        // HU-E06 y HU-E07: detalle y cambios de estado de la empresa dueña.
        var detalle = await Json(await digital.GetAsync($"/api/v1/empresa/postulaciones/{id}"));
        Assert.Equal("Prueba Postulante", detalle.GetProperty("estudiante").GetProperty("nombreCompleto").GetString());
        Assert.Equal(est.CurriculumId, detalle.GetProperty("curriculum").GetProperty("id").GetGuid());

        var enRevision = await CambiarEstado(digital, id, "EnRevision", "Revisando el CV.");
        Assert.Equal(HttpStatusCode.OK, enRevision.StatusCode);
        var historial = (await Json(enRevision)).GetProperty("historial");
        Assert.Equal(2, historial.GetArrayLength());
        Assert.Equal("Revisando el CV.", historial[1].GetProperty("comentario").GetString());
        Assert.Equal("Andrea Zelaya Flores", historial[1].GetProperty("cambiadoPor").GetString());

        Assert.Equal(HttpStatusCode.OK, (await CambiarEstado(digital, id, "Oferta")).StatusCode);

        // HU-06: el estudiante ve la oferta y la acepta.
        var propia = await Json(await estudiante.GetAsync($"/api/v1/estudiantes/me/postulaciones/{id}"));
        Assert.Equal("Oferta", propia.GetProperty("estado").GetString());
        Assert.Equal(new[] { "AceptarOferta", "DeclinarOferta" }, Acciones(propia));

        var aceptada = await estudiante.PostAsJsonAsync($"/api/v1/estudiantes/me/postulaciones/{id}/respuesta-oferta", new { aceptar = true });
        Assert.Equal(HttpStatusCode.OK, aceptada.StatusCode);
        Assert.Equal("Aceptada", (await Json(aceptada)).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Un_estudiante_no_ve_ni_modifica_la_postulacion_de_otro()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync();
        var duena = await e.CrearEstudianteAsync();
        var intruso = await e.CrearEstudianteAsync();
        var id = await Postular(await Cliente(duena.Correo), vacanteId, duena.CurriculumId);
        var clienteIntruso = await Cliente(intruso.Correo);

        Assert.Equal(HttpStatusCode.NotFound, (await clienteIntruso.GetAsync($"/api/v1/estudiantes/me/postulaciones/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteIntruso.PostAsync($"/api/v1/estudiantes/me/postulaciones/{id}/retirar", null)).StatusCode);
        Assert.Empty((await Json(await clienteIntruso.GetAsync("/api/v1/estudiantes/me/postulaciones"))).EnumerateArray());
    }

    // ---------- Reglas al postularse (HU-05) ----------

    [Fact]
    public async Task Postularse_aplica_las_reglas_de_negocio()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync();
        var industrial = await e.CarreraIdAsync("Ingeniería Industrial");
        var vacanteIndustrial = await e.CrearVacanteAsync(carreraId: industrial);
        var vencida = await e.CrearVacanteAsync(diasParaCerrar: -2);   // 2 días: Honduras va 6 h detrás de UTC
        var ok = await e.CrearEstudianteAsync();
        var sinVerificar = await e.CrearEstudianteAsync(verificado: false);

        // Perfil sin verificar.
        var r1 = await (await Cliente(sinVerificar.Correo)).PostAsJsonAsync($"/api/v1/vacantes/{vacanteId}/postulaciones", new { curriculumId = sinVerificar.CurriculumId });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r1.StatusCode);
        Assert.Equal("PERFIL_NO_VERIFICADO", await Code(r1));

        var cliente = await Cliente(ok.Correo);

        // Carrera que la vacante no acepta.
        var r2 = await cliente.PostAsJsonAsync($"/api/v1/vacantes/{vacanteIndustrial}/postulaciones", new { curriculumId = ok.CurriculumId });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r2.StatusCode);
        Assert.Equal("CARRERA_NO_ELEGIBLE", await Code(r2));

        // Fecha de cierre vencida.
        var r3 = await cliente.PostAsJsonAsync($"/api/v1/vacantes/{vencida}/postulaciones", new { curriculumId = ok.CurriculumId });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r3.StatusCode);
        Assert.Equal("VACANTE_NO_DISPONIBLE", await Code(r3));

        // CV de otro estudiante: no existe para este usuario.
        var r4 = await cliente.PostAsJsonAsync($"/api/v1/vacantes/{vacanteId}/postulaciones", new { curriculumId = sinVerificar.CurriculumId });
        Assert.Equal(HttpStatusCode.NotFound, r4.StatusCode);

        // Validación sintáctica: mismo mensaje que el esquema Zod.
        var r5 = await cliente.PostAsJsonAsync($"/api/v1/vacantes/{vacanteId}/postulaciones", new { curriculumId = Guid.Empty });
        Assert.Equal(HttpStatusCode.BadRequest, r5.StatusCode);
        Assert.Equal("Seleccione un currículum.", (await Json(r5)).GetProperty("errors").GetProperty("curriculumId")[0].GetString());

        // Duplicada.
        await Postular(cliente, vacanteId, ok.CurriculumId);
        var r6 = await cliente.PostAsJsonAsync($"/api/v1/vacantes/{vacanteId}/postulaciones", new { curriculumId = ok.CurriculumId });
        Assert.Equal(HttpStatusCode.Conflict, r6.StatusCode);
        Assert.Equal("POSTULACION_DUPLICADA", await Code(r6));

        // Una vacante que no existe (o que el estudiante no puede ver) responde 404.
        var r7 = await cliente.PostAsJsonAsync($"/api/v1/vacantes/{Guid.NewGuid()}/postulaciones", new { curriculumId = ok.CurriculumId });
        Assert.Equal(HttpStatusCode.NotFound, r7.StatusCode);
    }

    [Fact]
    public async Task El_indice_minimo_es_deseable_y_el_indice_solo_se_muestra_con_consentimiento()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync(indiceMinimo: 95m);
        var bajo = await e.CrearEstudianteAsync(indice: 80m, mostrarIndice: true);
        var alto = await e.CrearEstudianteAsync(indice: 99m, mostrarIndice: false);
        var digital = await Cliente(Datos.ReclutadorDigital);

        var idBajo = await Postular(await Cliente(bajo.Correo), vacanteId, bajo.CurriculumId);   // no excluye: se postula igual
        var idAlto = await Postular(await Cliente(alto.Correo), vacanteId, alto.CurriculumId);

        var dBajo = await Json(await digital.GetAsync($"/api/v1/empresa/postulaciones/{idBajo}"));
        Assert.False(dBajo.GetProperty("cumpleRequisitos").GetBoolean());
        Assert.Equal(80m, dBajo.GetProperty("estudiante").GetProperty("indiceAcademico").GetDecimal());

        var dAlto = await Json(await digital.GetAsync($"/api/v1/empresa/postulaciones/{idAlto}"));
        Assert.True(dAlto.GetProperty("cumpleRequisitos").GetBoolean());
        Assert.Equal(JsonValueKind.Null, dAlto.GetProperty("estudiante").GetProperty("indiceAcademico").ValueKind);
    }

    // ---------- Roles ----------

    [Fact]
    public async Task Cada_ruta_exige_su_rol()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync();
        var est = await e.CrearEstudianteAsync();
        var estudiante = await Cliente(est.Correo);
        var digital = await Cliente(Datos.ReclutadorDigital);

        Assert.Equal(HttpStatusCode.Forbidden, (await digital.GetAsync("/api/v1/estudiantes/me/postulaciones")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await digital.PostAsJsonAsync($"/api/v1/vacantes/{vacanteId}/postulaciones", new { curriculumId = est.CurriculumId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await estudiante.GetAsync($"/api/v1/empresa/vacantes/{vacanteId}/postulaciones")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("/api/v1/estudiantes/me/postulaciones")).StatusCode);
    }

    // ---------- Panel del estudiante (HU-06) ----------

    [Fact]
    public async Task Retirar_y_reactivar_con_otro_cv_siguen_la_maquina_de_estados()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync();
        var est = await e.CrearEstudianteAsync();
        var estudiante = await Cliente(est.Correo);
        var digital = await Cliente(Datos.ReclutadorDigital);
        var id = await Postular(estudiante, vacanteId, est.CurriculumId);

        var retirada = await estudiante.PostAsync($"/api/v1/estudiantes/me/postulaciones/{id}/retirar", null);
        Assert.Equal(HttpStatusCode.OK, retirada.StatusCode);
        var jr = await Json(retirada);
        Assert.Equal("Retirada", jr.GetProperty("estado").GetString());
        Assert.Equal(new[] { "Reactivar" }, Acciones(jr));

        // La empresa no puede mover una postulación retirada ni asignar estados que son del estudiante.
        var invalida = await CambiarEstado(digital, id, "EnRevision");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalida.StatusCode);
        Assert.Equal("TRANSICION_INVALIDA", await Code(invalida));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CambiarEstado(digital, id, "Aceptada")).StatusCode);

        // Retirar de nuevo no es una transición válida.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await estudiante.PostAsync($"/api/v1/estudiantes/me/postulaciones/{id}/retirar", null)).StatusCode);

        var reactivada = await estudiante.PostAsJsonAsync($"/api/v1/estudiantes/me/postulaciones/{id}/reactivar", new { curriculumId = est.CurriculumId });
        Assert.Equal(HttpStatusCode.OK, reactivada.StatusCode);
        var jv = await Json(reactivada);
        Assert.Equal("Enviada", jv.GetProperty("estado").GetString());
        Assert.Equal(3, jv.GetProperty("historial").GetArrayLength());   // Enviada → Retirada → Enviada
    }

    [Fact]
    public async Task El_estudiante_sigue_viendo_su_postulacion_si_la_vacante_se_pausa()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync();
        var est = await e.CrearEstudianteAsync();
        var estudiante = await Cliente(est.Correo);
        var id = await Postular(estudiante, vacanteId, est.CurriculumId);

        await e.CambiarEstadoVacanteAsync(vacanteId, EstadoVacante.Pausada);

        var panel = await Json(await estudiante.GetAsync("/api/v1/estudiantes/me/postulaciones"));
        Assert.Single(panel.EnumerateArray());
        Assert.Equal(id, panel[0].GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await estudiante.GetAsync($"/api/v1/estudiantes/me/postulaciones/{id}")).StatusCode);

        // Retirar sigue permitido; reactivar no, porque la vacante ya no está disponible.
        Assert.Equal(HttpStatusCode.OK, (await estudiante.PostAsync($"/api/v1/estudiantes/me/postulaciones/{id}/retirar", null)).StatusCode);
        var reactivar = await estudiante.PostAsJsonAsync($"/api/v1/estudiantes/me/postulaciones/{id}/reactivar", new { curriculumId = est.CurriculumId });
        Assert.Equal("VACANTE_NO_DISPONIBLE", await Code(reactivar));
        Assert.Empty(Acciones((await Json(await estudiante.GetAsync($"/api/v1/estudiantes/me/postulaciones/{id}")))));
    }

    [Fact]
    public async Task Aceptar_una_oferta_retira_las_demas_postulaciones_activas()
    {
        await using var e = new EscenarioPostulaciones(api);
        var v1 = await e.CrearVacanteAsync();
        var v2 = await e.CrearVacanteAsync();
        var est = await e.CrearEstudianteAsync();
        var estudiante = await Cliente(est.Correo);
        var digital = await Cliente(Datos.ReclutadorDigital);
        var id1 = await Postular(estudiante, v1, est.CurriculumId);
        var id2 = await Postular(estudiante, v2, est.CurriculumId);
        await LlevarAOferta(digital, id1);

        Assert.Equal(HttpStatusCode.OK, (await estudiante.PostAsJsonAsync($"/api/v1/estudiantes/me/postulaciones/{id1}/respuesta-oferta", new { aceptar = true })).StatusCode);

        var otra = await Json(await estudiante.GetAsync($"/api/v1/estudiantes/me/postulaciones/{id2}"));
        Assert.Equal("Retirada", otra.GetProperty("estado").GetString());
        Assert.Empty(Acciones(otra));   // con una práctica aceptada ya no puede reactivar

        // Con una práctica aceptada tampoco entra a nuevas vacantes.
        var v3 = await e.CrearVacanteAsync();
        var nueva = await estudiante.PostAsJsonAsync($"/api/v1/vacantes/{v3}/postulaciones", new { curriculumId = est.CurriculumId });
        Assert.Equal("PRACTICA_YA_ACEPTADA", await Code(nueva));
    }

    [Fact]
    public async Task Responder_la_oferta_exige_decirlo_y_solo_aplica_a_ofertas()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync();
        var est = await e.CrearEstudianteAsync();
        var estudiante = await Cliente(est.Correo);
        var id = await Postular(estudiante, vacanteId, est.CurriculumId);

        // Cuerpo vacío: 400, nunca se interpreta como "declinar".
        var vacio = await estudiante.PostAsJsonAsync($"/api/v1/estudiantes/me/postulaciones/{id}/respuesta-oferta", new { });
        Assert.Equal(HttpStatusCode.BadRequest, vacio.StatusCode);
        Assert.Equal("Indique si acepta o declina la oferta.", (await Json(vacio)).GetProperty("errors").GetProperty("aceptar")[0].GetString());

        // Sin oferta todavía: transición inválida.
        var sinOferta = await estudiante.PostAsJsonAsync($"/api/v1/estudiantes/me/postulaciones/{id}/respuesta-oferta", new { aceptar = true });
        Assert.Equal("TRANSICION_INVALIDA", await Code(sinOferta));
    }

    // ---------- Cupos (HU-E07) ----------

    [Fact]
    public async Task La_oferta_necesita_un_cupo_libre_y_declinar_lo_libera()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync(cupos: 1);
        var a = await e.CrearEstudianteAsync();
        var b = await e.CrearEstudianteAsync();
        var clienteA = await Cliente(a.Correo);
        var digital = await Cliente(Datos.ReclutadorDigital);
        var idA = await Postular(clienteA, vacanteId, a.CurriculumId);
        var idB = await Postular(await Cliente(b.Correo), vacanteId, b.CurriculumId);

        Assert.Equal(HttpStatusCode.OK, (await LlevarAOferta(digital, idA)).StatusCode);

        var sinCupo = await LlevarAOferta(digital, idB);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, sinCupo.StatusCode);
        Assert.Equal("SIN_CUPOS", await Code(sinCupo));

        await clienteA.PostAsJsonAsync($"/api/v1/estudiantes/me/postulaciones/{idA}/respuesta-oferta", new { aceptar = false });   // declina: libera el cupo
        Assert.Equal(HttpStatusCode.OK, (await CambiarEstado(digital, idB, "Oferta")).StatusCode);   // ya está en EnRevision
    }

    [Fact]
    public async Task El_comentario_de_cambio_de_estado_admite_maximo_500_caracteres()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync();
        var est = await e.CrearEstudianteAsync();
        var id = await Postular(await Cliente(est.Correo), vacanteId, est.CurriculumId);
        var digital = await Cliente(Datos.ReclutadorDigital);

        var respuesta = await CambiarEstado(digital, id, "EnRevision", new string('x', 501));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("Máximo 500 caracteres.", (await Json(respuesta)).GetProperty("errors").GetProperty("comentario")[0].GetString());
    }

    // ---------- Filtros (HU-06, HU-E05) ----------

    [Fact]
    public async Task Los_listados_se_filtran_por_estado_y_por_cumple_requisitos()
    {
        await using var e = new EscenarioPostulaciones(api);
        var v1 = await e.CrearVacanteAsync(indiceMinimo: 95m);
        var v2 = await e.CrearVacanteAsync();
        var bajo = await e.CrearEstudianteAsync(indice: 80m);
        var alto = await e.CrearEstudianteAsync(indice: 99m);
        var clienteBajo = await Cliente(bajo.Correo);
        var digital = await Cliente(Datos.ReclutadorDigital);
        var idBajo = await Postular(clienteBajo, v1, bajo.CurriculumId);        // no cumple el índice, queda Enviada
        var idAlto = await Postular(await Cliente(alto.Correo), v1, alto.CurriculumId);
        var idBajo2 = await Postular(clienteBajo, v2, bajo.CurriculumId);
        Assert.Equal(HttpStatusCode.OK, (await CambiarEstado(digital, idAlto, "EnRevision")).StatusCode);

        // Empresa (HU-E05): por estado, por cumpleRequisitos y combinados.
        var baseUrl = $"/api/v1/empresa/vacantes/{v1}/postulaciones";
        Assert.Equal(new[] { idAlto, idBajo }.Order(), IdsDe(await Json(await digital.GetAsync(baseUrl)), "postulacionId"));
        Assert.Equal(new[] { idAlto }, IdsDe(await Json(await digital.GetAsync($"{baseUrl}?estado=EnRevision")), "postulacionId"));
        Assert.Equal(new[] { idAlto }, IdsDe(await Json(await digital.GetAsync($"{baseUrl}?cumpleRequisitos=true")), "postulacionId"));
        Assert.Equal(new[] { idBajo }, IdsDe(await Json(await digital.GetAsync($"{baseUrl}?cumpleRequisitos=false")), "postulacionId"));
        Assert.Equal(new[] { idBajo }, IdsDe(await Json(await digital.GetAsync($"{baseUrl}?estado=Enviada&cumpleRequisitos=false")), "postulacionId"));
        Assert.Empty((await Json(await digital.GetAsync($"{baseUrl}?estado=Rechazada"))).EnumerateArray());

        // Estudiante (HU-06): por estado.
        Assert.Equal(HttpStatusCode.OK, (await clienteBajo.PostAsync($"/api/v1/estudiantes/me/postulaciones/{idBajo2}/retirar", null)).StatusCode);
        Assert.Equal(new[] { idBajo2 }, IdsDe(await Json(await clienteBajo.GetAsync("/api/v1/estudiantes/me/postulaciones?estado=Retirada")), "id"));
        var enviadas = await Json(await clienteBajo.GetAsync("/api/v1/estudiantes/me/postulaciones?estado=Enviada"));
        Assert.Equal(new[] { idBajo }, IdsDe(enviadas, "id"));
        Assert.Equal(2, (await Json(await clienteBajo.GetAsync("/api/v1/estudiantes/me/postulaciones"))).GetArrayLength());

        // Un estado que no existe no es un filtro válido.
        Assert.Equal(HttpStatusCode.BadRequest, (await digital.GetAsync($"{baseUrl}?estado=Inventado")).StatusCode);
    }

    // ---------- Cupos y concurrencia (HU-E07) ----------

    [Fact]
    public async Task Dos_ofertas_simultaneas_no_superan_el_cupo()
    {
        await using var e = new EscenarioPostulaciones(api);
        var vacanteId = await e.CrearVacanteAsync(cupos: 1);
        var a = await e.CrearEstudianteAsync();
        var b = await e.CrearEstudianteAsync();
        var digital = await Cliente(Datos.ReclutadorDigital);
        var idA = await Postular(await Cliente(a.Correo), vacanteId, a.CurriculumId);
        var idB = await Postular(await Cliente(b.Correo), vacanteId, b.CurriculumId);
        Assert.Equal(HttpStatusCode.OK, (await CambiarEstado(digital, idA, "EnRevision")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await CambiarEstado(digital, idB, "EnRevision")).StatusCode);

        // Las dos ofertas salen a la vez: solo una puede quedarse con el único cupo.
        var respuestas = await Task.WhenAll(CambiarEstado(digital, idA, "Oferta"), CambiarEstado(digital, idB, "Oferta"));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        var rechazada = Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal("SIN_CUPOS", await Code(rechazada));
    }

    // ---------- Ayudas ----------

    private Task<HttpClient> Cliente(string correo) => api.ClienteAutenticadoAsync(correo, ApiFixture.PasswordDemo);

    private static async Task<JsonElement> Json(HttpResponseMessage respuesta) => await respuesta.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<string> Code(HttpResponseMessage respuesta) => (await Json(respuesta)).GetProperty("code").GetString()!;

    private static string[] Acciones(JsonElement postulacion) =>
        postulacion.GetProperty("acciones").EnumerateArray().Select(a => a.GetString()!).ToArray();

    private static async Task<Guid> Postular(HttpClient estudiante, Guid vacanteId, Guid curriculumId)
    {
        var respuesta = await estudiante.PostAsJsonAsync($"/api/v1/vacantes/{vacanteId}/postulaciones", new { curriculumId });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await Json(respuesta)).GetProperty("id").GetGuid();
    }

    /// <summary>La máquina de estados no permite Enviada → Oferta: se pasa por EnRevision. Devuelve la respuesta del último salto.</summary>
    private static async Task<HttpResponseMessage> LlevarAOferta(HttpClient empresa, Guid id)
    {
        var revision = await CambiarEstado(empresa, id, "EnRevision");
        Assert.Equal(HttpStatusCode.OK, revision.StatusCode);
        return await CambiarEstado(empresa, id, "Oferta");
    }

    private static Task<HttpResponseMessage> CambiarEstado(HttpClient empresa, Guid id, string nuevoEstado, string? comentario = null) =>
        empresa.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/empresa/postulaciones/{id}/estado")
        {
            Content = JsonContent.Create(new { nuevoEstado, comentario }),
        });

    private static Guid[] IdsDe(JsonElement lista, string propiedad) =>
        lista.EnumerateArray().Select(x => x.GetProperty(propiedad).GetGuid()).Order().ToArray();
}
