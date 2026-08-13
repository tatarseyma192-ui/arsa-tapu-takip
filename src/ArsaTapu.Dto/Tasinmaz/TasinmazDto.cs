namespace ArsaTapu.Dto.Tasinmaz;

public class TasinmazDto
{
    public int Id { get; set; }
    public int KisiId { get; set; }
    public string? KisiAdSoyad { get; set; }

    /// <summary>Nullable — bazı kaynaklarda (ör. Excel) bulunmayabilir, bkz. Tasinmaz.cs.</summary>
    public string? TasinmazNo { get; set; }
    public string Nitelik { get; set; } = null!;
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }
    public int? BagimsizBolumNo { get; set; }
    public string ZeminHisseId { get; set; } = null!;
    public decimal Yuzolcum { get; set; }

    /// <summary>"Aktif" | "Satildi"</summary>
    public string Durum { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Yalnızca tekil kayıt getirilirken (GetirAsync) doldurulur — listelemede BOŞ bırakılır
    /// (performans; her satır için ayrı sorgu gerektirir). Aynı BagimsizBolumNo + ZeminHisseId'yi
    /// paylaşan BAŞKA kişilerin adları (gerçek ortaklık, madde 6).
    /// </summary>
    public List<string> OrtakKisiler { get; set; } = new();

    /// <summary>Aynı Il/Ilce/Mahalle/Ada/Parsel'de, farklı Bağımsız Bölüm/Zemin Hisse ID'ye sahip başka kişilerin adları (komşuluk, ortaklık DEĞİLDİR).</summary>
    public List<string> KomsuKisiler { get; set; } = new();
}
