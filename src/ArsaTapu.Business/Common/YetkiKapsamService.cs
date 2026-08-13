using ArsaTapu.DataAccess.UnitOfWork;
using ArsaTapu.Domain.Common;
using ArsaTapu.Domain.Entities;
using ArsaTapu.Domain.Exceptions;

namespace ArsaTapu.Business.Common;

public class YetkiKapsamService : IYetkiKapsamService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    private int? _cachedPatronKisiId;
    private bool _cozuldu;

    public YetkiKapsamService(ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<int?> PatronKisiIdGetirAsync(CancellationToken ct = default)
    {
        if (_cozuldu) return _cachedPatronKisiId;

        if (_currentUser.RoldeMi(Roller.Patron) && !string.IsNullOrEmpty(_currentUser.UserId))
        {
            var kisi = await _unitOfWork.Kisiler.KullaniciIdIleGetirAsync(_currentUser.UserId, ct);
            _cachedPatronKisiId = kisi?.Id;
        }
        else
        {
            _cachedPatronKisiId = null;
        }

        _cozuldu = true;
        return _cachedPatronKisiId;
    }

    public async Task KisiErisimKontrolEtAsync(int kisiId, CancellationToken ct = default)
    {
        var patronKisiId = await PatronKisiIdGetirAsync(ct);
        if (patronKisiId.HasValue && patronKisiId.Value != kisiId)
        {
            throw new YetkisizErisimException("Bu kişiye ait verilere erişim yetkiniz yok.");
        }
    }

    public async Task TasinmazSilYetkisiKontrolEtAsync(Tasinmaz tasinmaz, CancellationToken ct = default)
    {
        if (_currentUser.RoldeMi(Roller.Admin)) return;

        if (_currentUser.RoldeMi(Roller.Personel))
        {
            if (tasinmaz.IlkGorulduguYuklemeId is null)
                throw new YetkisizErisimException(
                    "Bu kayıt bir yükleme ile ilişkili değil; yalnızca Admin silebilir.");

            var yukleme = await _unitOfWork.YuklemeKayitlari.GetirAsync(tasinmaz.IlkGorulduguYuklemeId.Value, ct);
            if (yukleme is null || yukleme.YukleyenKullaniciId != _currentUser.UserId)
                throw new YetkisizErisimException("Yalnızca kendi yüklediğiniz kayıtları silebilirsiniz.");

            return;
        }

        throw new YetkisizErisimException("Bu işlem için yetkiniz yok.");
    }
}
