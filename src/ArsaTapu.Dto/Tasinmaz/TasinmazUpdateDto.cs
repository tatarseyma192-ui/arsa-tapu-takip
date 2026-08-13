namespace ArsaTapu.Dto.Tasinmaz;

/// <summary>
/// KisiId güncellemede değiştirilemez (mülkiyet tekilleştirme anahtarının — BagimsizBolumNo +
/// ZeminHisseId — hangi kişiye ait olduğunu belirler). TasinmazNo zaten anahtarın parçası
/// DEĞİLDİR (bkz. Tasinmaz.cs) ama yine de burada yer almaz — güncellemede değişmeyeceği
/// varsayılır. Kişi/kayıt hatası varsa kayıt silinip yeniden oluşturulmalıdır.
/// </summary>
public class TasinmazUpdateDto
{
    public string Nitelik { get; set; } = null!;
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;

    /// <summary>0 olabilir — bazı taşınmaz türlerinde (ör. yol/tarla parselleri) ada atanmaz.</summary>
    public int Ada { get; set; }
    public int Parsel { get; set; }
    public int? BagimsizBolumNo { get; set; }
    public string ZeminHisseId { get; set; } = null!;
    public decimal Yuzolcum { get; set; }

    /// <summary>"Aktif" | "Satildi"</summary>
    public string Durum { get; set; } = null!;
}
