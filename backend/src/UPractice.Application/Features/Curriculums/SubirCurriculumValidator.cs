using FluentValidation;

namespace UPractice.Application.Features.Curriculums;

/// <summary>
/// HU-03 / S3.8: solo PDF (extensión .pdf, tipo application/pdf y que el contenido empiece con "%PDF") de hasta 5 MB.
/// Cualquier falla del archivo se reporta en el campo <c>archivo</c> con un único mensaje.
/// </summary>
public sealed class SubirCurriculumValidator : AbstractValidator<SubirCurriculumRequest>
{
    private static readonly byte[] FirmaPdf = "%PDF"u8.ToArray();

    public SubirCurriculumValidator()
    {
        RuleFor(x => x.NombreVersion)
            .Must(n => n is { } texto && texto.Trim().Length is >= 3 and <= 60)
            .WithMessage(MensajesCurriculums.NombreVersion);

        RuleFor(x => x.Archivo)
            .Must(EsPdfValido).WithMessage(MensajesCurriculums.Archivo);
    }

    private static bool EsPdfValido(ArchivoSubido? archivo)
    {
        if (archivo is null || archivo.Tamano <= 0 || archivo.Tamano > MensajesCurriculums.TamanoMaximo) return false;
        if (!archivo.NombreArchivo.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(archivo.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) return false;
        return EmpiezaConFirmaPdf(archivo.Contenido);
    }

    private static bool EmpiezaConFirmaPdf(Stream contenido)
    {
        if (!contenido.CanSeek) return false;
        var inicio = contenido.Position;
        Span<byte> cabecera = stackalloc byte[4];
        var leidos = contenido.ReadAtLeast(cabecera, 4, throwOnEndOfStream: false);
        contenido.Position = inicio;
        return leidos == 4 && cabecera.SequenceEqual(FirmaPdf);
    }
}
