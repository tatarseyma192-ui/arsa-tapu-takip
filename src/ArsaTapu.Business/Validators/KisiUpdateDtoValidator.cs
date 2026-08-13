using ArsaTapu.Dto.Kisi;
using FluentValidation;

namespace ArsaTapu.Business.Validators;

public class KisiUpdateDtoValidator : AbstractValidator<KisiUpdateDto>
{
    public KisiUpdateDtoValidator()
    {
        RuleFor(x => x.AdSoyad).NotEmpty().MaximumLength(200);
    }
}
