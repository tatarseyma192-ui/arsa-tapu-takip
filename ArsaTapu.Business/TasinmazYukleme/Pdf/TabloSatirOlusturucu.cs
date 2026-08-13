namespace ArsaTapu.Business.TasinmazYukleme.Pdf;

/// <summary>
/// PDF kelime konumlarından tablo satır/sütun yapısını yeniden kurar. Herhangi bir PDF
/// kütüphanesine bağımlı DEĞİLDİR (yalnızca KonumluKelime kullanır) — bu sayede PDF
/// kütüphanesi değişse bile bu algoritma değişmeden kalır (Requirements madde 5).
///
/// Strateji:
///  1. Kelimeler Y konumuna göre satırlara kümelenir (aynı satırdaki kelimelerin merkez Y'si
///     birbirine yakın olur; tolerans, sayfadaki ortalama kelime yüksekliğinden türetilir).
///  2. Başlık satırındaki kelimeler ARALARINDAKİ EN BÜYÜK BOŞLUKLARA göre sütun gruplarına
///     ayrılır (sütunlar arası boşluk, bir sütun içindeki kelimeler arası boşluktan belirgin
///     şekilde büyüktür) — bu, sütun X sınırlarını verir.
///  3. Her veri satırındaki kelimeler, bu sütun sınırlarının MERKEZİNE en yakın olana atanır
///     (bir sütun o satırda boşsa — ör. Bağımsız Bölüm No boş olabilir — sonraki kelimeler
///     kaymadan doğru sütuna düşer, çünkü atama sınır ARALIĞINA değil MERKEZE yakınlığa göredir).
/// </summary>
public static class TabloSatirOlusturucu
{
    /// <summary>
    /// Gerçek bir WebTapu PDF'inde (2026-08-04 örneğiyle doğrulandı) büyük, döndürülmüş bir
    /// filigran metni ("BİLGİ AMAÇLIDIR", her harfi 100+ punto) gerçek 12pt tablo metniyle
    /// karışabiliyor — bazen ayrı "kelime" olarak (PdfPig/pdfplumber'ın kelime birleştirmesi
    /// yüzünden) gerçek veriyle TEK kelimeye kaynaşmış halde bile çıkabiliyor (ör. Yüzölçüm
    /// değeriyle Ada değerinin arasına giren bir filigran harfi ikisini "241.51Ç1093" gibi TEK
    /// kelime yapabiliyor). Bu, kelime seviyesinde filtrelemeyle çözülemez — KARAKTER seviyesinde
    /// sınırlayıcı kutu yüksekliğine göre filtrelenip kelimeler sıfırdan bu temiz karakterlerden kurulur.
    ///
    /// filigranYuksekligiEsigi: bu değerden BÜYÜK karakterler atılır. Gerçek tablo metni
    /// tipik olarak 8-16pt'tir, filigranlar genelde sayfayı kaplayacak kadar büyüktür (100+pt) —
    /// aradaki fark çok büyük olduğundan tek bir sabit eşik güvenle ayırt edebilir.
    /// </summary>
    public static List<KonumluKelime> HarflerdenKelimeOlustur(
        IReadOnlyList<KonumluHarf> harfler, double filigranYuksekligiEsigi = 36.0)
    {
        var gercekHarfler = harfler
            .Where(h => h.Yukseklik <= filigranYuksekligiEsigi && !string.IsNullOrWhiteSpace(h.Metin))
            .ToList();

        if (gercekHarfler.Count == 0) return new List<KonumluKelime>();

        // Kelime-arası boşluk eşiği, karakter YÜKSEKLİĞİYLE ORANTILI hesaplanır (mutlak sabit
        // değil) — farklı bir WebTapu ihracatında yazı boyutu değişirse de makul kalması için.
        // Gerçek örnekte 12pt yazıda kelime-arası boşluk ~3pt, harf-içi boşluk ~0pt idi (oran ~0.2).
        var ortalamaYukseklik = gercekHarfler.Average(h => h.Yukseklik);
        var kelimeArasiBoslukEsigi = Math.Max(ortalamaYukseklik * 0.2, 1.0);

        var sirali = gercekHarfler.OrderByDescending(h => h.UstY).ThenBy(h => h.SolX).ToList();

        var kelimeGruplari = new List<List<KonumluHarf>>();
        List<KonumluHarf>? mevcutGrup = null;

        foreach (var harf in sirali)
        {
            if (mevcutGrup is not null)
            {
                var onceki = mevcutGrup[^1];
                var ayniSatir = Math.Abs(onceki.UstY - harf.UstY) < 3;
                var bosluk = harf.SolX - onceki.SagX;

                if (ayniSatir && bosluk < kelimeArasiBoslukEsigi)
                {
                    mevcutGrup.Add(harf);
                    continue;
                }
            }

            mevcutGrup = new List<KonumluHarf> { harf };
            kelimeGruplari.Add(mevcutGrup);
        }

        return kelimeGruplari.Select(grup => new KonumluKelime(
            string.Concat(grup.Select(h => h.Metin)),
            grup.Min(h => h.SolX),
            grup.Max(h => h.SagX),
            grup.Max(h => h.UstY),
            grup.Min(h => h.AltY))).ToList();
    }

