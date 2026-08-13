using ArsaTapu.Domain.Exceptions;
using ClosedXML.Excel;

namespace ArsaTapu.Business.TasinmazYukleme;

public class ExcelSatirCikarici : IExcelSatirCikarici
{
    public List<Dictionary<string, string?>> SatirlariCikar(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);
        var sayfa = workbook.Worksheets.FirstOrDefault()
            ?? throw new BusinessRuleException("Excel dosyasında hiç sayfa bulunamadı.");

        var kullanilanAralik = sayfa.RangeUsed()
            ?? throw new BusinessRuleException("Excel dosyası boş görünüyor.");

        var satirlar = kullanilanAralik.RowsUsed().ToList();
        if (satirlar.Count < 2)
            throw new BusinessRuleException("Excel dosyasında başlık satırından sonra veri satırı bulunamadı.");

        var baslikSatiri = satirlar[0];
        var sutunEslesmeleri = new Dictionary<int, string>(); // hücre sütun no -> kanonik ad

        foreach (var hucre in baslikSatiri.CellsUsed())
        {
            var kanonikAd = KanonikSutunlar.EslesenSutunAdi(hucre.GetString());
            if (kanonikAd is not null)
                sutunEslesmeleri[hucre.Address.ColumnNumber] = kanonikAd;
        }

        var eksikSutunlar = KanonikSutunlar.Zorunlu.Where(z => !sutunEslesmeleri.ContainsValue(z)).ToList();
        if (eksikSutunlar.Count > 0)
        {
            var eksikAdlari = string.Join(", ", eksikSutunlar.Select(s => KanonikSutunlar.GoruntulemeAdlari[s]));
            throw new BusinessRuleException(
                $"Excel dosyasında beklenen sütun(lar) bulunamadı: {eksikAdlari}. " +
                $"Sütun başlıkları şu şekilde olmalı: {KanonikSutunlar.BeklenenBasliklarMetni()}");
        }

        var sonuc = new List<Dictionary<string, string?>>();
        foreach (var satir in satirlar.Skip(1))
        {
            var satirVerisi = new Dictionary<string, string?>();
            foreach (var (sutunNo, kanonikAd) in sutunEslesmeleri)
            {
                satirVerisi[kanonikAd] = satir.Cell(sutunNo).GetString();
            }

            // Tamamen boş satırları (ör. tablo sonundaki boş satırlar) atla.
            if (satirVerisi.Values.All(string.IsNullOrWhiteSpace)) continue;

            sonuc.Add(satirVerisi);
        }

        return sonuc;
    }
}
