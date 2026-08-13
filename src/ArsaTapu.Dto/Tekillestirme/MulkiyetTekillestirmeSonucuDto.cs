namespace ArsaTapu.Dto.Tekillestirme;

public class MulkiyetTekillestirmeSonucuDto
{
    public List<MulkiyetAdayDto> YeniAlimlar { get; set; } = new();
    public List<MulkiyetAdayDto> ZatenKayitliOlanlar { get; set; } = new();
}
