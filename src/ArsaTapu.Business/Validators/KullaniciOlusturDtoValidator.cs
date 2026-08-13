using ArsaTapu.Domain.Common;
using ArsaTapu.Dto.Auth;
using FluentValidation;

namespace ArsaTapu.Business.Validators;

public class KullaniciOlusturDtoValidator : AbstractValidator<KullaniciOlusturDto>
{
    public KullaniciOlusturDtoValidator()
    {
        RuleFor(x => x.Eposta).NotEmpty().EmailAddress().MaximumLength(256);

        // Not: ASP.NET Core Identity'nin kendi parola karmaşıklık kuralları (Program.cs'te
        // yapılandırılmış) zaten UserManager.CreateAsync sırasında ayrıca uygulanır — bu yalnızca
        // erken/anlaşılır bir geri bildirim için minimum uzunluk kontrolüdür.
        RuleFor(x => x.Sifre).NotEmpty().MinimumLength(8);

        RuleFor(x => x.AdSoyad).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Rol)
            .NotEmpty()
            .Must(rol => Roller.Tumu.Contains(rol))
            .WithMessage($"Rol şunlardan biri olmalı: {string.Join(", ", Roller.Tumu)}");

        RuleFor(x => x.KisiId)
            .NotNull()
            .WithMessage("Rol 'Patron' iken KisiId zorunludur — hesabın hangi kişiye ait olduğunu belirtir.")
            .When(x => x.Rol == Roller.Patron);
    }
}
