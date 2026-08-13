using System.Net.Http.Headers;
using System.Net.Http.Json;
using ArsaTapu.Dto.Auth;
using ArsaTapu.Dto.Common;
using ArsaTapu.Dto.Kisi;
using ArsaTapu.Dto.Tasinmaz;

namespace ArsaTapu.Web.Services;

/// <summary>
/// ArsaTapu.Api'ye HTTP üzerinden konuşan TEK sınıf (Handbook madde 3: "Tüm veri alışverişi
/// API üzerinden gerçekleştirilmelidir"). Bu proje Business/DataAccess'e HİÇ referans vermez —
/// React ön yüzünün yapacağı işi burada Razor Pages + bu istemci yapar.
/// </summary>
public interface IApiIstemcisi
{
    Task<LoginResponseDto?> GirisYapAsync(string kullaniciAdiVeyaEposta, string sifre);
    Task<PagedResult<KisiDto>?> KisileriListeleAsync(string jwt, int sayfa = 1, string? arama = null);
    Task<KisiDto?> KisiGetirAsync(int id, string jwt);
    Task<PagedResult<TasinmazDto>?> KisininTasinmazlariniGetirAsync(int kisiId, string jwt);
    Task<TasinmazDto?> TasinmazDetayGetirAsync(int id, string jwt);
}

public class ApiIstemcisi : IApiIstemcisi
{
    private readonly HttpClient _httpClient;

    public ApiIstemcisi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponseDto?> GirisYapAsync(string kullaniciAdiVeyaEposta, string sifre)
    {
        var yanit = await _httpClient.PostAsJsonAsync("api/auth/login",
            new LoginRequestDto { KullaniciAdiVeyaEposta = kullaniciAdiVeyaEposta, Sifre = sifre });

        if (!yanit.IsSuccessStatusCode) return null;

        var sarmalanmis = await yanit.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        return sarmalanmis?.Success == true ? sarmalanmis.Data : null;
    }

    public async Task<PagedResult<KisiDto>?> KisileriListeleAsync(string jwt, int sayfa = 1, string? arama = null)
    {
        var yol = $"api/kisi?sayfa={sayfa}" + (string.IsNullOrWhiteSpace(arama) ? "" : $"&arama={Uri.EscapeDataString(arama)}");
        return await GetAsync<PagedResult<KisiDto>>(yol, jwt);
    }

    public async Task<KisiDto?> KisiGetirAsync(int id, string jwt) =>
        await GetAsync<KisiDto>($"api/kisi/{id}", jwt);

    public async Task<PagedResult<TasinmazDto>?> KisininTasinmazlariniGetirAsync(int kisiId, string jwt) =>
        await GetAsync<PagedResult<TasinmazDto>>($"api/tasinmaz?kisiId={kisiId}&sayfaBoyutu=100", jwt);

    public async Task<TasinmazDto?> TasinmazDetayGetirAsync(int id, string jwt) =>
        await GetAsync<TasinmazDto>($"api/tasinmaz/{id}", jwt);

    private async Task<T?> GetAsync<T>(string yol, string jwt)
    {
        using var istek = new HttpRequestMessage(HttpMethod.Get, yol);
        istek.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var yanit = await _httpClient.SendAsync(istek);
        if (!yanit.IsSuccessStatusCode) return default;

        var sarmalanmis = await yanit.Content.ReadFromJsonAsync<ApiResponse<T>>();
        return sarmalanmis?.Success == true ? sarmalanmis.Data : default;
    }
}
