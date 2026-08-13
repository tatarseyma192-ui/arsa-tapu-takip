using ArsaTapu.Dto.Tekillestirme;
using ClosedXML.Excel;

namespace ArsaTapu.Business.TasinmazYukleme;

public class ExcelUreticiService : IExcelUreticiService
{
    public byte[] Uret(IReadOnlyList<MulkiyetAdayDto> satirlar)
    {
        using var workbook = new XLWorkbook();
        var sayfa = workbook.Worksheets.Add("Taşınmazlar");

        for (var i = 0; i < KanonikSutunlar.Tumu.Length; i++)
        {
            sayfa.Cell(1, i + 1).Value = KanonikSutunlar.GoruntulemeAdlari[KanonikSutunlar.Tumu[i]];
            sayfa.Cell(1, i + 1).Style.Font.Bold = true;
        }

        for (var satirIndex = 0; satirIndex < satirlar.Count; satirIndex++)
        {
            var s = satirlar[satirIndex];
            var satirNo = satirIndex + 2;

            // Nullable sayısal alanlar için: değer varsa sayı olarak, yoksa hücre boş bırakılır
            // (ClosedXML'in .Value setter'ı nullable value type'lardan doğrudan atamayı desteklemeyebilir).
            sayfa.Cell(satirNo, 1).Value = s.TasinmazNo ?? "";
            sayfa.Cell(satirNo, 2).Value = s.Nitelik ?? "";
            sayfa.Cell(satirNo, 3).Value = s.Il ?? "";
            sayfa.Cell(satirNo, 4).Value = s.Ilce ?? "";
            sayfa.Cell(satirNo, 5).Value = s.Mahalle ?? "";

            if (s.Yuzolcum.HasValue) sayfa.Cell(satirNo, 6).Value = s.Yuzolcum.Value;
            if (s.Ada.HasValue) sayfa.Cell(satirNo, 7).Value = s.Ada.Value;
            if (s.Parsel.HasValue) sayfa.Cell(satirNo, 8).Value = s.Parsel.Value;
            if (s.BagimsizBolumNo.HasValue) sayfa.Cell(satirNo, 9).Value = s.BagimsizBolumNo.Value;

            sayfa.Cell(satirNo, 10).Value = s.ZeminHisseId ?? "";
        }

        sayfa.Columns().AdjustToContents();

        using var bellekAkisi = new MemoryStream();
        workbook.SaveAs(bellekAkisi);
        return bellekAkisi.ToArray();
    }
}
