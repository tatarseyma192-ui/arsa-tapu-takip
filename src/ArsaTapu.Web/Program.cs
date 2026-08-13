using ArsaTapu.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// ArsaTapu.Api'nin adresi appsettings'ten okunur — API ve Web AYRI iki .NET uygulamasıdır
// (Handbook madde 3 gereği Web, Business/DataAccess'e değil, yalnızca API'ye HTTP ile konuşur).
var apiTabanUrl = builder.Configuration["ArsaTapuApi:TabanUrl"]
    ?? throw new InvalidOperationException(
        "ArsaTapuApi:TabanUrl tanımlı değil. appsettings.Development.json içinde " +
        "ArsaTapu.Api'nin çalıştığı adresi belirtin (örn. \"https://localhost:5001/\").");

builder.Services.AddHttpClient<IApiIstemcisi, ApiIstemcisi>(client =>
{
    client.BaseAddress = new Uri(apiTabanUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Web uygulaması KENDİ tarayıcı oturumunu çerezle (cookie) yönetir; API'den alınan JWT bu
// çerezin İÇİNDE bir claim olarak taşınır (BFF — Backend for Frontend — deseni). Böylece
// kullanıcı adına her sayfa isteğinde JWT'yi yeniden göndermesine gerek kalmaz.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Giris";
        options.LogoutPath = "/Cikis";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true; // JavaScript erişemez — XSS'e karşı standart önlem
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Hata");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
