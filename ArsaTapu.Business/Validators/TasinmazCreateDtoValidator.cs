using ArsaTapu.Dto.Tasinmaz;
using FluentValidation;

namespace ArsaTapu.Business.Validators;

public class TasinmazCreateDtoValidator : AbstractValidator<TasinmazCreateDto>
{
    public TasinmazCreateDtoValidator()
    {
        RuleFor(x => x.KisiId).GreaterThan(0);
        RuleFor(x => x.TasinmazNo).MaximumLength(50).When(x => x.TasinmazNo is not null);
        RuleFor(x => x.Nitelik).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Il).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ilce).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Mahalle).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Ada).GreaterThanOrEqualTo(0); // 0 = ada atanmamis (yol/tarla parselleri, gercek veriyle dogrulandi)
        RuleFor(x => x.Parsel).GreaterThan(0);
        RuleFor(x => x.ZeminHisseId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Yuzolcum).GreaterThan(0);
        RuleFor(x => x.BagimsizBolumNo).GreaterThan(0).When(x => x.BagimsizBolumNo.HasValue);
    }
}
