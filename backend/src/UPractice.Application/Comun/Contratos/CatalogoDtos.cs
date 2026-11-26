namespace UPractice.Application.Comun.Contratos;

/// <summary>Habilidad del catálogo (S5.6 <c>HabilidadDto</c>).</summary>
public sealed record HabilidadDto(Guid Id, string Nombre);

/// <summary>URL firmada temporal para descargar un PDF (S3.8, S5.6 <c>UrlDescargaDto</c>).</summary>
public sealed record UrlDescargaDto(string Url, DateTime ExpiraEn);
