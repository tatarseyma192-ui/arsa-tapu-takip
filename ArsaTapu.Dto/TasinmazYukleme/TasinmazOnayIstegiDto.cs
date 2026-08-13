using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Dto.TasinmazYukleme;

/// <summary>
/// Kullanıcının önizlemeyi kontrol edip onayladığı satırlar. Sunucu, gelen satırları
/// KÖRÜ KÖRÜNE kaydetmez — tekilleştirme sınıflandırması onay anında sunucu tarafında
/// YENİDEN hesaplanır (istemci ile sunucu arasında başka bir yükleme araya girmiş olabilir).
/// </summary>
public class TasinmazOnayIstegiDto
{
    public int KisiId { get; set; }
    public string KaynakDosyaAdi { get; set; } = null!;

    /// <summary>"Pdf" | "Excel"</summary>
    public string KaynakTuru { get; set; } = null!;

    public List<MulkiyetAdayDto> Satirlar { get; set; } = new();

    /// <summary>
    /// true: Bu dosya kişinin O ANDAKİ TÜM taşınmaz portföyünün eksiksiz bir dökümüdür —
    /// veritabanındaki TÜM aktif taşınmazları bu dosyayla karşılaştır, dosyada görünmeyen
    /// HERHANGİ BİR aktif taşınmaz "Satıldı" işaretlenir (Requirements madde 3, eski/varsayılan
    /// davranış).
    ///
    /// false (VARSAYILAN — kullanıcı isteği üzerine daha GÜVENLİ tarafta bırakıldı): Bu dosya
    /// yalnızca BELİRLİ bir il/ilçe için (ör. "sadece Gaziantep/Şahinbey kayıtları") — dosyada
    /// GEÇEN il/ilçe kombinasyonları dışındaki mevcut aktif taşınmazlara HİÇ DOKUNULMAZ (ne
    /// "Satıldı" ne "hâlâ görüldü" güncellenir). Yalnızca dosyada geçen il/ilçe(ler) kapsamında,
    /// o kapsamda olup dosyada görünmeyen taşınmazlar "Satıldı" işaretlenir.
    ///
    /// Yanlışlıkla TÜM portföyü "Satıldı" yapma riskini önlemek için varsayılan false'tur —
    /// yalnızca kullanıcı gerçekten kişinin TAM ve GÜNCEL portföyünü yüklediğini biliyorsa true
    /// gönderilmelidir.
    /// </summary>
    public bool TamPortfoyMu { get; set; } = false;
}
