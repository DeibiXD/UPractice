using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Tests.Infraestructura;

/// <summary>
/// SuperAdmin propio de las pruebas de HU-A03/HU-A04 (sin contraseña temporal), para no depender del SuperAdmin del seed
/// cuya contraseña cambian otras pruebas. Borra al terminar el SuperAdmin y los coordinadores creados.
/// </summary>
public sealed class EscenarioAdmin(ApiFixture api) : IAsyncDisposable
{
    private readonly List<Guid> usuarios = [];

    public async Task<HttpClient> ClienteSuperAdminAsync()
    {
        var admin = new Usuario
        {
            Correo = $"superadmin.{Guid.NewGuid():N}@upractice.test",
            PasswordHash = api.Servicio<IHasherPassword>().Hashear(ApiFixture.PasswordDemo),
            Nombre1 = "Super",
            Apellido1 = "Admin",
            Rol = Rol.SuperAdmin,
        };
        await using (var db = api.CrearContextoSinUsuario())
        {
            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();
        }
        usuarios.Add(admin.Id);
        return await api.ClienteAutenticadoAsync(admin.Correo, ApiFixture.PasswordDemo);
    }

    /// <summary>Registra un usuario creado por la API para borrarlo al final.</summary>
    public void Registrar(Guid usuarioId) => usuarios.Add(usuarioId);

    public async Task<Usuario> UsuarioAsync(Guid id)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == id);
    }

    public async Task<Guid> UniversidadAsync(string dominio)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Universidades.AsNoTracking().Where(u => u.DominioCorreo == dominio).Select(u => u.Id).SingleAsync();
    }

    /// <summary>La "Universidad de Prueba Inactiva" del seed base.</summary>
    public async Task<Guid> UniversidadInactivaAsync()
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Universidades.AsNoTracking().Where(u => !u.Activa).Select(u => u.Id).FirstAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = api.CrearContextoSinUsuario();
        await db.Usuarios.Where(u => usuarios.Contains(u.Id)).ExecuteDeleteAsync();
    }
}
