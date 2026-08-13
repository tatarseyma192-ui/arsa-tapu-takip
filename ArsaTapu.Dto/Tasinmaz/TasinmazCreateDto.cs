namespace ArsaTapu.Dto.Tasinmaz;

public class TasinmazCreateDto
{
    public int KisiId { get; set; }

    /// <summary>
    /// Opsiyonel — mülkiyet tekilleştirme anahtarının (BagimsizBolumNo + ZeminHisseId) parçası
    /// DEĞİLDİR (bkz. Tasinmaz.cs). Bazı kaynaklarda (ör. Excel) hiç bulunmayabilir.
    /// </summary>
    public string? TasinmazNo { get; set; }

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
}
