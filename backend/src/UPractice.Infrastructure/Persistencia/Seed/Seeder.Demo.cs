using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia.Seed;

/// <summary>
/// Datos de demostración (solo desarrollo, Docker y presentación). Todas las cuentas usan Seed:DemoPassword.
/// Se cargan una sola vez: si ya existe alguna universidad, no hace nada.
/// </summary>
internal sealed partial class Seeder
{
    private async Task SembrarDemoAsync(CancellationToken ct)
    {
        if (await db.Universidades.AnyAsync(ct)) return;

        var password = configuration["Seed:DemoPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No se cargaron datos de demostración: falta Seed:DemoPassword.");
            return;
        }
        var hash = hasher.Hashear(password);
        var habilidades = await db.Habilidades.ToDictionaryAsync(h => h.Nombre, ct);
        var ahora = DateTime.UtcNow;

        // ----- Universidades, facultades y carreras -----
        var unah = new Universidad { Nombre = "Universidad Nacional Autónoma de Honduras", Siglas = "UNAH", DominioCorreo = "unah.hn" };
        var unitec = new Universidad { Nombre = "Universidad Tecnológica Centroamericana", Siglas = "UNITEC", DominioCorreo = "unitec.edu" };

        var sistemasUnah = new Carrera { Nombre = "Ingeniería en Sistemas" };
        var industrialUnah = new Carrera { Nombre = "Ingeniería Industrial" };
        var contaduriaUnah = new Carrera { Nombre = "Contaduría Pública y Finanzas" };
        var administracionUnah = new Carrera { Nombre = "Administración de Empresas" };
        unah.Facultades.Add(new Facultad { Nombre = "Facultad de Ingeniería", Carreras = { sistemasUnah, industrialUnah } });
        unah.Facultades.Add(new Facultad { Nombre = "Facultad de Ciencias Económicas", Carreras = { contaduriaUnah, administracionUnah } });

        var sistemasUnitec = new Carrera { Nombre = "Ingeniería en Sistemas Computacionales" };
        var industrialUnitec = new Carrera { Nombre = "Ingeniería Industrial y de Sistemas" };
        var finanzasUnitec = new Carrera { Nombre = "Licenciatura en Finanzas" };
        unitec.Facultades.Add(new Facultad { Nombre = "Facultad de Ingeniería", Carreras = { sistemasUnitec, industrialUnitec } });
        unitec.Facultades.Add(new Facultad { Nombre = "Facultad de Ciencias Administrativas", Carreras = { finanzasUnitec } });
        db.Universidades.AddRange(unah, unitec);

        // ----- Coordinadores -----
        var coordUnah = Cuenta("coordinacion@unah.hn", "María", "José", "Rivera", "Cruz", Rol.Coordinador, universidad: unah);
        var coordUnitec = Cuenta("coordinacion@unitec.edu", "Carlos", null, "Mejía", "Paz", Rol.Coordinador, universidad: unitec);

        // ----- Empresas (tenants), aprobadas en ambas universidades -----
        var digital = new Empresa
        {
            NombreComercial = "Soluciones Digitales HN", RazonSocial = "Soluciones Digitales de Honduras S. de R.L.",
            Rtn = "08019020000011", Sector = "Tecnología", SitioWeb = "https://digitales.example.com",
        };
        var finanzas = new Empresa
        {
            NombreComercial = "Finanzas Centroamérica", RazonSocial = "Finanzas Centroamérica S.A.",
            Rtn = "05019020000022", Sector = "Servicios financieros",
        };
        db.Empresas.AddRange(digital, finanzas);
        foreach (var empresa in new[] { digital, finanzas })
        {
            db.HabilitacionesEmpresa.Add(Habilitacion(empresa, unah, coordUnah, ahora));
            db.HabilitacionesEmpresa.Add(Habilitacion(empresa, unitec, coordUnitec, ahora));
        }

