namespace UPractice.Application.Abstracciones;

/// <summary>Fecha de negocio en Honduras e instante UTC, sustituibles en las pruebas.</summary>
public interface IFechaHonduras
{
    DateOnly Hoy();
    DateTime UtcAhora();
}
