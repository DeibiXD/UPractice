using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UPractice.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreComercial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RTN = table.Column<string>(type: "character(14)", fixedLength: true, maxLength: 14, nullable: false),
                    Sector = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SitioWeb = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    MotivoSuspension = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                    table.CheckConstraint("CK_Empresas_RTN", "\"RTN\" ~ '^[0-9]{14}$'");
                    table.CheckConstraint("CK_Empresas_Suspension", "\"Activa\" OR \"MotivoSuspension\" IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "Habilidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habilidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Universidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Siglas = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    DominioCorreo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facultades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniversidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facultades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facultades_Universidades_UniversidadId",
                        column: x => x.UniversidadId,
                        principalTable: "Universidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Correo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Nombre1 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre2 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Apellido1 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Apellido2 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: true),
                    UniversidadId = table.Column<Guid>(type: "uuid", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    DebeCambiarPassword = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.CheckConstraint("CK_Usuarios_CorreoMinusculas", "\"Correo\" = lower(\"Correo\")");
                    table.CheckConstraint("CK_Usuarios_Pertenencia", "(\"Rol\" IN ('AdminEmpresa', 'Reclutador') AND \"EmpresaId\" IS NOT NULL AND \"UniversidadId\" IS NULL) OR (\"Rol\" IN ('Estudiante', 'Coordinador') AND \"UniversidadId\" IS NOT NULL AND \"EmpresaId\" IS NULL) OR (\"Rol\" = 'SuperAdmin' AND \"EmpresaId\" IS NULL AND \"UniversidadId\" IS NULL)");
                    table.CheckConstraint("CK_Usuarios_Rol", "\"Rol\" IN ('SuperAdmin', 'Coordinador', 'AdminEmpresa', 'Reclutador', 'Estudiante')");
                    table.ForeignKey(
                        name: "FK_Usuarios_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Usuarios_Universidades_UniversidadId",
                        column: x => x.UniversidadId,
                        principalTable: "Universidades",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Carreras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carreras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carreras_Facultades_FacultadId",
                        column: x => x.FacultadId,
                        principalTable: "Facultades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Curriculums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreVersion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ArchivoKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EsPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    Eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curriculums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Curriculums_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstudianteHabilidades",
                columns: table => new
                {
                    EstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    HabilidadId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstudianteHabilidades", x => new { x.EstudianteId, x.HabilidadId });
                    table.ForeignKey(
                        name: "FK_EstudianteHabilidades_Habilidades_HabilidadId",
                        column: x => x.HabilidadId,
                        principalTable: "Habilidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstudianteHabilidades_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HabilitacionesEmpresa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UniversidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MotivoRechazo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResueltaPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabilitacionesEmpresa", x => x.Id);
                    table.CheckConstraint("CK_HabilitacionesEmpresa_Estado", "\"Estado\" IN ('Pendiente', 'Aprobada', 'Rechazada')");
                    table.CheckConstraint("CK_HabilitacionesEmpresa_Motivo", "\"Estado\" <> 'Rechazada' OR \"MotivoRechazo\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_HabilitacionesEmpresa_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HabilitacionesEmpresa_Universidades_UniversidadId",
                        column: x => x.UniversidadId,
                        principalTable: "Universidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HabilitacionesEmpresa_Usuarios_ResueltaPorId",
                        column: x => x.ResueltaPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadaPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Modalidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Cupos = table.Column<int>(type: "integer", nullable: false),
                    Remunerada = table.Column<bool>(type: "boolean", nullable: false),
                    IndiceMinimo = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaCierre = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaPublicacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacantes", x => x.Id);
                    table.UniqueConstraint("AK_Vacantes_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.CheckConstraint("CK_Vacantes_Cupos", "\"Cupos\" BETWEEN 1 AND 50");
                    table.CheckConstraint("CK_Vacantes_Estado", "\"Estado\" IN ('Borrador', 'Publicada', 'Pausada', 'Cerrada')");
                    table.CheckConstraint("CK_Vacantes_IndiceMinimo", "\"IndiceMinimo\" IS NULL OR \"IndiceMinimo\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Vacantes_Modalidad", "\"Modalidad\" IN ('Presencial', 'Remota', 'Hibrida')");
                    table.ForeignKey(
                        name: "FK_Vacantes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacantes_Usuarios_CreadaPorId",
                        column: x => x.CreadaPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerfilesEstudiante",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroCuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CarreraId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndiceAcademico = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MostrarIndice = table.Column<bool>(type: "boolean", nullable: false),
                    EstadoVerificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ObservacionVerificacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VerificadoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaVerificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesEstudiante", x => x.UsuarioId);
                    table.CheckConstraint("CK_PerfilesEstudiante_Estado", "\"EstadoVerificacion\" IN ('Pendiente', 'Verificado', 'Rechazado')");
                    table.CheckConstraint("CK_PerfilesEstudiante_Indice", "\"IndiceAcademico\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_PerfilesEstudiante_Observacion", "\"EstadoVerificacion\" <> 'Rechazado' OR \"ObservacionVerificacion\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_PerfilesEstudiante_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerfilesEstudiante_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerfilesEstudiante_Usuarios_VerificadoPorId",
                        column: x => x.VerificadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Postulaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VacanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurriculumId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CumpleRequisitos = table.Column<bool>(type: "boolean", nullable: false),
                    FechaPostulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postulaciones", x => x.Id);
                    table.UniqueConstraint("AK_Postulaciones_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.CheckConstraint("CK_Postulaciones_Estado", "\"Estado\" IN ('Enviada', 'EnRevision', 'Entrevista', 'Oferta', 'Aceptada', 'Declinada', 'Rechazada', 'Retirada')");
                    table.ForeignKey(
                        name: "FK_Postulaciones_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curriculums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Postulaciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Postulaciones_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Postulaciones_Vacantes_VacanteId_EmpresaId",
                        columns: x => new { x.VacanteId, x.EmpresaId },
                        principalTable: "Vacantes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VacanteCarreras",
                columns: table => new
                {
                    VacanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarreraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacanteCarreras", x => new { x.VacanteId, x.CarreraId });
                    table.ForeignKey(
                        name: "FK_VacanteCarreras_Carreras_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carreras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VacanteCarreras_Vacantes_VacanteId",
                        column: x => x.VacanteId,
                        principalTable: "Vacantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacanteHabilidades",
                columns: table => new
                {
                    VacanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    HabilidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    EsRequerida = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacanteHabilidades", x => new { x.VacanteId, x.HabilidadId });
                    table.ForeignKey(
                        name: "FK_VacanteHabilidades_Habilidades_HabilidadId",
                        column: x => x.HabilidadId,
                        principalTable: "Habilidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VacanteHabilidades_Vacantes_VacanteId",
                        column: x => x.VacanteId,
                        principalTable: "Vacantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPostulacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostulacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CambiadoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPostulacion", x => x.Id);
                    table.CheckConstraint("CK_HistorialPostulacion_EstadoAnterior", "\"EstadoAnterior\" IS NULL OR \"EstadoAnterior\" IN ('Enviada', 'EnRevision', 'Entrevista', 'Oferta', 'Aceptada', 'Declinada', 'Rechazada', 'Retirada')");
                    table.CheckConstraint("CK_HistorialPostulacion_EstadoNuevo", "\"EstadoNuevo\" IN ('Enviada', 'EnRevision', 'Entrevista', 'Oferta', 'Aceptada', 'Declinada', 'Rechazada', 'Retirada')");
                    table.ForeignKey(
                        name: "FK_HistorialPostulacion_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialPostulacion_Postulaciones_PostulacionId_EmpresaId",
                        columns: x => new { x.PostulacionId, x.EmpresaId },
                        principalTable: "Postulaciones",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialPostulacion_Usuarios_CambiadoPorId",
                        column: x => x.CambiadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Practicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostulacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: true),
                    HorasCompletadas = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FueContratado = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practicas", x => x.Id);
                    table.UniqueConstraint("AK_Practicas_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.CheckConstraint("CK_Practicas_Estado", "\"Estado\" IN ('EnCurso', 'Finalizada', 'Abandonada')");
                    table.CheckConstraint("CK_Practicas_Fechas", "\"FechaFin\" IS NULL OR \"FechaFin\" >= \"FechaInicio\"");
                    table.CheckConstraint("CK_Practicas_Horas", "\"HorasCompletadas\" IS NULL OR \"HorasCompletadas\" >= 0");
                    table.ForeignKey(
                        name: "FK_Practicas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Practicas_Postulaciones_PostulacionId_EmpresaId",
                        columns: x => new { x.PostulacionId, x.EmpresaId },
                        principalTable: "Postulaciones",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosPractica",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PracticaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoDocumento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ArchivoKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SubidoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstadoRevision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RevisadoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ObservacionRevision = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosPractica", x => x.Id);
                    table.CheckConstraint("CK_DocumentosPractica_Estado", "\"EstadoRevision\" IN ('Pendiente', 'Aprobado', 'Rechazado')");
                    table.CheckConstraint("CK_DocumentosPractica_Tipo", "\"TipoDocumento\" IN ('CartaPresentacion', 'Convenio', 'ConstanciaHoras', 'CartaFinalizacion', 'Otro')");
                    table.ForeignKey(
                        name: "FK_DocumentosPractica_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosPractica_Practicas_PracticaId_EmpresaId",
                        columns: x => new { x.PracticaId, x.EmpresaId },
                        principalTable: "Practicas",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosPractica_Usuarios_RevisadoPorId",
                        column: x => x.RevisadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentosPractica_Usuarios_SubidoPorId",
                        column: x => x.SubidoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluacionesEmpresa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PracticaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Calificacion = table.Column<short>(type: "smallint", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionesEmpresa", x => x.Id);
                    table.CheckConstraint("CK_EvaluacionesEmpresa_Calificacion", "\"Calificacion\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_EvaluacionesEmpresa_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluacionesEmpresa_Practicas_PracticaId_EmpresaId",
                        columns: x => new { x.PracticaId, x.EmpresaId },
                        principalTable: "Practicas",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluacionesPracticante",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PracticaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Calificacion = table.Column<short>(type: "smallint", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionesPracticante", x => x.Id);
                    table.CheckConstraint("CK_EvaluacionesPracticante_Calificacion", "\"Calificacion\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_EvaluacionesPracticante_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluacionesPracticante_Practicas_PracticaId_EmpresaId",
                        columns: x => new { x.PracticaId, x.EmpresaId },
                        principalTable: "Practicas",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carreras_FacultadId_Nombre",
                table: "Carreras",
                columns: new[] { "FacultadId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_EstudianteId",
                table: "Curriculums",
                column: "EstudianteId",
                unique: true,
                filter: "\"EsPrincipal\" AND NOT \"Eliminado\"");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPractica_EmpresaId",
                table: "DocumentosPractica",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPractica_PracticaId_EmpresaId",
                table: "DocumentosPractica",
                columns: new[] { "PracticaId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPractica_RevisadoPorId",
                table: "DocumentosPractica",
                column: "RevisadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPractica_SubidoPorId",
                table: "DocumentosPractica",
                column: "SubidoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_RTN",
                table: "Empresas",
                column: "RTN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstudianteHabilidades_HabilidadId",
                table: "EstudianteHabilidades",
                column: "HabilidadId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesEmpresa_EmpresaId",
                table: "EvaluacionesEmpresa",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesEmpresa_PracticaId",
                table: "EvaluacionesEmpresa",
                column: "PracticaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesEmpresa_PracticaId_EmpresaId",
                table: "EvaluacionesEmpresa",
                columns: new[] { "PracticaId", "EmpresaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesPracticante_EmpresaId",
                table: "EvaluacionesPracticante",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesPracticante_PracticaId",
                table: "EvaluacionesPracticante",
                column: "PracticaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesPracticante_PracticaId_EmpresaId",
                table: "EvaluacionesPracticante",
                columns: new[] { "PracticaId", "EmpresaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facultades_UniversidadId_Nombre",
                table: "Facultades",
                columns: new[] { "UniversidadId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HabilitacionesEmpresa_EmpresaId_UniversidadId",
                table: "HabilitacionesEmpresa",
                columns: new[] { "EmpresaId", "UniversidadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HabilitacionesEmpresa_ResueltaPorId",
                table: "HabilitacionesEmpresa",
                column: "ResueltaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_HabilitacionesEmpresa_UniversidadId_Estado",
                table: "HabilitacionesEmpresa",
                columns: new[] { "UniversidadId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPostulacion_CambiadoPorId",
                table: "HistorialPostulacion",
                column: "CambiadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPostulacion_EmpresaId",
                table: "HistorialPostulacion",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPostulacion_PostulacionId_EmpresaId",
                table: "HistorialPostulacion",
                columns: new[] { "PostulacionId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPostulacion_PostulacionId_Fecha",
                table: "HistorialPostulacion",
                columns: new[] { "PostulacionId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesEstudiante_CarreraId",
                table: "PerfilesEstudiante",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesEstudiante_EstadoVerificacion",
                table: "PerfilesEstudiante",
                column: "EstadoVerificacion");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesEstudiante_VerificadoPorId",
                table: "PerfilesEstudiante",
                column: "VerificadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_CurriculumId",
                table: "Postulaciones",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_EmpresaId_Estado",
                table: "Postulaciones",
                columns: new[] { "EmpresaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_EstudianteId",
                table: "Postulaciones",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_VacanteId_EmpresaId",
                table: "Postulaciones",
                columns: new[] { "VacanteId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_VacanteId_EstudianteId",
                table: "Postulaciones",
                columns: new[] { "VacanteId", "EstudianteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Practicas_EmpresaId",
                table: "Practicas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Practicas_PostulacionId",
                table: "Practicas",
                column: "PostulacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Practicas_PostulacionId_EmpresaId",
                table: "Practicas",
                columns: new[] { "PostulacionId", "EmpresaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Universidades_DominioCorreo",
                table: "Universidades",
                column: "DominioCorreo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Universidades_Siglas",
                table: "Universidades",
                column: "Siglas",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmpresaId",
                table: "Usuarios",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UniversidadId",
                table: "Usuarios",
                column: "UniversidadId");

            migrationBuilder.CreateIndex(
                name: "IX_VacanteCarreras_CarreraId",
                table: "VacanteCarreras",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_VacanteHabilidades_HabilidadId",
                table: "VacanteHabilidades",
                column: "HabilidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_CreadaPorId",
                table: "Vacantes",
                column: "CreadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_EmpresaId_Estado",
                table: "Vacantes",
                columns: new[] { "EmpresaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Vacantes_Estado_FechaCierre",
                table: "Vacantes",
                columns: new[] { "Estado", "FechaCierre" });

            // Nombre de habilidad único sin distinguir mayúsculas (§3.5).
            migrationBuilder.Sql("CREATE UNIQUE INDEX \"UX_Habilidades_NombreLower\" ON \"Habilidades\" (lower(\"Nombre\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"UX_Habilidades_NombreLower\";");

            migrationBuilder.DropTable(
                name: "DocumentosPractica");

            migrationBuilder.DropTable(
                name: "EstudianteHabilidades");

            migrationBuilder.DropTable(
                name: "EvaluacionesEmpresa");

            migrationBuilder.DropTable(
                name: "EvaluacionesPracticante");

            migrationBuilder.DropTable(
                name: "HabilitacionesEmpresa");

            migrationBuilder.DropTable(
                name: "HistorialPostulacion");

            migrationBuilder.DropTable(
                name: "PerfilesEstudiante");

            migrationBuilder.DropTable(
                name: "VacanteCarreras");

            migrationBuilder.DropTable(
                name: "VacanteHabilidades");

            migrationBuilder.DropTable(
                name: "Practicas");

            migrationBuilder.DropTable(
                name: "Carreras");

            migrationBuilder.DropTable(
                name: "Habilidades");

            migrationBuilder.DropTable(
                name: "Postulaciones");

            migrationBuilder.DropTable(
                name: "Facultades");

            migrationBuilder.DropTable(
                name: "Curriculums");

            migrationBuilder.DropTable(
                name: "Vacantes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "Universidades");
        }
    }
}