        var adminDigital = Cuenta("admin@digitales.example.com", "Luis", "Fernando", "Castro", null, Rol.AdminEmpresa, empresa: digital);
        var reclutaDigital = Cuenta("rrhh@digitales.example.com", "Andrea", null, "Zelaya", "Flores", Rol.Reclutador, empresa: digital);
        var adminFinanzas = Cuenta("admin@finanzas.example.com", "Jorge", "Alberto", "Bonilla", null, Rol.AdminEmpresa, empresa: finanzas);
        _ = Cuenta("rrhh@finanzas.example.com", "Karla", "Patricia", "Reyes", "Mendoza", Rol.Reclutador, empresa: finanzas);

        // ----- Estudiantes -----
        var ana = Estudiante("ana.lopez@unah.hn", "Ana", "Sofía", "López", "Martínez", unah, sistemasUnah, "20191001234", 86.5m, coordUnah, ahora,
            habilidades["C#"], habilidades["SQL"], habilidades["React"], habilidades["Git"]);
        Estudiante("diego.ramos@unah.hn", "Diego", null, "Ramos", "Velásquez", unah, contaduriaUnah, "20181005678", 78m, coordUnah, ahora,
            habilidades["Contabilidad"], habilidades["Excel avanzado"]);
        Estudiante("sofia.hernandez@unitec.edu", "Sofía", "Isabel", "Hernández", null, unitec, sistemasUnitec, "11911034", 91.2m, coordUnitec, ahora,
            habilidades["Python"], habilidades["Análisis de datos"], habilidades["Power BI"]);
        // Perfil pendiente de verificación, para probar HU-C02.
        Estudiante("pedro.garcia@unah.hn", "Pedro", null, "García", "Sosa", unah, sistemasUnah, "20201009012", 72.3m, verificador: null, ahora);

        // ----- Vacantes -----
        var backend = Vacante(digital, reclutaDigital, "Practicante de desarrollo backend .NET",
            "Apoyarás al equipo de producto en el desarrollo de APIs con .NET y PostgreSQL, pruebas automatizadas y revisiones de código.",
            Modalidad.Hibrida, "Tegucigalpa", cupos: 2, remunerada: true, indiceMinimo: 75m, EstadoVacante.Publicada, ahora,
            [sistemasUnah, sistemasUnitec], (habilidades["C#"], true), (habilidades["SQL"], true), (habilidades["Docker"], false));
        Vacante(digital, reclutaDigital, "Practicante de desarrollo frontend React",
            "Construirás pantallas en React con TypeScript, consumirás APIs REST y participarás en la definición de la experiencia de usuario.",
            Modalidad.Remota, "Honduras (remoto)", cupos: 1, remunerada: true, indiceMinimo: null, EstadoVacante.Publicada, ahora,
            [sistemasUnah, sistemasUnitec], (habilidades["React"], true), (habilidades["TypeScript"], false));
        Vacante(digital, adminDigital, "Practicante de análisis de procesos",
            "Levantamiento y mejora de procesos internos con indicadores. Vacante en preparación (borrador).",
            Modalidad.Presencial, "Tegucigalpa", cupos: 1, remunerada: false, indiceMinimo: null, EstadoVacante.Borrador, ahora,
            [industrialUnah, industrialUnitec]);
        Vacante(finanzas, adminFinanzas, "Practicante de contabilidad y finanzas",
            "Apoyo en conciliaciones bancarias, registros contables y preparación de reportes financieros mensuales.",
            Modalidad.Presencial, "San Pedro Sula", cupos: 2, remunerada: true, indiceMinimo: 70m, EstadoVacante.Publicada, ahora,
            [contaduriaUnah, finanzasUnitec], (habilidades["Contabilidad"], true), (habilidades["Excel avanzado"], false));

        // ----- Una postulación con su historial (HU-05, HU-06, HU-E05) -----
        var cvAna = new Curriculum
        {
            Estudiante = ana, NombreVersion = "CV desarrollo", EsPrincipal = true,
        };
        cvAna.ArchivoKey = $"curriculums/{ana.Id}/{cvAna.Id}.pdf";
        db.Curriculums.Add(cvAna);