    public static List<List<KonumluKelime>> SatirlaraGrupla(IReadOnlyList<KonumluKelime> kelimeler)
    {
        if (kelimeler.Count == 0) return new List<List<KonumluKelime>>();

        var ortalamaYukseklik = kelimeler.Average(k => k.Yukseklik);
        var tolerans = Math.Max(ortalamaYukseklik * 0.6, 1.0);

        var siraliKelimeler = kelimeler.OrderByDescending(k => k.MerkezY).ToList(); // yukarıdan aşağıya

        var satirlar = new List<List<KonumluKelime>>();
        List<KonumluKelime>? mevcutSatir = null;

        foreach (var kelime in siraliKelimeler)
        {
            var mevcutSatirOrtalamaY = mevcutSatir?.Average(k => k.MerkezY);

            if (mevcutSatir is null || Math.Abs(mevcutSatirOrtalamaY!.Value - kelime.MerkezY) > tolerans)
            {
                mevcutSatir = new List<KonumluKelime>();
                satirlar.Add(mevcutSatir);
            }

            mevcutSatir!.Add(kelime);
        }

        foreach (var satir in satirlar)
            satir.Sort((a, b) => a.MerkezX.CompareTo(b.MerkezX));

        return satirlar;
    }

    /// <summary>Başlık satırındaki kelimeleri, aralarındaki en büyük boşluklara göre beklenenSutunSayisi gruba ayırır.</summary>
    public static List<(double SolSinir, double SagSinir)> SutunSinirlariniHesapla(
        IReadOnlyList<KonumluKelime> baslikSatiri, int beklenenSutunSayisi)
    {
        var sirali = baslikSatiri.OrderBy(k => k.MerkezX).ToList();

        if (sirali.Count <= beklenenSutunSayisi)
            return sirali.Select(k => (k.SolX, k.SagX)).ToList();

        var bosluklar = new List<(int Index, double Bosluk)>();
        for (var i = 0; i < sirali.Count - 1; i++)
            bosluklar.Add((i, sirali[i + 1].SolX - sirali[i].SagX));

        var sinirIndeksleri = bosluklar
            .OrderByDescending(b => b.Bosluk)
            .Take(beklenenSutunSayisi - 1)
            .Select(b => b.Index)
            .ToHashSet();

        var gruplar = new List<List<KonumluKelime>> { new() { sirali[0] } };
        for (var i = 0; i < sirali.Count - 1; i++)
        {
            if (sinirIndeksleri.Contains(i))
                gruplar.Add(new List<KonumluKelime>());

            gruplar[^1].Add(sirali[i + 1]);
        }

        return gruplar.Select(g => (g.Min(k => k.SolX), g.Max(k => k.SagX))).ToList();
    }

    /// <summary>Bir satırdaki kelimeleri, en yakın sütun MERKEZİNE göre sütunlara dağıtır.</summary>
    public static string[] SatiriSutunlaraAyir(
        IReadOnlyList<KonumluKelime> satir, IReadOnlyList<(double Sol, double Sag)> sutunSinirlari)
    {
        var sutunMetinleri = new string[sutunSinirlari.Count];
        for (var i = 0; i < sutunMetinleri.Length; i++) sutunMetinleri[i] = "";

        var merkezler = sutunSinirlari.Select(s => (s.Sol + s.Sag) / 2).ToList();

        foreach (var kelime in satir.OrderBy(k => k.MerkezX))
        {
            var enYakinIndex = 0;
            var enKucukMesafe = double.MaxValue;
            for (var i = 0; i < merkezler.Count; i++)
            {
                var mesafe = Math.Abs(kelime.MerkezX - merkezler[i]);
                if (mesafe < enKucukMesafe)
                {
                    enKucukMesafe = mesafe;
                    enYakinIndex = i;
                }
            }

            sutunMetinleri[enYakinIndex] = string.IsNullOrEmpty(sutunMetinleri[enYakinIndex])
                ? kelime.Metin
                : sutunMetinleri[enYakinIndex] + " " + kelime.Metin;
        }

        return sutunMetinleri;
    }

