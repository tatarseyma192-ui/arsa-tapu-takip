using Microsoft.Extensions.Configuration;

namespace ArsaTapu.Business.ParselSorgu;

public class KmlDosyaDepoService : IKmlDosyaDepoService
{
    private readonly string _kokDizin;

    public KmlDosyaDepoService(IConfiguration configuration)
    {
        var yapilandirilanYol = configuration["KmlDepolama:KokDizin"] ?? "App_Data/kml";
        _kokDizin = Path.IsPathRooted(yapilandirilanYol)
            ? yapilandirilanYol
            : Path.Combine(Directory.GetCurrentDirectory(), yapilandirilanYol);

        Directory.CreateDirectory(_kokDizin);
    }

    public string DosyaAdiOlustur(string il, string ilce, string mahalle, int ada, int parsel)
    {
        string Temizle(string parca)
        {
            // Dosya adları için Türkçe karakterler ASCII eşdeğerine çevrilir (Requirements madde 4.2
            // örneği düz ASCII: "GAZIANTEP_SAHINBEY_BINEVLER_171_190.kml") — indirme/ZIP/URL
            // uyumluluğu açısından ekran metninden (görüntüleme adları) daha temkinli olunur.
            var donusturulmus = parca.Trim()
                .Replace('İ', 'I').Replace('I', 'I').Replace('ı', 'i')
                .Replace('Ç', 'C').Replace('ç', 'c')
                .Replace('Ğ', 'G').Replace('ğ', 'g')
                .Replace('Ö', 'O').Replace('ö', 'o')
                .Replace('Ş', 'S').Replace('ş', 's')
                .Replace('Ü', 'U').Replace('ü', 'u')
                .Replace(' ', '-');

            var gecersizler = Path.GetInvalidFileNameChars();
            var temiz = new string(donusturulmus.Where(c => !gecersizler.Contains(c)).ToArray());
            return temiz.ToUpperInvariant();
        }

        return $"{Temizle(il)}_{Temizle(ilce)}_{Temizle(mahalle)}_{ada}_{parsel}.kml";
    }

    public async Task<string> KaydetAsync(string dosyaAdi, byte[] icerik, CancellationToken ct = default)
    {
        var tamYol = Path.Combine(_kokDizin, dosyaAdi);
        await File.WriteAllBytesAsync(tamYol, icerik, ct);

        // DB'ye yalnızca dosya ADI kaydedilir (Handbook madde 14: yol kod/DB içine sabitlenmez);
        // kök dizin appsettings'ten okunarak her erişimde yeniden birleştirilir.
        return dosyaAdi;
    }

    public Task SilAsync(string dosyaYolu, CancellationToken ct = default)
    {
        var tamYol = Path.Combine(_kokDizin, dosyaYolu);
        if (File.Exists(tamYol))
            File.Delete(tamYol);

        return Task.CompletedTask;
    }
}
