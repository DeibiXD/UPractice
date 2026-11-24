using Microsoft.Extensions.DependencyInjection;
using UPractice.Application;
using UPractice.Application.Features.Empresa.Usuarios;
using UPractice.Domain.Enums;

namespace UPractice.Tests;

public sealed class UsuariosEmpresaValidatorTests
{
    private readonly CrearUsuarioEmpresaValidator validador;

    public UsuariosEmpresaValidatorTests()
    {
        new ServiceCollection().AddApplication();
        validador = new CrearUsuarioEmpresaValidator();
    }

    [Theory]
    [InlineData(Rol.Reclutador)]
    [InlineData(Rol.AdminEmpresa)]
    public async Task Acepta_roles_permitidos_nombres_compuestos_y_correo_con_espacios_exteriores(Rol rol)
    {
        var solicitud = Solicitud() with { Rol = rol };

        Assert.True((await validador.ValidateAsync(solicitud)).IsValid);
    }

    [Fact]
    public async Task Acepta_limites_sobre_nombres_recortados_y_opcionales_vacios()
    {
        var minimo = Solicitud() with { Nombre1 = "  An  ", Apellido1 = "  Li  ", Nombre2 = null, Apellido2 = "" };
        var maximo = Solicitud() with
        {
            Nombre1 = "  " + new string('A', 50) + "  ",
            Apellido1 = new string('B', 50),
            Nombre2 = new string('C', 50),
            Apellido2 = "   ",
            Correo = new string('a', 64) + "@" + new string('b', 60) + "." + new string('c', 20) + ".com",
        };

        Assert.True((await validador.ValidateAsync(minimo)).IsValid);
        Assert.True((await validador.ValidateAsync(maximo)).IsValid);
    }

    [Theory]
    [InlineData("nombreVacio", "nombre1", "Debe tener entre 2 y 50 letras.")]
    [InlineData("nombreNulo", "nombre1", "Debe tener entre 2 y 50 letras.")]
    [InlineData("nombreCorto", "nombre1", "Debe tener entre 2 y 50 letras.")]
    [InlineData("nombreLargo", "nombre1", "Debe tener entre 2 y 50 letras.")]
    [InlineData("nombreConNumero", "nombre1", "Debe tener entre 2 y 50 letras.")]
    [InlineData("apellidoVacio", "apellido1", "Debe tener entre 2 y 50 letras.")]
    [InlineData("apellidoConNumero", "apellido1", "Debe tener entre 2 y 50 letras.")]
    [InlineData("nombre2Largo", "nombre2", "Máximo 50 letras.")]
    [InlineData("apellido2Invalido", "apellido2", "Máximo 50 letras.")]
    [InlineData("correoVacio", "correo", "Ingrese un correo válido.")]
    [InlineData("correoSinArroba", "correo", "Ingrese un correo válido.")]
    [InlineData("correoLargo", "correo", "Ingrese un correo válido.")]
    public async Task Rechaza_campos_invalidos_con_los_mensajes_compartidos(string caso, string campo, string mensaje)
    {
        var solicitud = Solicitud();
        solicitud = caso switch
        {
            "nombreVacio" => solicitud with { Nombre1 = "   " },
            "nombreNulo" => solicitud with { Nombre1 = null! },
            "nombreCorto" => solicitud with { Nombre1 = " A " },
            "nombreLargo" => solicitud with { Nombre1 = new string('A', 51) },
            "nombreConNumero" => solicitud with { Nombre1 = "Ana1" },
            "apellidoVacio" => solicitud with { Apellido1 = "" },
            "apellidoConNumero" => solicitud with { Apellido1 = "Castro2" },
            "nombre2Largo" => solicitud with { Nombre2 = new string('A', 51) },
            "apellido2Invalido" => solicitud with { Apellido2 = "López2" },
            "correoVacio" => solicitud with { Correo = "  " },
            "correoSinArroba" => solicitud with { Correo = "correo-invalido" },
            "correoLargo" => solicitud with { Correo = new string('a', 138) + "@example.test" },
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };

        var resultado = await validador.ValidateAsync(solicitud);

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo && e.ErrorMessage == mensaje);
    }

    [Theory]
    [InlineData(Rol.Estudiante)]
    [InlineData(Rol.Coordinador)]
    [InlineData(Rol.SuperAdmin)]
    [InlineData((Rol)999)]
    public async Task Solo_se_pueden_solicitar_roles_de_empresa(Rol rol)
    {
        var resultado = await validador.ValidateAsync(Solicitud() with { Rol = rol });

        Assert.Contains(resultado.Errors, e => e.PropertyName == "rol" && e.ErrorMessage == "Seleccione un rol permitido.");
    }

    private static CrearUsuarioEmpresaRequest Solicitud() => new(
        "  Ana-María  ", "  Lucía  ", "  O'Connor  ", "  López  ",
        "  RECLUTADOR@EMPRESA.EXAMPLE.COM  ", Rol.Reclutador);
}
