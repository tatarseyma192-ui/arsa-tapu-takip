namespace ArsaTapu.Dto.ParselKml;

/// <summary>
/// KML dosyasının description alanına yazılacak Taşınmaz No / Bağımsız Bölüm bilgisi
/// (Requirements madde 4.2 örneği: "Bağımsız Bölümler: 2, 4" / "Taşınmaz No: 13425953, 13423289").
/// </summary>
public class TasinmazReferansDto
{
    /// <summary>Opsiyonel — bazı kaynaklarda (ör. Excel) hiç bulunmayabilir, bkz. Tasinmaz.cs.</summary>
    public string? TasinmazNo { get; set; }
    public int? BagimsizBolumNo { get; set; }
}
