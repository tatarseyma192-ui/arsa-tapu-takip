using System.Collections.Concurrent;

namespace ArsaTapu.Business.ParselSorgu;

public class TkgmIdCache : ITkgmIdCache
{
    private readonly ConcurrentDictionary<int, List<(int Id, string Text)>> _ilceListeleri = new();
    private readonly ConcurrentDictionary<int, List<(int Id, string Text)>> _mahalleListeleri = new();

    public List<(int Id, string Text)>? IlceListesiGetir(int ilId) =>
        _ilceListeleri.TryGetValue(ilId, out var liste) ? liste : null;

    public void IlceListesiKaydet(int ilId, List<(int Id, string Text)> liste) =>
        _ilceListeleri[ilId] = liste;

    public List<(int Id, string Text)>? MahalleListesiGetir(int ilceId) =>
        _mahalleListeleri.TryGetValue(ilceId, out var liste) ? liste : null;

    public void MahalleListesiKaydet(int ilceId, List<(int Id, string Text)> liste) =>
        _mahalleListeleri[ilceId] = liste;
}
