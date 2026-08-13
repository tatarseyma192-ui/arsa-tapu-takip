namespace ArsaTapu.Dto.Tekillestirme;

public class KmlTekillestirmeSonucuDto
{
    public List<ParselAdayDto> SorgulanmasiGerekenler { get; set; } = new();
    public List<ParselAdayDto> ZatenCekilmisOlanlar { get; set; } = new();
}
