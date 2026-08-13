namespace ArsaTapu.Business.TasinmazYukleme;

/// <summary>
/// Requirements madde 2'deki PDF/Excel sütun şemasının TEK referans noktası.
/// PDF ve Excel çıkarıcıları (IPdfSatirCikarici / IExcelSatirCikarici) bu sütun adlarını
/// anahtar olarak kullanan bir Dictionary üretir; SatirDonusturucu bunu MulkiyetAdayDto'ya çevirir.
/// PDF yapısı değişirse yalnızca çıkarıcı implementasyonları güncellenir, bu sınıf ve
/// SatirDonusturucu değişmez (Requirements madde 5: izole modül).
/// </summary>
public static class KanonikSutunlar
{
    public const string TasinmazNo = "TasinmazNo";
    public const string Nitelik = "Nitelik";
    public const string Il = "Il";
    public const string Ilce = "Ilce";
    public const string Mahalle = "Mahalle";
    public const string Yuzolcum = "Yuzolcum";
    public const string Ada = "Ada";
    public const string Parsel = "Parsel";
    public const string BagimsizBolumNo = "BagimsizBolumNo";
    public const string ZeminHisseId = "ZeminHisseId";

    /// <summary>Requirements madde 2'deki PDF sütun sırasıyla birebir aynı.</summary>
    public static readonly string[] Tumu =
    {
        TasinmazNo, Nitelik, Il, Ilce, Mahalle, Yuzolcum, Ada, Parsel, BagimsizBolumNo, ZeminHisseId
    };

    /// <summary>
    /// Zorunlu sütunlar — BagimsizBolumNo hariç (Requirements madde 2: "boş olabilir") VE
    /// TasinmazNo hariç (2026-08-04'te sağlanan gerçek bir Excel örneğinde bu sütun hiç yoktu;
    /// TasinmazNo mülkiyet tekilleştirme anahtarının parçası değildir, yalnızca varsa gösterilir).
    /// Yalnızca ExcelSatirCikarici tarafından "bu sütun başlıkta var mı" kontrolünde kullanılır —
    /// PDF tarafı (PdfSatirCikarici) TÜM KanonikSutunlar.Tumu sütunlarını arar, çünkü WebTapu
    /// PDF'i TasinmazNo'yu her zaman içerir (bu iki kaynağın şeması KASITLI olarak farklıdır).
    /// </summary>
    public static readonly string[] Zorunlu =
    {
        Nitelik, Il, Ilce, Mahalle, Yuzolcum, Ada, Parsel, ZeminHisseId
    };

    public static readonly IReadOnlyDictionary<string, string> GoruntulemeAdlari = new Dictionary<string, string>
    {
        [TasinmazNo] = "Taşınmaz No",
        [Nitelik] = "Nitelik",
        [Il] = "İl",
        [Ilce] = "İlçe",
        [Mahalle] = "Mahalle",
        [Yuzolcum] = "Yüzölçüm",
        [Ada] = "Ada",
        [Parsel] = "Parsel",
        [BagimsizBolumNo] = "Bağımsız Bölüm No",
        [ZeminHisseId] = "Zemin Hisse ID"
    };

    /// <summary>
    /// Bir satırın TAMAMININ (ör. PDF'te bir satırdaki tüm kelimelerin birleşimi) başlık satırı
    /// olup olmadığını puanlamak için kullanılan gevşek anahtar kelime kümeleri (substring eşleşmesi
    /// yeterli — bu yalnızca "hangi satır başlık satırı" tespiti için kullanılır, sütun ATAMASI için
    /// DEĞİL; sütun ataması EslesenSutunAdi ile TAM eşleşme üzerinden yapılır).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> BaslikAnahtarKelimeleri = new Dictionary<string, string[]>
    {
        [TasinmazNo] = new[] { "tasinmaz", "no" },
        [Nitelik] = new[] { "nitelik" },
        [Il] = new[] { "il" },
        [Ilce] = new[] { "ilce" },
        [Mahalle] = new[] { "mahalle" },
        [Yuzolcum] = new[] { "yuzolcum" },
        [Ada] = new[] { "ada" },
        [Parsel] = new[] { "parsel" },
        [BagimsizBolumNo] = new[] { "bagimsiz", "bolum" },
        [ZeminHisseId] = new[] { "zemin", "hisse" }
    };

    /// <summary>
    /// Bir sütun başlığı HÜCRESİNİN (Excel) veya bir sütun FRAZININ (PDF, kelime grubu birleşimi)
    /// hangi kanonik sütuna denk geldiğini TAM eşleşme ile belirler. Kısa/genel kelimelerde
    /// ("il" gibi) yanlış eşleşmeyi önlemek için CONTAINS değil, TAM normalize edilmiş metin eşleşmesi kullanılır.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> BeklenenTamBasliklar = new Dictionary<string, string[]>
    {
        [TasinmazNo] = new[] { "tasinmaz no", "tasinmazno" },
        [Nitelik] = new[] { "nitelik" },
        [Il] = new[] { "il" },
        [Ilce] = new[] { "ilce" },
        [Mahalle] = new[] { "mahalle" },
        [Yuzolcum] = new[] { "yuzolcum", "yuz olcum" },
        [Ada] = new[] { "ada" },
        [Parsel] = new[] { "parsel" },
        [BagimsizBolumNo] = new[] { "bagimsiz bolum no", "bb no", "bagimsizbolumno" },
        [ZeminHisseId] = new[] { "zemin hisse id", "zemin hisse no", "zeminhisseid" }
    };

    public static string Normallestir(string metin) =>
        metin.Trim()
            .Replace('İ', 'i').Replace('I', 'i').Replace('ı', 'i')
            .Replace('Ç', 'c').Replace('ç', 'c')
            .Replace('Ğ', 'g').Replace('ğ', 'g')
            .Replace('Ö', 'o').Replace('ö', 'o')
            .Replace('Ş', 's').Replace('ş', 's')
            .Replace('Ü', 'u').Replace('ü', 'u')
            .ToLowerInvariant();

    /// <summary>Bir başlık metnini (hücre veya PDF sütun frazı) kanonik sütun adına eşler; eşleşmezse null döner.</summary>
    public static string? EslesenSutunAdi(string baslikMetni)
    {
        var normal = Normallestir(baslikMetni);
        foreach (var (kanonikAd, varyantlar) in BeklenenTamBasliklar)
        {
            if (varyantlar.Contains(normal))
                return kanonikAd;
        }
        return null;
    }

    public static string BeklenenBasliklarMetni() =>
        string.Join(", ", Tumu.Select(s => GoruntulemeAdlari[s]));
}
