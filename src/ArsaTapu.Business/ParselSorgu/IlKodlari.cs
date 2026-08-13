namespace ArsaTapu.Business.ParselSorgu;

/// <summary>
/// TKGM'nin il adı -> iç veritabanı Id eşleştirmesi. Kaynak: parselsorgu.tkgm.gov.tr'nin kendi
/// statik `app/modules/administrativeQuery/data/ilListe.json` dosyası (gerçek network trafiğinden
/// — HAR kaydından — çıkarıldı, 2026-08-04). Bu Id'ler TKGM'nin resmi plaka kodları DEĞİLDİR,
/// kendi iç veritabanı anahtarlarıdır.
///
/// Türkiye'nin 81 ili 1999'dan (Düzce) beri sabittir; bu yüzden burada SABİT (hardcoded) tutmak,
/// her istekte ekstra bir ağ çağrısı yapıp TKGM'nin ön yüz statik dosyasına bağımlı kalmaktan
/// (o dosyanın yolu/yapısı değişebilir — bu bir "gerçek API" değil, bir ön yüz varlığıdır) daha
/// sağlam bir tercihtir. İlçe/mahalle listeleri (binlerce kayıt, daha az stabil) SABİTLENMEZ;
/// bunlar için gerçek cbsapi uç noktaları çağrılır ve ITkgmIdCache ile önbelleğe alınır.
/// </summary>
public static class IlKodlari
{
    private static readonly IReadOnlyDictionary<string, int> Tablo = new Dictionary<string, int>
    {
        ["Adana"] = 23, ["Adıyaman"] = 24, ["Afyonkarahisar"] = 25, ["Ağrı"] = 26, ["Amasya"] = 27,
        ["Ankara"] = 28, ["Antalya"] = 29, ["Artvin"] = 30, ["Aydın"] = 31, ["Balıkesir"] = 32,
        ["Bilecik"] = 33, ["Bingöl"] = 34, ["Bitlis"] = 35, ["Bolu"] = 36, ["Burdur"] = 37,
        ["Bursa"] = 38, ["Çanakkale"] = 39, ["Çankırı"] = 40, ["Çorum"] = 41, ["Denizli"] = 42,
        ["Diyarbakır"] = 43, ["Edirne"] = 44, ["Elazığ"] = 45, ["Erzincan"] = 46, ["Erzurum"] = 47,
        ["Eskişehir"] = 48, ["Gaziantep"] = 49, ["Giresun"] = 50, ["Gümüşhane"] = 51, ["Hakkari"] = 52,
        ["Hatay"] = 53, ["Isparta"] = 54, ["Mersin"] = 55, ["İstanbul"] = 56, ["İzmir"] = 57,
        ["Kars"] = 58, ["Kastamonu"] = 59, ["Kayseri"] = 60, ["Kırklareli"] = 61, ["Kırşehir"] = 62,
        ["Kocaeli"] = 63, ["Konya"] = 64, ["Kütahya"] = 65, ["Malatya"] = 66, ["Manisa"] = 67,
        ["Kahramanmaraş"] = 68, ["Mardin"] = 69, ["Muğla"] = 70, ["Muş"] = 71, ["Nevşehir"] = 72,
        ["Niğde"] = 73, ["Ordu"] = 74, ["Rize"] = 75, ["Sakarya"] = 76, ["Samsun"] = 77,
        ["Siirt"] = 78, ["Sinop"] = 79, ["Sivas"] = 80, ["Tekirdağ"] = 81, ["Tokat"] = 82,
        ["Trabzon"] = 83, ["Tunceli"] = 84, ["Şanlıurfa"] = 85, ["Uşak"] = 86, ["Van"] = 87,
        ["Yozgat"] = 88, ["Zonguldak"] = 89, ["Aksaray"] = 90, ["Bayburt"] = 91, ["Karaman"] = 92,
        ["Kırıkkale"] = 93, ["Batman"] = 94, ["Şırnak"] = 95, ["Bartın"] = 96, ["Ardahan"] = 97,
        ["Iğdır"] = 98, ["Yalova"] = 99, ["Karabük"] = 100, ["Kilis"] = 101, ["Osmaniye"] = 102,
        ["Düzce"] = 103
    };

    private static readonly IReadOnlyDictionary<string, int> NormalizeEdilmisTablo =
        Tablo.ToDictionary(kv => Normallestir(kv.Key), kv => kv.Value);

    /// <summary>Türkçe karakter/case farklarına toleranslı arama. Bulunamazsa null döner.</summary>
    public static int? Bul(string ilAdi)
    {
        var anahtar = Normallestir(ilAdi);
        if (NormalizeEdilmisTablo.TryGetValue(anahtar, out var id)) return id;

        // Yedek: PDF'ten gelen olası fazladan boşlukları (satır kaydırması artığı) da tolere et.
        var bosluksuzAnahtar = anahtar.Replace(" ", "");
        foreach (var (kanonikAd, tabloId) in NormalizeEdilmisTablo)
        {
            if (kanonikAd.Replace(" ", "") == bosluksuzAnahtar) return tabloId;
        }

        return null;
    }

    /// <summary>
    /// Il/Ilce/Mahalle adı eşleştirmede kullanılan normalize etme mantığı. TKGM'nin kendi
    /// verisinde bile tutarsızlıklar var (ör. "Ağaçli" vs beklenen "Ağaçlı") — bu yüzden ı/i
    /// ayrımı da (Türkçe büyük/küçük harf tuzağı dahil) burada bilerek yok sayılır.
    /// </summary>
    internal static string Normallestir(string metin) =>
        metin.Trim()
            .Replace('İ', 'i').Replace('I', 'i').Replace('ı', 'i')
            .Replace('Ç', 'c').Replace('ç', 'c')
            .Replace('Ğ', 'g').Replace('ğ', 'g')
            .Replace('Ö', 'o').Replace('ö', 'o')
            .Replace('Ş', 's').Replace('ş', 's')
            .Replace('Ü', 'u').Replace('ü', 'u')
            .ToLowerInvariant();
}
