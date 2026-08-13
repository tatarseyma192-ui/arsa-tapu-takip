using ArsaTapu.Business.Common;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Exceptions;

namespace ArsaTapu.Tests.TestYardimcilari;

/// <summary>Testlerde gerçek HTTP bağlamı olmadan Patron kapsamını simüle etmek için sahte servis.</summary>
public class SahteYetkiKapsamService : IYetkiKapsamService
{
    private readonly int? _patronKisiId;

    public SahteYetkiKapsamService(int? patronKisiId = null)
    {
        _patronKisiId = patronKisiId;
    }

    public Task<int?> PatronKisiIdGetirAsync(CancellationToken ct = default) => Task.FromResult(_patronKisiId);

    public Task KisiErisimKontrolEtAsync(int kisiId, CancellationToken ct = default)
    {
        if (_patronKisiId.HasValue && _patronKisiId.Value != kisiId)
            throw new YetkisizErisimException();

        return Task.CompletedTask;
    }

    public Task TasinmazSilYetkisiKontrolEtAsync(Tasinmaz tasinmaz, CancellationToken ct = default) =>
        Task.CompletedTask;
}
