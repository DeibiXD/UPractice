using UPractice.Application.Abstracciones;
using UPractice.Domain.Enums;

namespace UPractice.Tests.Infraestructura;

/// <summary>ICurrentUser fijo para crear un AppDbContext "como si" fuera ese usuario.</summary>
public sealed record UsuarioDePrueba(Guid? UsuarioId = null, Rol? Rol = null, Guid? EmpresaId = null, Guid? UniversidadId = null) : ICurrentUser;
