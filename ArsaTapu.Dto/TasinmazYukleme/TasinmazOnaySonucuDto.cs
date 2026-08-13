using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Dto.TasinmazYukleme;

/// <summary>
/// Karşılaştırma motorunun (Requirements madde 4.1 + madde 3) sonucu.
/// KmlSorgulanmasiGerekenParseller: KML çekme motoruna DOKUNULMADAN (Requirements madde 5,
/// izole modül) yalnızca hangi Ada/Parsel'lerin sorgulanması gerektiğinin tespiti.
/// </summary>
public class TasinmazOnaySonucuDto
{
    public int YuklemeKaydiId { get; set; }
    public int KisiId { get; set; }
    public DateTime YuklemeTarihi { get; set; }

    public int YeniAlimSayisi { get; set; }
    public int SatildiSayisi { get; set; }
    public int ZatenKayitliSayisi { get; set; }

    /// <summary>
    /// İstekteki TamPortfoyMu aynen yansıtılır — kullanıcı arayüzü, "Satıldı" sayısını hangi
    /// kapsamda hesapladığımızı (tam portföy mü, yalnızca belirli il/ilçe mi) gösterebilsin.
    /// </summary>
    public bool TamPortfoyMu { get; set; }

    /// <summary>
    /// TamPortfoyMu=false iken, "Satıldı" karşılaştırmasının SINIRLANDIĞI il/ilçe kombinasyonları
    /// (dosyada geçenler) — kullanıcı arayüzünde "bu yükleme şu il/ilçeler için değerlendirildi"
    /// diye gösterilebilir. TamPortfoyMu=true iken boş kalır (kapsam sınırı yok).
    /// </summary>
    public List<string> DegerlendirilenIlIlceler { get; set; } = new();

    public List<ParselAdayDto> KmlSorgulanmasiGerekenParseller { get; set; } = new();
}
