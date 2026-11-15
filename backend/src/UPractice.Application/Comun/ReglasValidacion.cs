using FluentValidation;

namespace UPractice.Application.Comun;

/// <summary>
/// Reglas sintácticas compartidas (S5.7 y docs/mensajes-validacion.md). El mensaje debe ser idéntico
/// al del esquema Zod del frontend, carácter por carácter.
/// </summary>
public static class ReglasValidacion
{
    public const string MensajeCorreo = "Ingrese un correo válido.";
    public const string MensajePassword = "La contraseña debe tener 8 a 64 caracteres, con mayúscula, minúscula y número.";
    public const string MensajeConfirmar = "Las contraseñas no coinciden.";
    public const string MensajeNombreObligatorio = "Debe tener entre 2 y 50 letras.";
    public const string MensajeNombreOpcional = "Máximo 50 letras.";

    private const string SoloLetras = @"^[\p{L}][\p{L} '\-]*$";
    private const string SoloLetrasOVacio = @"^(\s*|[\p{L}][\p{L} '\-]*)$";

    public static IRuleBuilderOptions<T, string> CorreoValido<T>(this IRuleBuilder<T, string> regla) =>
        regla.NotEmpty().WithMessage(MensajeCorreo)
            .MaximumLength(150).WithMessage(MensajeCorreo)
            .EmailAddress().WithMessage(MensajeCorreo);

    public static IRuleBuilderOptions<T, string> PasswordSegura<T>(this IRuleBuilder<T, string> regla) =>
        regla.NotEmpty().WithMessage(MensajePassword)
            .Length(8, 64).WithMessage(MensajePassword)
            .Matches("[A-Z]").WithMessage(MensajePassword)
            .Matches("[a-z]").WithMessage(MensajePassword)
            .Matches("[0-9]").WithMessage(MensajePassword);

    /// <summary>Nombre1 y Apellido1: requeridos, 2 a 50, solo letras y espacios (D-23).</summary>
    public static IRuleBuilderOptions<T, string> NombreObligatorio<T>(this IRuleBuilder<T, string> regla) =>
        regla.NotEmpty().WithMessage(MensajeNombreObligatorio)
            .Length(2, 50).WithMessage(MensajeNombreObligatorio)
            .Matches(SoloLetras).WithMessage(MensajeNombreObligatorio);

    /// <summary>Nombre2 y Apellido2: opcionales, máximo 50, solo letras y espacios.</summary>
    public static IRuleBuilderOptions<T, string?> NombreOpcional<T>(this IRuleBuilder<T, string?> regla) =>
        regla.MaximumLength(50).WithMessage(MensajeNombreOpcional)
            .Matches(SoloLetrasOVacio).WithMessage(MensajeNombreOpcional); // null también es válido
}
