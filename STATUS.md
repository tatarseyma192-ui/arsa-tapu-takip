# Durum Raporu — Arsa/Tapu Takip Sistemi

Son güncelleme: 2026-08-04

Bu belge, hangi modülün **gerçek veriyle doğrulandığını**, hangisinin **yalnızca statik
incelendiğini/sentetik testlerle çalıştığını** ve hangisinin **hiç çalıştırılmadığını** net
şekilde ayırt etmek için tutulur. Amaç: ileride biri gerçek `dotnet build` aldığında veya bir
hata araştırırken nereye daha dikkatli bakması gerektiğini bilmesi.

**Bu belge her yeni kod turunda güncellenir** — yeni bir modül eklendiğinde veya bir modülün
doğrulama durumu değiştiğinde (ör. sentetik testten gerçek veri doğrulamasına geçtiğinde) ilgili
satır güncellenir, yeni riskler varsa "Bilinen Açık Riskler" bölümüne eklenir.

## Modül Durumu

| Modül | Kod Durumu | Test/Doğrulama Durumu |
|---|---|---|
| Domain / Dto katmanları | Tamam | Statik incelendi (basit POCO'lar, düşük risk) |
| Kişi CRUD | Tamam | Birim testi yok ama basit CRUD; statik incelendi |
| Taşınmaz CRUD | Tamam | Statik incelendi |
| JWT + rol bazlı yetkilendirme | Tamam | Statik incelendi. **Gerçek login/token akışı hiç çalıştırılmadı** |
| **Kullanıcı oluşturma/listeleme** (`KullaniciController`) | Tamam | **Bu turda eklendi** — daha önce yalnızca ilk Admin seed'i ve login/refresh vardı, Admin/Personel/Patron hesabı oluşturacak bir uç HİÇ YOKTU (kullanıcı sorusuyla fark edildi, gerçek bir eksikti). Doğrulama kuralı (Patron rolünde KisiId zorunlu) birim testli; UserManager/RoleManager akışı statik incelendi, gerçek çalıştırılmadı |
| Ortaklık / Komşuluk hesaplama | Tamam | Senaryo testleriyle doğrulandı (3 senaryo: saf ortaklık/karışık/saf komşuluk), InMemory DB |
| Taşınmaz detayında ortaklık/komşuluk göstergesi | Tamam | **Bu turda eklendi** — `TasinmazDto.OrtakKisiler`/`KomsuKisiler`, yalnızca tekil kayıt (`GetirAsync`) getirilirken hesaplanır (listelemede performans için atlanır). 2 birim testle doğrulandı |
| Mülkiyet / KML tekilleştirme servisleri | Tamam | Birim testli |
| **PDF parse (WebTapu)** | Tamam | ✅ **GERÇEK WebTapu PDF örneğiyle doğrulandı** (2026-08-04) — 30/30 kayıt doğru. Filigran karışması ve çok satırlı Nitelik/İlçe sorunları bulunup düzeltildi |
| **Excel parse / üretim** | Tamam | ✅ **GERÇEK bir Excel örneğiyle doğrulandı** (2026-08-04) — 19/19 satır hatasız parse edildi, sütun sırası/eşleştirme, boş hücre ("-" literal), karışık sayı formatı (metin+native) ve karşılaştırma motoru (4 Zaten Kayıtlı / 15 Yeni Alım, PDF'teki örtüşen kayıtlarla mantıken tutarlı) doğrulandı. Bu süreçte **Taşınmaz No'nun bazı gerçek Excel formatlarında hiç bulunmadığı** ortaya çıktı — mülkiyet tekilleştirme anahtarı TasinmazNo+BagimsizBolumNo+ZeminHisseId'den **BagimsizBolumNo+ZeminHisseId**'e indirgendi (TasinmazNo artık opsiyonel/yalnızca referans alanı) |
| Karşılaştırma motoru (Yeni Alım/Satıldı/YuklemeKaydi) | Tamam | Birim testli (InMemory DB) + gerçek Excel/PDF çapraz verisiyle mantık doğrulandı — **gerçek PostgreSQL'e hiç uygulanmadı**. Kullanıcı isteği üzerine **kısmi yükleme desteği** eklendi: `TamPortfoyMu` (varsayılan false) — false iken "Satıldı" karşılaştırması yalnızca dosyada geçen il/ilçe kombinasyonlarıyla sınırlı, dosyada hiç bahsi geçmeyen il/ilçelerdeki mevcut taşınmazlara dokunulmaz. Birim testlerle doğrulandı (kısmi ve tam portföy senaryoları ayrı ayrı) |
| **TKGM / Parsel Sorgu istemcisi** | Tamam | ✅ Gerçek HAR (network trafiği) kaydından çıkarılan doğrulanmış uç noktalara göre yazıldı; sahte HTTP handler ile test edildi. **Gerçek TKGM sunucusuna hiç bağlanılmadı** — sunucunun rate-limit/bot koruması olup olmadığı bilinmiyor |
| Manuel KML yükleme | Tamam | Statik incelendi + birim testli (TKGM'ye bağımlı değil, düşük risk) |
| Deneysel/Kaynak (Otomatik/Manuel) etiketleme | Tamam | Birim testli |
| KML toplu sorgu — "hepsi / belirli" seçimi | Tamam | Kullanıcı isteği üzerine eklendi: `TumunuSecModu=true` + `KisiId` ile kişinin KML'si eksik TÜM aktif parselleri otomatik bulunup sorgulanır; `false` (varsayılan) ile yalnızca elle seçilen liste kullanılır. Birim testli (satılmış taşınmazların kapsam dışı kaldığı da doğrulandı) |
| **ArsaTapu.Web (yeni ASP.NET Core Razor Pages arayüzü)** | 🟡 Statik incelendi, hiç çalıştırılmadı | Bu turda eklendi — Giriş, Kişi listesi (arama), Kişi profili (portföy tablosu), Taşınmaz detayı (**ortaklık/komşuluk gösterimi dahil**) çalışır durumda yazıldı. `ArsaTapu.Api`'ye yalnızca HTTP ile bağlanıyor (Handbook madde 3'e uygun — Business/DataAccess'e referansı YOK). Yükleme/KML/Ortaklık listesi/Kullanıcı Yönetimi ekranları henüz YOK (React mockup'ta var, Web'de değil) |
| ConnectionString / Jwt:Key | 🟡 Değer üretildi, gerçek bağlantı hiç denenmedi | `appsettings.Development.json`'a gerçek (rastgele üretilmiş) bir Jwt anahtarı ve varsayılan yerel PostgreSQL bağlantı dizesi (`Host=localhost;...;Username=postgres;Password=postgres`) yazıldı — farklı bir PostgreSQL kurulumunuz varsa Host/Username/Password güncellenmeli |

## Bu Turda Yapılan Çapraz Kontrol (2026-08-04)

Tüm kod tabanına geriye dönük uygulandı:

- **Namespace/using kontrolü**: 38 tanımlı namespace çıkarıldı, tüm `using ArsaTapu.*`
  ifadeleri gerçek namespace'lere karşılık geldiği doğrulandı.
- **Interface/implementasyon kontrolü**: 24 interface → implementasyon eşleştirmesi yapıldı,
  hepsi tam. (Otomatik script `IRepository<T>` ve `ITkgmIdCache`'i generic/tuple imza yüzünden
  yanlış pozitif işaretledi; elle açılıp doğrulandı, gerçek eksik yok.)
- **DI kaydı kontrolü**: Constructor'larda kullanılan 22 farklı interface, `BusinessServisleriniEkle()`
  ve `Program.cs`'teki kayıtlarla karşılaştırıldı — hepsi kayıtlı.
- **Paket sürümü kontrolü**: 21 paket, tüm .csproj'lar arasında sürüm çakışması için tarandı —
  çakışma yok. Tüm projeler net9.0 hedefliyor.
- **Bulunan ve düzeltilen 1 gerçek eksik**: `ArsaTapu.Business/DependencyInjection.cs`
  `IServiceCollection`/`AddScoped` kullanıyordu ama bu yalnızca EF Core/FluentValidation
  paketleri üzerinden dolaylı (transitive) geliyordu — `Microsoft.Extensions.DependencyInjection.Abstractions`
  paketi Business.csproj'a açıkça eklendi.
- **Katmanlı mimari bütünlüğü (bağımsız olarak tekrar doğrulandı)**: Domain ve Dto katmanlarının
  hiçbir `ArsaTapu.*` dış bağımlılığı yok; DataAccess yalnızca Domain'e bağımlı; Business yalnızca
  Domain+Dto+DataAccess'e bağımlı (Api'ye ASLA) — hem dosyalardaki `using` ifadeleri hem 6
  `.csproj`'un `ProjectReference` grafiği birebir tutarlı bulundu. İhlal yok.

**Bu kontrollerin sınırı**: Yanlış namespace, eksik DI kaydı, eksik implementasyon ve katman
ihlali gibi "derlenmeyecek türden" hataların çoğunu yakalar — ama şunları YAKALAYAMAZ: yanlış
parametre TİPİ eşleşmesi (isim doğru, tip yanlış), EF Core'un bir LINQ ifadesini SQL'e
çeviremediği durumlar (yalnızca çalışma zamanında ortaya çıkar), NuGet paket sürümünün gerçekten
var olup olmadığı. Bunlar için gerçek `dotnet build`/`dotnet test` şart.

## Bilinen Açık Riskler

1. **`dotnet restore` + `dotnet build` bu ortamda hiç çalıştırılamadı** (internet erişimi yok,
   dotnet SDK kurulu değil). Kod tabanı yalnızca statik/manuel incelemeyle (yukarıdaki çapraz
   kontrol dahil) doğrulanabildi. Gerçek derleme kullanıcının kendi ortamında yapılacak.
2. **Migration'lar hiç gerçek bir PostgreSQL'e uygulanmadı.** Şema, index'ler (özellikle
   madde 4.3 için eklenen filtreli/partial unique index'ler) ve soft-delete davranışı yalnızca
   EF Core InMemory sağlayıcısıyla test edildi — InMemory çoğu ilişkisel kısıtı (unique index,
   check constraint) FİİLEN ZORLAMAZ, bu yüzden testler mantığı doğrular ama DB seviyesindeki
   kısıtların gerçekten çalıştığını KANITLAMAZ.
3. **TKGM sunucusuna (`cbsapi.tkgm.gov.tr`) hiç gerçek istek atılmadı.** Kod, kullanıcının
   sağladığı gerçek HAR kaydından çıkarılan uç nokta/şema bilgisine göre yazıldı ve sahte bir
   HTTP handler ile test edildi, ama gerçek sunucu şunları içerebilir: rate-limit/bot koruması
   (HAR'da görülmedi ama tek oturumla kesin değil), parsel bulunamadığında farklı bir yanıt
   şekli, veya zamanla değişmiş bir API sürümü.
4. **Paket sürümleri (`UglyToad.PdfPig` 0.1.8, `ClosedXML` 0.102.2) NuGet.org'a karşı
   doğrulanamadı** — bu ortamda NuGet erişimi yok. `dotnet restore` bu sürümleri bulamazsa
   güncel kararlı sürümle değiştirilmeli.
5. **Gerçek Excel örneği artık doğrulandı** (bkz. yukarıdaki modül tablosu) — bu maddeye artık
   gerek yok, ama doğrulama sürecinde bulunup düzeltilen gerçek hatalar not düşülüyor:
   - Boş Bağımsız Bölüm No, boş hücre yerine literal `"-"` karakteriyle işaretlenmişti — eski kod
     bunu sayıya çevirmeye çalışıp hatalı reddediyordu.
   - Sayısal hücreler (Ada/Parsel/Yüzölçüm) Excel'de bazen native sayı bazen metin geliyordu;
     eski `TamSayiyaCevir` yalnızca rakamları koruyup birleştiriyordu — "182.0" gibi bir değeri
     sessizce **1820**'ye çevirirdi. Artık önce tam sayı, olmazsa kesirsiz ondalık olarak
     doğru ayrıştırılıyor.
   - `Ada=0` gerçek veride geçerli bir değer (yol/tarla parselleri) ama `TasinmazCreateDtoValidator`/
     `TasinmazUpdateDtoValidator` `GreaterThan(0)` istiyordu — `GreaterThanOrEqualTo(0)` yapıldı.
   - **En büyük bulgu**: gerçek Excel'de Taşınmaz No sütunu HİÇ YOKTU. Mülkiyet tekilleştirme
     anahtarı bu yüzden **BagimsizBolumNo+ZeminHisseId**'e indirgendi (TasinmazNo artık anahtarın
     parçası değil) — bu, `Tasinmaz` entity'si, EF Core unique index'i, repository/tekilleştirme
     servisi imzaları ve ilgili DTO'lar dahil birden fazla dosyayı etkileyen kök bir değişiklikti.
     Gerçek PDF+Excel çapraz verisiyle test edildi: aynı gerçek taşınmaz (Zemin Hisse ID eşleşmesi
     ile doğrulandı) PDF'te TasinmazNo'lu, Excel'de TasinmazNo'suz göründüğünde doğru şekilde
     "Zaten Kayıtlı" olarak tanınıyor.
6. **JWT login/refresh akışı ve ilk Admin seed'i hiç gerçek çalıştırılmadı** — yalnızca kod
   seviyesinde incelendi.
7. **`ArsaTapu.Web` (yeni ASP.NET Core Razor Pages arayüzü) hiç çalıştırılmadı.** Bu turda
   eklenen ~20 yeni dosyalık, tek seferde yazılan bir proje — kimlik doğrulama (çerezde JWT
   taşıma), API istemcisi (HttpClient) ve 4 sayfa (Giriş/Kişi listesi/Kişi profili/Taşınmaz
   detayı) içeriyor. Elle dikkatli kontrol edildi (parantez/namespace/using) ama bu ölçekte
   yeni bir proje, gerçek `dotnet build` görmeden yüksek risk taşır. Ayrıca yalnızca temel
   ekranlar var — Yükleme/KML/Ortaklık listesi/Kullanıcı Yönetimi ekranları henüz yazılmadı.
