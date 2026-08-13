using Microsoft.Extensions.Configuration;

namespace ArsaTapu.Business.ParselSorgu;

public class ParselSorguHizSinirlayici : IParselSorguHizSinirlayici
{
    private readonly TimeSpan _minimumAralik;
    private readonly SemaphoreSlim _kilit = new(1, 1);
    private DateTime _sonIstekZamaniUtc = DateTime.MinValue;

    public ParselSorguHizSinirlayici(IConfiguration configuration)
    {
        var milisaniye = configuration.GetValue<int?>("ParselSorgu:MinimumAralikMs") ?? 2000;
        _minimumAralik = TimeSpan.FromMilliseconds(milisaniye);
    }

    public async Task BeklemeSuresinceBeklaAsync(CancellationToken ct = default)
    {
        await _kilit.WaitAsync(ct);
        try
        {
            var simdi = DateTime.UtcNow;
            var gecenSure = simdi - _sonIstekZamaniUtc;

            if (gecenSure < _minimumAralik)
            {
                await Task.Delay(_minimumAralik - gecenSure, ct);
            }

            _sonIstekZamaniUtc = DateTime.UtcNow;
        }
        finally
        {
            _kilit.Release();
        }
    }
}
