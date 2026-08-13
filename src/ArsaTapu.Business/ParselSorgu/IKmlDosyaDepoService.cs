namespace ArsaTapu.Business.ParselSorgu;

/// <summary>
/// Handbook madde 14: "Dosya yolları sabit olarak kod içerisine yazılmamalı. Dosya depolama
/// yapısı değiştirilebilir olmalı. Dosya işlemleri merkezi olarak yönetilmelidir." Depolama
/// konumu appsettings üzerinden yapılandırılır; ileride bulut depolamaya geçilirse yalnızca
/// bu implementasyon değişir.
/// </summary>
public interface IKmlDosyaDepoService
{
    /// <summary>Requirements madde 4.2: {İL}_{İLÇE}_{MAHALLE}_{ADA}_{PARSEL}.kml</summary>
    string DosyaAdiOlustur(string il, string ilce, string mahalle, int ada, int parsel);

    /// <summary>Dosyayı kaydeder, DB'ye yazılacak (göreli) dosya yolunu döner.</summary>
    Task<string> KaydetAsync(string dosyaAdi, byte[] icerik, CancellationToken ct = default);

    Task SilAsync(string dosyaYolu, CancellationToken ct = default);
}
