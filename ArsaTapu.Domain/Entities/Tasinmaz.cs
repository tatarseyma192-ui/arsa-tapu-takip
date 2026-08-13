using ArsaTapu.Domain.Common;
using ArsaTapu.Domain.Enums;

namespace ArsaTapu.Domain.Entities;

/// <summary>
/// Requirements madde 8: Tasinmaz.
/// Mülkiyet tekilleştirme anahtarı: BagimsizBolumNo + ZeminHisseId (TasinmazNo ANAHTARIN
/// PARÇASI DEĞİLDİR — 2026-08-04'te sağlanan gerçek bir Excel örneğinde bu sütun hiç yoktu;
/// aynı kişiye ait gerçek bir kayıt hem PDF'te TasinmazNo'lu hem Excel'de TasinmazNo'suz
/// görülebildiğinden, TasinmazNo yalnızca AÇIKLAYICI/referans bir alandır, eşleştirmede
/// KULLANILMAZ — ZeminHisseId zaten tek başına stabil/tekil bir kimlik olarak davranıyor).
/// Ortaklık/komşuluk hesaplaması da bu alanlar üzerinden yapılır (bkz. OrtaklikService).
/// </summary>
public class Tasinmaz : BaseEntity
{
    public int KisiId { get; set; }
    public Kisi? Kisi { get; set; }

    /// <summary>
    /// Nullable: yalnızca WebTapu PDF'inde bulunur, Excel formatında hiç yer almayabilir
    /// (2026-08-04'te doğrulandı). Eşleştirme/tekilleştirmede KULLANILMAZ — yalnızca
    /// mevcutsa görüntülemede/referans amaçlı tutulur.
    /// </summary>
    public string? TasinmazNo { get; set; }

    public string Nitelik { get; set; } = null!;

    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }

    /// <summary>Bağımsız bölüm yoksa (arsa vasıflı taşınmazlarda) null olabilir.</summary>
    public int? BagimsizBolumNo { get; set; }

    public string ZeminHisseId { get; set; } = null!;
    public decimal Yuzolcum { get; set; }

    public TasinmazDurum Durum { get; set; } = TasinmazDurum.Aktif;

    /// <summary>
    /// Nullable: manuel (Admin/Personel elle) girilen kayıtlarda bir yükleme kökeni olmayabilir.
    /// PDF/Excel parse motoru devreye girdiğinde bu alan otomatik doldurulacaktır.
    /// </summary>
    public int? IlkGorulduguYuklemeId { get; set; }
    public YuklemeKaydi? IlkGorulduguYukleme { get; set; }

    public int? SonGorulduguYuklemeId { get; set; }
    public YuklemeKaydi? SonGorulduguYukleme { get; set; }
}
