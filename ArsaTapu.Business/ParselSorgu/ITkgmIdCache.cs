namespace ArsaTapu.Business.ParselSorgu;

/// <summary>
/// İlçe/mahalle listelerinin önbelleği. Kullanıcı isteği: "mahalle ID'lerini önbelleğe alıp
/// tekrar tekrar sorgulamamak mantıklı olur, TKGM sitesine gereksiz yük binmesin." Singleton
/// olarak kaydedilir — TkgmParselSorguIstemcisi (AddHttpClient ile Transient) her çağrıda
/// yeniden oluşsa bile önbellek TÜM UYGULAMA ÖMRÜ boyunca paylaşılır.
/// </summary>
public interface ITkgmIdCache
{
    List<(int Id, string Text)>? IlceListesiGetir(int ilId);
    void IlceListesiKaydet(int ilId, List<(int Id, string Text)> liste);

    List<(int Id, string Text)>? MahalleListesiGetir(int ilceId);
    void MahalleListesiKaydet(int ilceId, List<(int Id, string Text)> liste);
}
