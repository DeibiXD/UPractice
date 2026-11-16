using FluentValidation;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun;

namespace UPractice.Application.Features.Auth.Empresa;

public sealed class RegistroEmpresaValidator : AbstractValidator<RegistroEmpresaRequest>
{
    public RegistroEmpresaValidator(IEmpresaHabilitacionRepository repositorio)
    {
        RuleFor(x => x.NombreComercial).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RazonSocial).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Rtn)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Matches(@"^[0-9]{14}$")
            .WithMessage("El RTN debe contener exactamente 14 dígitos numéricos.");
        RuleFor(x => x.Sector).NotEmpty().MaximumLength(80);
        RuleFor(x => x.SitioWeb)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Ingrese una URL válida.")
            .MaximumLength(200);
        RuleFor(x => x.Admin).Cascade(CascadeMode.Stop)
            .NotNull()
            .SetValidator(new AdminEmpresaRegistroValidator());
        RuleFor(x => x.UniversidadesIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Seleccione al menos una universidad.")
            .NotEmpty().WithMessage("Seleccione al menos una universidad.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("Seleccione universidades válidas.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("No repita universidades.")
            .MustAsync(repositorio.SonUniversidadesActivasAsync)
            .WithMessage("Una o más universidades no están disponibles.");
    }
}

public sealed class AdminEmpresaRegistroValidator : AbstractValidator<AdminEmpresaRegistroRequest>
{
    public AdminEmpresaRegistroValidator()
    {
        RuleFor(x => x.Nombre1).NombreObligatorio();
        RuleFor(x => x.Nombre2).NombreOpcional();
        RuleFor(x => x.Apellido1).NombreObligatorio();
        RuleFor(x => x.Apellido2).NombreOpcional();
        RuleFor(x => x.Correo).CorreoValido();
        RuleFor(x => x.Password).PasswordSegura();
        RuleFor(x => x.ConfirmarPassword)
            .Equal(x => x.Password)
            .WithMessage(ReglasValidacion.MensajeConfirmar);
    }
}
