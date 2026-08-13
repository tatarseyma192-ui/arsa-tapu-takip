using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Business.TasinmazYukleme;

/// <summary>
/// Requirements madde 2.1: "Onaylanınca sistem veritabanına işler + indirilebilir Excel üretir".
/// Önizlemedeki (veya onaylanmış) satırlardan indirilebilir bir .xlsx dosyası üretir.
/// </summary>
public interface IExcelUreticiService
{
    byte[] Uret(IReadOnlyList<MulkiyetAdayDto> satirlar);
}
