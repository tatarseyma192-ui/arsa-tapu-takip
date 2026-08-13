using ArsaTapu.Dto.Tasinmaz;
using FluentValidation;

namespace ArsaTapu.Business.Validators;

public class TasinmazUpdateDtoValidator : AbstractValidator<TasinmazUpdateDto>
{
    public TasinmazUpdateDtoValidator()
    {
        RuleFor(x => x.Nitelik).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Il).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ilce).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Mahalle).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Ada).GreaterThanOrEqualTo(0); // 0 = ada atanmamis (yol/tarla parselleri, gercek veriyle dogrulandi)
        RuleFor(x => x.Parsel).GreaterThan(0);
        RuleFor(x => x.ZeminHisseId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Yuzolcum).GreaterThan(0);
        RuleFor(x => x.BagimsizBolumNo).GreaterThan(0).When(x => x.BagimsizBolumNo.HasValue);
        RuleFor(x => x.Durum)
            .Must(d => d is "Aktif" or "Satildi")
            .WithMessage("Durum yalnızca 'Aktif' veya 'Satildi' olabilir.");
    }
}