        var postulacion = new Postulacion
        {
            EmpresaId = digital.Id, Vacante = backend, Estudiante = ana, Curriculum = cvAna,
            Estado = EstadoPostulacion.EnRevision, CumpleRequisitos = true, FechaPostulacion = ahora.AddDays(-2),
        };
        postulacion.Historial.Add(new HistorialPostulacion
        {
            EmpresaId = digital.Id, EstadoNuevo = EstadoPostulacion.Enviada, CambiadoPor = ana, Fecha = ahora.AddDays(-2),
        });
        postulacion.Historial.Add(new HistorialPostulacion
        {
            EmpresaId = digital.Id, EstadoAnterior = EstadoPostulacion.Enviada, EstadoNuevo = EstadoPostulacion.EnRevision,
            CambiadoPor = reclutaDigital, Comentario = "Perfil interesante, se revisará el CV.", Fecha = ahora.AddDays(-1),
        });
        db.Postulaciones.Add(postulacion);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Datos de demostración cargados.");

        // ----- Helpers locales -----
        Usuario Cuenta(string correo, string nombre1, string? nombre2, string apellido1, string? apellido2, Rol rol,
            Universidad? universidad = null, Empresa? empresa = null)
        {
            var usuario = new Usuario
            {
                Correo = correo, PasswordHash = hash, Nombre1 = nombre1, Nombre2 = nombre2, Apellido1 = apellido1, Apellido2 = apellido2,
                Rol = rol, Universidad = universidad, Empresa = empresa,
            };
            db.Usuarios.Add(usuario);
            return usuario;
        }

        Usuario Estudiante(string correo, string nombre1, string? nombre2, string apellido1, string? apellido2, Universidad universidad,
            Carrera carrera, string numeroCuenta, decimal indice, Usuario? verificador, DateTime fecha, params Habilidad[] declaradas)
        {
            var usuario = Cuenta(correo, nombre1, nombre2, apellido1, apellido2, Rol.Estudiante, universidad: universidad);
            usuario.Perfil = new PerfilEstudiante
            {
                NumeroCuenta = numeroCuenta, Carrera = carrera, IndiceAcademico = indice, MostrarIndice = true,
                EstadoVerificacion = verificador is null ? EstadoVerificacion.Pendiente : EstadoVerificacion.Verificado,
                VerificadoPor = verificador, FechaVerificacion = verificador is null ? null : fecha,
            };
            foreach (var habilidad in declaradas)
                usuario.Habilidades.Add(new EstudianteHabilidad { Habilidad = habilidad });
            return usuario;
        }

        Vacante Vacante(Empresa empresa, Usuario autor, string titulo, string descripcion, Modalidad modalidad, string ubicacion,
            int cupos, bool remunerada, decimal? indiceMinimo, EstadoVacante estado, DateTime fecha, Carrera[] carreras,
            params (Habilidad Habilidad, bool EsRequerida)[] pedidas)
        {
            var vacante = new Vacante
            {
                Empresa = empresa, EmpresaId = empresa.Id, CreadaPor = autor, Titulo = titulo, Descripcion = descripcion,
                Modalidad = modalidad, Ubicacion = ubicacion, Cupos = cupos, Remunerada = remunerada, IndiceMinimo = indiceMinimo,
                Estado = estado, FechaCierre = DateOnly.FromDateTime(fecha.AddDays(45)),
                FechaPublicacion = estado == EstadoVacante.Publicada ? fecha.AddDays(-3) : null,
            };
            foreach (var carrera in carreras)
                vacante.Carreras.Add(new VacanteCarrera { Carrera = carrera });
            foreach (var (habilidad, esRequerida) in pedidas)
                vacante.Habilidades.Add(new VacanteHabilidad { Habilidad = habilidad, EsRequerida = esRequerida });
            db.Vacantes.Add(vacante);
            return vacante;
        }
    }

    private static HabilitacionEmpresa Habilitacion(Empresa empresa, Universidad universidad, Usuario coordinador, DateTime fecha) => new()
    {
        Empresa = empresa, Universidad = universidad, Estado = EstadoHabilitacion.Aprobada,
        ResueltaPor = coordinador, FechaSolicitud = fecha.AddDays(-7), FechaResolucion = fecha.AddDays(-6),
    };
}
