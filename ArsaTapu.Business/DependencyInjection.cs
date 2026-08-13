using ArsaTapu.Business.Common;
using ArsaTapu.Business.Kisi;
using ArsaTapu.Business.Ortaklik;
using ArsaTapu.Business.ParselSorgu;
using ArsaTapu.Business.Tasinmaz;
using ArsaTapu.Business.TasinmazYukleme;
using ArsaTapu.Business.Tekillestirme;
using ArsaTapu.Business.Validators;
using ArsaTapu.DataAccess.Repositories;
using ArsaTapu.DataAccess.UnitOfWork;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ArsaTapu.Business;

/// <summary>Business + DataAccess katmanlarının tüm DI kayıtlarını tek yerde toplar (Handbook madde 7).</summary>
public static class DependencyInjection
{
    public static IServiceCollection BusinessServisleriniEkle(this IServiceCollection services)
    {
        // DataAccess
        services.AddScoped<IKisiRepository, KisiRepository>();
        services.AddScoped<ITasinmazRepository, TasinmazRepository>();
        services.AddScoped<IParselKmlRepository, ParselKmlRepository>();
        services.AddScoped<IYuklemeKaydiRepository, YuklemeKaydiRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Business
        services.AddScoped<IYetkiKapsamService, YetkiKapsamService>();
        services.AddScoped<IKisiService, KisiService>();
        services.AddScoped<ITasinmazService, TasinmazService>();
        services.AddScoped<IOrtaklikService, OrtaklikService>();
        services.AddScoped<IMulkiyetTekillestirmeService, MulkiyetTekillestirmeService>();
        services.AddScoped<IKmlTekillestirmeService, KmlTekillestirmeService>();

        // TasinmazYukleme (PDF/Excel parse + karşılaştırma motoru) — izole modül (Requirements madde 5)
        services.AddScoped<IDosyaDogrulamaService, DosyaDogrulamaService>();
        services.AddScoped<IPdfSatirCikarici, PdfSatirCikarici>();
        services.AddScoped<IExcelSatirCikarici, ExcelSatirCikarici>();
        services.AddScoped<IExcelUreticiService, ExcelUreticiService>();
        services.AddScoped<ITasinmazYuklemeService, TasinmazYuklemeService>();

        // ParselSorgu (TKGM otomasyonu) — izole modül (Handbook madde 4). IParselSorguIstemcisi
        // AddHttpClient ile Api katmanında (Program.cs) kaydedilir, burada DEĞİL.
        services.AddSingleton<IParselSorguHizSinirlayici, ParselSorguHizSinirlayici>();
        services.AddSingleton<ITkgmIdCache, TkgmIdCache>();
        services.AddSingleton<IKmlDosyaDepoService, KmlDosyaDepoService>();
        services.AddScoped<IParselKmlService, ParselKmlService>();

        // Validasyon
        services.AddValidatorsFromAssemblyContaining<KisiCreateDtoValidator>();

        return services;
    }
}
