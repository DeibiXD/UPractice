namespace UPractice.Application.Comun.Contratos;

public sealed record PaginaResultado<T>(
    IReadOnlyList<T> Items, int Pagina, int TamanoPagina, int TotalRegistros, int TotalPaginas);
