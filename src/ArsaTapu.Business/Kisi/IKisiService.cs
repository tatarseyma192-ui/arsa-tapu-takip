using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.Kisi;

namespace ArsaTapu.Business.Kisi;

public interface IKisiService
{
    Task<PagedResult<KisiDto>> ListeleAsync(PagedRequest istek, CancellationToken ct = default);
    Task<KisiDto> GetirAsync(int id, CancellationToken ct = default);

    /// <summary>Patron kendi profilini görür. Patron değilse veya bağlı Kisi yoksa null döner.</summary>
    Task<KisiDto?> KendiProfiliniGetirAsync(CancellationToken ct = default);

    Task<KisiDto> OlusturAsync(KisiCreateDto istek, CancellationToken ct = default);
    Task<KisiDto> GuncelleAsync(int id, KisiUpdateDto istek, CancellationToken ct = default);
    Task SilAsync(int id, CancellationToken ct = default);
}
