namespace ArsaTapu.Business.ParselSorgu;

/// <summary>
/// TKGM sorgu istemcisinin ham sonucu. Gerçek TKGM API'si (HAR kaydından doğrulandı) parsel
/// sorgusu sonrası doğrudan indirilebilir bir .kml dosyası döndürüyor — bu yüzden burada ham
/// koordinat değil, doğrudan KML bayt dizisi taşınır (ParselKmlService bunun üzerine yalnızca
/// açıklama/description enjeksiyonu yapar, geometriyi yeniden İNŞA ETMEZ).
/// </summary>
public class ParselSorguIstemciSonucu
{
    public bool Basarili { get; init; }
    public byte[]? KmlBaytlari { get; init; }

    /// <summary>Yalnızca Basarili=false iken doludur; kullanıcıya gösterilebilir (teknik detay içermez).</summary>
    public string? HataMesaji { get; init; }

    public static ParselSorguIstemciSonucu BasariliSonuc(byte[] kmlBaytlari) =>
        new() { Basarili = true, KmlBaytlari = kmlBaytlari };

    public static ParselSorguIstemciSonucu BasarisizSonuc(string hataMesaji) =>
        new() { Basarili = false, HataMesaji = hataMesaji };
}
