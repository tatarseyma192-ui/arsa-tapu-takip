using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Business.Tekillestirme;

/// <summary>
/// Requirements madde 4.2: KML tekilleştirme anahtarı = Il + Ilce + Mahalle + Ada + Parsel.
/// Mülkiyet tekilleştirme anahtarından (IMulkiyetTekillestirmeService) KASITLI olarak farklıdır.
/// </summary>
public interface IKmlTekillestirmeService
{
    Task<bool> CekilmisMiAsync(
        string il, string ilce, string mahalle, int ada, int parsel, CancellationToken ct = default);

    /// <summary>
    /// Aday Ada/Parsel listesini "sorgulanması gereken" / "zaten çekilmiş" olarak sınıflandırır.
    /// Aynı istek/parti içinde tekrarlanan Ada/Parsel'ler için de yalnızca 1 sorgu üretir.
    /// Parsel Sorgu (TKGM) entegrasyonu (ayrı adımda gelecek) bu metodu kullanacaktır.
    /// </summary>
    Task<KmlTekillestirmeSonucuDto> SiniflandirAsync(
        IReadOnlyList<ParselAdayDto> adaylar, CancellationToken ct = default);
}