    /// <summary>
    /// Satırlar arasından başlık satırını bulur: KanonikSutunlar.BaslikAnahtarKelimeleri'nden
    /// en az 5 tanesiyle (gevşek/substring) eşleşen satır. Bulunamazsa null döner.
    /// </summary>
    public static int? BaslikSatiriniBul(IReadOnlyList<List<KonumluKelime>> satirlar)
    {
        var enIyiIndex = -1;
        var enIyiSkor = 0;

        for (var i = 0; i < satirlar.Count; i++)
        {
            var satirMetni = KanonikSutunlar.Normallestir(string.Join(" ", satirlar[i].Select(k => k.Metin)));
            var skor = KanonikSutunlar.BaslikAnahtarKelimeleri.Count(kv => kv.Value.All(satirMetni.Contains));

            if (skor > enIyiSkor)
            {
                enIyiSkor = skor;
                enIyiIndex = i;
            }
        }

        return enIyiSkor >= 5 ? enIyiIndex : null;
    }

    /// <summary>
    /// Gerçek bir WebTapu PDF'inde (2026-08-04 örneğiyle doğrulandı) uzun bir Nitelik değeri
    /// ("1 KATLI OTO GALERİ 2 MESKENLİ KARGİR BİNA" gibi) veya uzun bir İlçe/Mahalle adı
    /// ("Şehitkamil" gibi) birden fazla görsel satıra yayılabiliyor — yalnızca satırın İLK
    /// satırında Taşınmaz No/Ada/Parsel/Zemin Hisse ID basılı oluyor, geri kalan sütun metni
    /// SONRAKİ satır(lar)a taşıyor. Bu metod, anahtar sütunları BOŞ olan satırları bir önceki
    /// "gerçek" (anahtar sütunlu) satıra ait devam parçası sayıp İLGİLİ sütuna ekler.
    ///
    /// Sayfa altbilgisi/başlığı gibi İLGİSİZ boş satırların yanlışlıkla eklenmesini önlemek için
    /// yalnızca bir önceki işlenen satıra YETERİNCE YAKIN (birlestirmeEsigiY) olan parçalar
    /// birleştirilir — gerçek WebTapu örneğinde parça-arası boşluk (~14pt) normal satır arası
    /// boşluktan (~19pt) belirgin küçük, sayfa altbilgisi boşluğu (~29pt) ise belirgin büyüktü.
    /// </summary>
    public static List<string[]> SatirParcalariniBirlestir(
        IReadOnlyList<(double OrtalamaY, string[] Sutunlar)> satirlar,
        IReadOnlyList<int> anahtarSutunIndeksleri,
        double birlestirmeEsigiY)
    {
        var sonuc = new List<string[]>();
        double? sonIslenenY = null;

        foreach (var (ortalamaY, sutunlar) in satirlar)
        {
            // TÜMÜ (All) kasıtlı — yalnızca BİRİ (Any) olsaydı, ör. sayfa numarası gibi bir
            // gürültü metni sırf ZeminHisseId sütununun en-yakın-merkezine düşmüş olsa bile
            // "gerçek satır" sanılabilirdi. Gerçek bir kayıtta bu dört alan HER ZAMAN aynı
            // satırda birlikte basılır (bkz. gerçek WebTapu örneği doğrulaması).
            var anahtarVar = anahtarSutunIndeksleri.All(i => !string.IsNullOrWhiteSpace(sutunlar[i]));

            if (anahtarVar)
            {
                sonuc.Add((string[])sutunlar.Clone());
                sonIslenenY = ortalamaY;
                continue;
            }

            if (sonuc.Count == 0 || sonIslenenY is null) continue;

            if (Math.Abs(ortalamaY - sonIslenenY.Value) > birlestirmeEsigiY) continue; // ilgisiz gürültü (ör. sayfa altbilgisi)

            var hedefSatir = sonuc[^1];
            for (var s = 0; s < sutunlar.Length; s++)
            {
                var deger = sutunlar[s]?.Trim();
                if (string.IsNullOrEmpty(deger)) continue;

                hedefSatir[s] = string.IsNullOrWhiteSpace(hedefSatir[s]) ? deger : $"{hedefSatir[s]} {deger}";
            }

            sonIslenenY = ortalamaY;
        }

        return sonuc;
    }
}
