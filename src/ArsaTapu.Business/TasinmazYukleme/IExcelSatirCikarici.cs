namespace ArsaTapu.Business.TasinmazYukleme;

/// <summary>
/// Requirements madde 2.2: Doğrudan Excel yükleme, PDF ile aynı sütun şemasını bekler.
/// Şema uyuşmuyorsa (eksik/yanlış sütun) BusinessRuleException ile hangi sütunun eksik
/// olduğu net biçimde bildirilir (UI/UX Standards madde 6 — teknik detay değil).
/// </summary>
public interface IExcelSatirCikarici
{
    /// <summary>Ham satırları (kanonik sütun adı -> hücre metni) ve toplam satır sayısını döner.</summary>
    List<Dictionary<string, string?>> SatirlariCikar(Stream excelStream);
}
