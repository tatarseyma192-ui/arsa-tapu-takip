using System.Globalization;
using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Business.TasinmazYukleme;

/// <summary>
/// PDF ve Excel çıkarıcılarının ürettiği ham (kanonik sütun adı -> metin) satırını
/// MulkiyetAdayDto'ya çevirir. Her iki kaynak da AYNI kanonik sözlük şeklini ürettiği için
/// bu dönüştürme mantığı TEK yerde toplanır (Handbook madde 7: aynı işlev tekrar yazılmaz).
/// Bir satır geçersizse (zorunlu alan boş / sayısal alan sayı değil) satır ATLANIR ve
/// kullanıcıya gösterilebilir bir hata metni üretilir — tüm parti tek satır yüzünden iptal olmaz.
/// </summary>
public static class SatirDonusturucu
{
    public static (MulkiyetAdayDto? Sonuc, string? Hata) Donustur(IReadOnlyDictionary<string, string?> hamSatir, int satirNo)
    {
        string? Deger(string anahtar) => hamSatir.TryGetValue(anahtar, out var v) ? v?.Trim() : null;

        // NOT: Taşınmaz No BİLEREK zorunlu değil — 2026-08-04'te sağlanan gerçek bir Excel
        // örneğinde bu sütun hiç yoktu. Mülkiyet tekilleştirme anahtarının (BagimsizBolumNo +
        // ZeminHisseId) parçası değildir, yalnızca varsa taşınır/gösterilir.
        var tasinmazNo = Deger(KanonikSutunlar.TasinmazNo);
        if (string.IsNullOrWhiteSpace(tasinmazNo)) tasinmazNo = null;

        var nitelik = Deger(KanonikSutunlar.Nitelik);
        if (string.IsNullOrWhiteSpace(nitelik))
            return (null, $"{satirNo}. satır: Nitelik boş olamaz.");

        var il = Deger(KanonikSutunlar.Il);
        if (string.IsNullOrWhiteSpace(il))
            return (null, $"{satirNo}. satır: İl boş olamaz.");

        var ilce = Deger(KanonikSutunlar.Ilce);
        if (string.IsNullOrWhiteSpace(ilce))
            return (null, $"{satirNo}. satır: İlçe boş olamaz.");

        var mahalle = Deger(KanonikSutunlar.Mahalle);
        if (string.IsNullOrWhiteSpace(mahalle))
            return (null, $"{satirNo}. satır: Mahalle boş olamaz.");

        var zeminHisseId = Deger(KanonikSutunlar.ZeminHisseId);
        if (string.IsNullOrWhiteSpace(zeminHisseId))
            return (null, $"{satirNo}. satır: Zemin Hisse ID boş olamaz.");

        var yuzolcumHam = Deger(KanonikSutunlar.Yuzolcum);
        if (!OndalikSayiyaCevir(yuzolcumHam, out var yuzolcum))
            return (null, $"{satirNo}. satır: Yüzölçüm sayısal bir değer değil ('{yuzolcumHam}').");

        var adaHam = Deger(KanonikSutunlar.Ada);
        if (!TamSayiyaCevir(adaHam, out var ada))
            return (null, $"{satirNo}. satır: Ada sayısal bir değer değil ('{adaHam}').");

        var parselHam = Deger(KanonikSutunlar.Parsel);
        if (!TamSayiyaCevir(parselHam, out var parsel))
            return (null, $"{satirNo}. satır: Parsel sayısal bir değer değil ('{parselHam}').");

        int? bagimsizBolumNo = null;
        var bbHam = Deger(KanonikSutunlar.BagimsizBolumNo);
        if (!BosDegerMi(bbHam))
        {
            if (!TamSayiyaCevir(bbHam, out var bbDeger))
                return (null, $"{satirNo}. satır: Bağımsız Bölüm No sayısal bir değer değil ('{bbHam}').");
            bagimsizBolumNo = bbDeger;
        }

        return (new MulkiyetAdayDto
        {
            TasinmazNo = tasinmazNo,
            Nitelik = nitelik,
            Il = il,
            Ilce = ilce,
            Mahalle = mahalle,
            Yuzolcum = yuzolcum,
            Ada = ada,
            Parsel = parsel,
            BagimsizBolumNo = bagimsizBolumNo,
            ZeminHisseId = zeminHisseId
        }, null);
    }

    /// <summary>
    /// Gerçek bir Excel örneğinde (2026-08-04'te doğrulandı) boş Bağımsız Bölüm No, boş hücre
    /// yerine LİTERAL "-" karakteriyle işaretleniyordu — bu, boşluk (whitespace) SAYILMAZ,
    /// bu yüzden ayrı bir kontrol gerekiyor (aksi halde "-" sayısal değere çevrilmeye
    /// çalışılıp hatalı biçimde reddedilirdi).
    /// </summary>
    private static bool BosDegerMi(string? deger) =>
        string.IsNullOrWhiteSpace(deger) || deger.Trim() is "-" or "--" or "yok" or "Yok" or "YOK";

    /// <summary>
    /// Hem Türkçe (binlik nokta, ondalık virgül: "1.234,56") hem İngilizce (binlik virgül,
    /// ondalık nokta: "1,234.56") biçimleri kabul eder. En sağdaki ayraç ondalık ayracı kabul edilir.
    /// </summary>
    private static bool OndalikSayiyaCevir(string? ham, out decimal sonuc)
    {
        sonuc = 0;
        if (string.IsNullOrWhiteSpace(ham)) return false;

        var metin = ham.Trim();
        var sonVirgul = metin.LastIndexOf(',');
        var sonNokta = metin.LastIndexOf('.');

        string normal;
        if (sonVirgul > sonNokta)
            normal = metin.Replace(".", "").Replace(',', '.');
        else if (sonNokta > sonVirgul)
            normal = metin.Replace(",", "");
        else
            normal = metin;

        return decimal.TryParse(normal, NumberStyles.Number, CultureInfo.InvariantCulture, out sonuc);
    }

    /// <summary>
    /// Gerçek bir Excel örneğinde (2026-08-04'te doğrulandı) Ada/Parsel/Yüzölçüm gibi sayısal
    /// hücreler bazen METİN ("12984.94") bazen DOĞRUDAN SAYI (14450) olarak geliyordu. Excel/PDF
    /// kaynaklı native sayı hücreleri bazen ".0" gibi gereksiz bir ondalık son ek taşıyabilir
    /// (ör. "182.0"). ESKİ yaklaşım (yalnızca rakamları koruyup birleştirmek) bu durumda
    /// "182.0" -> "1820" gibi TAMAMEN YANLIŞ bir sayı üretirdi — bu yüzden önce doğrudan tam
    /// sayı, olmazsa ondalık (kesirsiz olmak KAYDIYLA) olarak ayrıştırılır.
    /// </summary>
    private static bool TamSayiyaCevir(string? ham, out int sonuc)
    {
        sonuc = 0;
        if (string.IsNullOrWhiteSpace(ham)) return false;

        var metin = ham.Trim();

        if (int.TryParse(metin, NumberStyles.Integer, CultureInfo.InvariantCulture, out sonuc))
            return true;

        if (OndalikSayiyaCevir(metin, out var ondalik) && ondalik == Math.Truncate(ondalik))
        {
            sonuc = (int)ondalik;
            return true;
        }

        return false;
    }
}
