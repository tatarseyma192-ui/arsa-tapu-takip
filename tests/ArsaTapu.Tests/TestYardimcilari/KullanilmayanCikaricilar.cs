using ArsaTapu.Business.TasinmazYukleme;
using ArsaTapu.Dto.Tekillestirme;

namespace ArsaTapu.Tests.TestYardimcilari;

/// <summary>
/// TasinmazYuklemeService.OnaylaVeIsleAsync testlerinde PDF/Excel çıkarıcıları hiç
/// kullanılmaz (doğrudan onay isteğiyle çağrılır); bu sahte sınıflar yalnızca DI
/// gereksinimini karşılamak için var, çağrılırlarsa test kasıtlı olarak patlar.
/// </summary>
public class KullanilmayanPdfSatirCikarici : IPdfSatirCikarici
{
    public List<Dictionary<string, string?>> SatirlariCikar(Stream pdfStream) => throw new NotImplementedException();
}

public class KullanilmayanExcelSatirCikarici : IExcelSatirCikarici
{
    public List<Dictionary<string, string?>> SatirlariCikar(Stream excelStream) => throw new NotImplementedException();
}

public class KullanilmayanExcelUreticiService : IExcelUreticiService
{
    public byte[] Uret(IReadOnlyList<MulkiyetAdayDto> satirlar) => throw new NotImplementedException();
}
