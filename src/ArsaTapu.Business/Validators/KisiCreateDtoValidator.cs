using ArsaTapu.Dto.Kisi;
using FluentValidation;

namespace ArsaTapu.Business.Validators;

public class KisiCreateDtoValidator : AbstractValidator<KisiCreateDto>
{
    public KisiCreateDtoValidator()
    {
        RuleFor(x => x.AdSoyad).NotEmpty().MaximumLength(200);
    }
}
