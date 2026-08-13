namespace ArsaTapu.Api.Authorization;

/// <summary>
/// Rol bazlı yetkilendirme policy isimleri. Controller'larda [Authorize(Policy=...)]
/// ile kullanılır; rol kontrolleri controller içine dağıtılmaz (AI Working Rules madde 2).
/// </summary>
public static class PolicyIsimleri
{
    /// <summary>Admin + Personel.</summary>
    public const string YonetimVePersonel = "YonetimVePersonel";

    /// <summary>Yalnızca Admin.</summary>
    public const string SadeceYonetim = "SadeceYonetim";
}
