using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.Tasinmaz;

namespace ArsaTapu.Business.Tasinmaz;

public interface ITasinmazService
{
    Task<PagedResult<TasinmazDto>> ListeleAsync(TasinmazFiltreDto filtre, CancellationToken ct = default);
    Task<TasinmazDto> GetirAsync(int id, CancellationToken ct = default);
    Task<TasinmazDto> OlusturAsync(TasinmazCreateDto istek, CancellationToken ct = default);
    Task<TasinmazDto> GuncelleAsync(int id, TasinmazUpdateDto istek, CancellationToken ct = default);
    Task SilAsync(int id, CancellationToken ct = default);
}
