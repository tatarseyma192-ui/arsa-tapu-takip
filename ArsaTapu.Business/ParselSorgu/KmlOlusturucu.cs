using System.Globalization;
using System.Xml.Linq;
using ArsaTapu.Dto.ParselKml;

namespace ArsaTapu.Business.ParselSorgu;

/// <summary>
/// Requirements madde 4.2: KML açıklama/description alanına bağlı Taşınmaz No / Bağımsız Bölüm
/// bilgilerini yazar.
///
/// TKGM'den indirilen KML dosyasının TAM iç yapısı bu ortamdan doğrulanamadı (HAR kaydında
/// dosya içeriği boş yakalanmış — tarayıcı "indirme" yanıtlarını bazen ağ sekmesine kaydetmez).
/// Bu yüzden enjeksiyon SAVUNMACI/en-iyi-çaba şeklindedir: KML ayrıştırılamazsa veya hiç
/// Placemark bulunamazsa, orijinal bayt dizisi DEĞİŞTİRİLMEDEN döner — asıl geometri/dosya
/// hiçbir durumda bozulmaz veya kaybolmaz, yalnızca açıklama eklenemez.
/// </summary>
public static class KmlOlusturucu
{
    public static byte[] AciklamaEnjekteEt(
        byte[] orijinalKmlBaytlari, IReadOnlyList<TasinmazReferansDto> tasinmazReferanslari)
    {
        if (tasinmazReferanslari.Count == 0) return orijinalKmlBaytlari;

        XDocument belge;
        try
        {
            using var girisAkisi = new MemoryStream(orijinalKmlBaytlari);
            belge = XDocument.Load(girisAkisi);
        }
        catch
        {
            // Ayrıştırılamadı (beklenmeyen/bozuk format) — orijinali koru, hata fırlatma.
            return orijinalKmlBaytlari;
        }

        var ad = belge.Root?.Name.Namespace ?? XNamespace.None;
        var placemarklar = belge.Descendants(ad + "Placemark").ToList();
        if (placemarklar.Count == 0) return orijinalKmlBaytlari;

        var ekAciklama = AciklamaMetniOlustur(tasinmazReferanslari);
        if (string.IsNullOrWhiteSpace(ekAciklama)) return orijinalKmlBaytlari;

        foreach (var placemark in placemarklar)
        {
            var mevcutAciklama = placemark.Element(ad + "description");
            if (mevcutAciklama is not null)
            {
                mevcutAciklama.Value = string.IsNullOrWhiteSpace(mevcutAciklama.Value)
                    ? ekAciklama
                    : $"{mevcutAciklama.Value}\n---\n{ekAciklama}";
            }
            else
            {
                var yeniAciklama = new XElement(ad + "description", ekAciklama);
                var adElemani = placemark.Element(ad + "name");
                if (adElemani is not null)
                    adElemani.AddAfterSelf(yeniAciklama);
                else
                    placemark.AddFirst(yeniAciklama);
            }
        }

        using var cikisAkisi = new MemoryStream();
        belge.Save(cikisAkisi);
        return cikisAkisi.ToArray();
    }

    private static string AciklamaMetniOlustur(IReadOnlyList<TasinmazReferansDto> referanslar)
    {
        var satirlar = new List<string>();

        var bagimsizBolumler = referanslar
            .Where(t => t.BagimsizBolumNo.HasValue)
            .Select(t => t.BagimsizBolumNo!.Value.ToString(CultureInfo.InvariantCulture))
            .Distinct()
            .ToList();
        if (bagimsizBolumler.Count > 0)
            satirlar.Add($"Bağımsız Bölümler: {string.Join(", ", bagimsizBolumler)}");

        var tasinmazNolari = referanslar
            .Select(t => t.TasinmazNo)
            .Where(no => !string.IsNullOrWhiteSpace(no))
            .Distinct()
            .ToList();
        if (tasinmazNolari.Count > 0)
            satirlar.Add($"Taşınmaz No: {string.Join(", ", tasinmazNolari)}");

        return string.Join("\n", satirlar);
    }
}
