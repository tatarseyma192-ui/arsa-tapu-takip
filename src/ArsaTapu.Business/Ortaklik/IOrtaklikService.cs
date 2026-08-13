using ArsaTapu.Dto.Ortaklik;

namespace ArsaTapu.Business.Ortaklik;

public interface IOrtaklikService
{
    /// <summary>Gerçek ortaklık: aynı Bağımsız Bölüm No + aynı Zemin Hisse ID.</summary>
    Task<IReadOnlyList<GercekOrtaklikDto>> GercekOrtaklikGetirAsync(int[]? kisiIds, CancellationToken ct = default);

    /// <summary>Komşuluk: aynı Ada/Parsel, farklı Bağımsız Bölüm/Zemin Hisse ID. Ortaklık DEĞİLDİR.</summary>
    Task<IReadOnlyList<KomsulukDto>> KomsulukGetirAsync(int[]? kisiIds, CancellationToken ct = default);
}
