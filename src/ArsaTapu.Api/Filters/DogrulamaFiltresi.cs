using ArsaTapu.Dto.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArsaTapu.Api.Filters;

/// <summary>
/// Action parametreleri için kayıtlı FluentValidation validator'larını otomatik çalıştırır.
/// Hatalar Technical Defaults madde 4'teki standart errors[] formatında döner.
/// </summary>
public class DogrulamaFiltresi : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public DogrulamaFiltresi(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(arg);
            var sonuc = await validator.ValidateAsync(validationContext);

            if (!sonuc.IsValid)
            {
                var hatalar = sonuc.Errors
                    .Select(e => new FieldError { Field = e.PropertyName, Message = e.ErrorMessage })
                    .ToList();

                context.Result = new BadRequestObjectResult(ApiResponse.Fail("Girilen veriler geçersiz.", hatalar));
                return;
            }
        }

        await next();
    }
}
