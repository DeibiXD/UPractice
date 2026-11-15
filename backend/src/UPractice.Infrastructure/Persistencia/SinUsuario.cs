using UPractice.Application.Abstracciones;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia;

/// <summary>Usuario vacío para herramientas de diseño (dotnet ef): con él los filtros globales no devuelven filas.</summary>
internal sealed class SinUsuario : ICurrentUser
{
    public Guid? UsuarioId => null;
    public Rol? Rol => null;
    public Guid? EmpresaId => null;
    public Guid? UniversidadId => null;
}
