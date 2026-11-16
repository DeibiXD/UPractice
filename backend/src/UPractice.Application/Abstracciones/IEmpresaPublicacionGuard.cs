namespace UPractice.Application.Abstracciones;

/// <summary>Contrato de guardia usado por toda acción que publique una vacante.</summary>
public interface IEmpresaPublicacionGuard
{
    Task ValidarPuedePublicarAsync(CancellationToken ct);
}
