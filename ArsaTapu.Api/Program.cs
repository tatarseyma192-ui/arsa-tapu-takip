using System.Text;
using ArsaTapu.Api.Authorization;
using ArsaTapu.Api.Filters;
using ArsaTapu.Api.Middleware;
using ArsaTapu.Api.Services;
using ArsaTapu.Business;
using ArsaTapu.Business.ParselSorgu;
using ArsaTapu.DataAccess.Context;
using ArsaTapu.DataAccess.Identity;
using ArsaTapu.Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- DbContext (PostgreSQL) ---
// Bağlantı dizesi appsettings.json'da KASITLI OLARAK BOŞ (Handbook madde 9). Development'ta
// `dotnet user-secrets set "ConnectionStrings:VarsayilanBaglanti" "..."`, production'da ortam
// değişkeni (ConnectionStrings__VarsayilanBaglanti) ile sağlanmalıdır. Bkz. README.md.
var baglantiDizesi = builder.Configuration.GetConnectionString("VarsayilanBaglanti");
if (string.IsNullOrWhiteSpace(baglantiDizesi))
{
    throw new InvalidOperationException(
        "ConnectionStrings:VarsayilanBaglanti tanımlı değil. Development'ta user-secrets, " +
        "production'da ortam değişkeni ile sağlayın. Bkz. README.md.");
}

builder.Services.AddDbContext<ArsaTapuDbContext>(options => options.UseNpgsql(baglantiDizesi));

// --- Identity (kullanıcı/rol yönetimi) ---
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ArsaTapuDbContext>()
    .AddDefaultTokenProviders();

// --- Giriş yapan kullanıcı bilgisi (audit + yetki kapsamı için) ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// --- JWT Authentication ---
// Jwt:Key ve ConnectionStrings:VarsayilanBaglanti appsettings.json'da KASITLI OLARAK BOŞ
// bırakılmıştır (Handbook madde 9 — hassas bilgiler düz metin saklanmaz). Development'ta
// `dotnet user-secrets set`, production'da ortam değişkeni/secret manager ile sağlanmalıdır.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key tanımlı değil. Development'ta 'dotnet user-secrets set \"Jwt:Key\" \"...\"' ile, " +
        "production'da ortam değişkeni (Jwt__Key) ile sağlayın. Bkz. README.md.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// --- Rol bazlı policy'ler (Requirements madde 1) ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyIsimleri.YonetimVePersonel, p => p.RequireRole(Roller.Admin, Roller.Personel));
    options.AddPolicy(PolicyIsimleri.SadeceYonetim, p => p.RequireRole(Roller.Admin));
});

// --- Business + DataAccess DI (repository/UoW/servis/validator kayıtları) ---
builder.Services.BusinessServisleriniEkle();

// --- Parsel Sorgu (TKGM) istemcisi — Handbook madde 4: izole entegrasyon.
// Taban URL/timeout appsettings'ten; TKGM site yapısı değişirse yalnızca
// TkgmParselSorguIstemcisi (ve gerekirse buradaki config) güncellenir. ---
// Gerçek uç nokta ve gerekli header'lar 2026-08-04 tarihli HAR kaydından doğrulandı:
// Referer zorunlu, kimlik doğrulama/cookie/API key GEREKMİYOR, yanıtlar gzip sıkıştırılmış.
builder.Services.AddHttpClient<IParselSorguIstemcisi, TkgmParselSorguIstemcisi>(client =>
{
    var tabanUrl = builder.Configuration["TkgmParselSorgu:TabanUrl"]
        ?? "https://cbsapi.tkgm.gov.tr/megsiswebapi.v3.1/api";
    // KRİTİK: BaseAddress mutlaka "/" ile bitmeli. Aksi halde .NET'in Uri birleştirme kuralı
    // (relative path'te başta "/" YOKSA base'in path'ine EKLENİR, VARSA base'in tüm path'i
    // SİLİNİP relative path'in kendisiyle DEĞİŞTİRİLİR) yüzünden istemcideki göreli yollar
    // (baştan "/" olmadan yazılan "idariYapi/...", "parsel/...") yanlış birleşip
    // "/megsiswebapi.v3.1/api" öneki tamamen kaybolurdu.
    if (!tabanUrl.EndsWith('/')) tabanUrl += "/";
    client.BaseAddress = new Uri(tabanUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "ArsaTapuTakip/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Referrer = new Uri(
        builder.Configuration["TkgmParselSorgu:RefererUrl"] ?? "https://parselsorgu.tkgm.gov.tr/");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// --- Controllers + otomatik doğrulama filtresi ---
// Not: FluentValidation validator kayıtları BusinessServisleriniEkle() içinde yapılır, burada tekrarlanmaz.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<DogrulamaFiltresi>();
});

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Arsa / Tapu Takip API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- CORS (frontend dev sunucusu) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Development'ta HTTPS'e yönlendirme BİLEREK atlanır — aksi halde ArsaTapu.Web'in bu API'ye
// yaptığı http://localhost:5000 isteği https'e yönlendirilir ve orada güvenilir olmayan
// geliştirme sertifikası (dotnet dev-certs https --trust) sorunu çıkarabilir. Üretimde (reverse
// proxy/hosting ortamı) bu genelde zaten proxy seviyesinde ele alınır.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- Başlangıç: rolleri ve ilk Admin kullanıcısını oluştur ---
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var rol in Roller.Tumu)
    {
        if (!await roleManager.RoleExistsAsync(rol))
            await roleManager.CreateAsync(new IdentityRole(rol));
    }

    // İlk Admin kullanıcısı yalnızca appsettings'te InitialAdmin tanımlıysa ve
    // sistemde hiç Admin yoksa oluşturulur. Üretimde InitialAdmin:Sifre ortam
    // değişkeni/user-secrets ile verilmeli, appsettings dosyasına yazılmamalıdır.
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEposta = app.Configuration["InitialAdmin:Eposta"];
    var adminSifre = app.Configuration["InitialAdmin:Sifre"];

    if (!string.IsNullOrWhiteSpace(adminEposta) && !string.IsNullOrWhiteSpace(adminSifre))
    {
        var adminVarMi = (await userManager.GetUsersInRoleAsync(Roller.Admin)).Any();
        if (!adminVarMi)
        {
            var adminKullanici = new ApplicationUser
            {
                UserName = adminEposta,
                Email = adminEposta,
                AdSoyad = "Sistem Yöneticisi",
                EmailConfirmed = true
            };

            var olusturmaSonucu = await userManager.CreateAsync(adminKullanici, adminSifre);
            if (olusturmaSonucu.Succeeded)
            {
                await userManager.AddToRoleAsync(adminKullanici, Roller.Admin);
            }
        }
    }
}

app.Run();
