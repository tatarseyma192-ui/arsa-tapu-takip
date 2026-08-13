namespace ArsaTapu.Dto.ParselKml;

/// <summary>Requirements madde 5 girdisi: KML tetikleme listesinden gelen tek parsel isteği.</summary>
public class ParselSorguIstegiDto
{
    public string Il { get; set; } = null!;
    public string Ilce { get; set; } = null!;
    public string Mahalle { get; set; } = null!;
    public int Ada { get; set; }
    public int Parsel { get; set; }

    /// <summary>KML description alanına yazılacak referanslar (madde 4.2). Boş bırakılabilir.</summary>
    public List<TasinmazReferansDto> TasinmazReferanslari { get; set; } = new();
}
