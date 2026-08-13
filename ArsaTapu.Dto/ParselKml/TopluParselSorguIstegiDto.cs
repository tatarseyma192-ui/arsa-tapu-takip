namespace ArsaTapu.Dto.ParselKml;

public class TopluParselSorguIstegiDto
{
    /// <summary>
    /// Elle seçilmiş, açık parsel listesi. TumunuSecModu=false iken (VARSAYILAN) bu liste
    /// AYNEN kullanılır — kullanıcı hangi parselleri istiyorsa yalnızca onlar sorgulanır.
    /// TumunuSecModu=true iken bu alan YOK SAYILIR (KisiId'ye göre sunucu tarafında hesaplanır).
    /// </summary>
    public List<ParselSorguIstegiDto> Parseller { get; set; } = new();

    /// <summary>
    /// true: KisiId'nin TÜM taşınmazlarına ait, henüz KML'si başarıyla çekilmemiş parseller
    /// otomatik olarak bulunup sorgulanır (kullanıcı isteği: "hepsi | belirli tapular gibi
    /// bir opsiyon olsun"). false (VARSAYILAN): yalnızca Parseller listesindeki açık seçim
    /// sorgulanır.
    /// </summary>
    public bool TumunuSecModu { get; set; } = false;

    /// <summary>TumunuSecModu=true iken ZORUNLUDUR — hangi kişinin parselleri taranacağını belirtir.</summary>
    public int? KisiId { get; set; }
}
