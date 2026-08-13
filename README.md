# Arsa / Tapu Takip Sistemi — Backend

Katmanlı mimari (Technical Defaults madde 2): `Domain` → `DataAccess` → `Business` → `Api`, `Dto` bağımsız.

**Önce [`STATUS.md`](./STATUS.md)'ye bakın** — hangi modülün gerçek veriyle doğrulandığı, hangisinin
yalnızca statik incelendiği ve bilinen açık riskler orada net şekilde listelidir.

## Kurulum

```bash
cd ArsaTapuTakip
dotnet restore
```

### Güvenlik ilkesi (Handbook madde 9)

`appsettings.json` içinde bağlantı dizesi, JWT anahtarı ve InitialAdmin **KASITLI OLARAK BOŞ**
bırakılmıştır ve boşsa uygulama açılışta anlamlı bir hata ile durur (bkz. `Program.cs`). Bu
değerler hiçbir ortamda appsettings dosyalarına gerçek değerle yazılmaz.

**Development (yerel geliştirme) — `dotnet user-secrets` kullanılır:**

```bash
dotnet user-secrets init --project src/ArsaTapu.Api
dotnet user-secrets set "ConnectionStrings:VarsayilanBaglanti" "Host=localhost;Port=5432;Database=arsatapu;Username=...;Password=..." --project src/ArsaTapu.Api
dotnet user-secrets set "Jwt:Key" "en-az-32-karakterlik-gizli-anahtar" --project src/ArsaTapu.Api
# İsteğe bağlı: ilk Admin kullanıcısını da user-secrets ile verebilirsiniz (appsettings.Development.json yerine)
dotnet user-secrets set "InitialAdmin:Eposta" "admin@ornek.local" --project src/ArsaTapu.Api
dotnet user-secrets set "InitialAdmin:Sifre" "GucluBirSifre123!" --project src/ArsaTapu.Api
```

`user-secrets` değerleri proje klasörü dışında (`%APPDATA%\Microsoft\UserSecrets` veya
`~/.microsoft/usersecrets`) saklanır, repoya asla girmez — `dotnet user-secrets` bunun için
vardır. `appsettings.Development.json` içine de yerel/sahte bir InitialAdmin bırakılabilir
(hızlı başlangıç için), ama bu dosya **`.gitignore`'dadır** ve gerçek/paylaşılan bir ortamda
asla kullanılmamalıdır.

**Production — ortam değişkenleri (veya bir secret manager: Azure Key Vault, AWS Secrets
Manager, Docker/Kubernetes secrets vb.) kullanılır, `dotnet user-secrets` DEĞİL:**

`user-secrets` yalnızca yerel geliştirme için tasarlanmıştır; production'da .NET'in çift alt
çizgi (`__`) konvansiyonuyla iç içe appsettings anahtarları ortam değişkenine çevrilir:

```bash
export ConnectionStrings__VarsayilanBaglanti="Host=...;Database=...;Username=...;Password=..."
export Jwt__Key="uretimde-kullanilacak-farkli-ve-guclu-bir-anahtar"
# InitialAdmin production'da genelde BOŞ bırakılır; ilk Admin ayrı, tek seferlik bir
# yönetimsel adımla (ör. veritabanına elle ekleme veya ayrı bir seed script'i) oluşturulur.
```

Container/orkestrasyon ortamında (Docker, Kubernetes) bu değerler secret/ConfigMap olarak
enjekte edilir; appsettings dosyaları imaja gerçek değerle asla gömülmez.

### Migration + çalıştırma

```bash
dotnet ef migrations add InitialCreate --project src/ArsaTapu.DataAccess --startup-project src/ArsaTapu.Api
dotnet ef database update --project src/ArsaTapu.DataAccess --startup-project src/ArsaTapu.Api

dotnet run --project src/ArsaTapu.Api
```

## Web arayüzü (ArsaTapu.Web) — ASP.NET Core Razor Pages

Kullanıcı isteği üzerine React yerine/yanına eklendi (bu turda). **React mockup'ın yerini
almaz** — ayrı, gerçek API'ye bağlı çalışan bir .NET tabanlı arayüzdür; Node.js/npm gerekmez.

**Mimari (Handbook madde 3'e uygun):** `ArsaTapu.Web` yalnızca `ArsaTapu.Dto`'ya referans verir,
Business/DataAccess'e ASLA doğrudan erişmez — tüm veri `ArsaTapu.Api`'ye HTTP ile (yeni
`IApiIstemcisi`) gidilerek alınır, React'ın yapacağı işin aynısı. Giriş sonrası JWT, kullanıcının
tarayıcı çerezinin İÇİNDE bir claim olarak taşınır (BFF deseni) — her sayfa isteğinde yeniden
giriş gerekmez.

**Çalıştırma — İKİ ayrı terminal gerekir (API + Web ayrı süreçlerdir):**
```bash
# Terminal 1 — API (http://localhost:5000)
dotnet run --project src/ArsaTapu.Api

# Terminal 2 — Web arayüzü (http://localhost:5010)
dotnet run --project src/ArsaTapu.Web
```
Tarayıcıda `http://localhost:5010` açın, `InitialAdmin` bilgileriyle giriş yapın.

**Şu ana kadar hazır ekranlar:** Giriş, Kişi listesi (arama dahil), Kişi profili (portföy tablosu),
Taşınmaz detayı (**ortaklık/komşuluk gösterimi dahil** — bu turda backend'e de eklendi, bkz.
`TasinmazDto.OrtakKisiler`/`KomsuKisiler`).

**Henüz eklenmeyenler** (kapsam netliği için açıkça belirtiliyor): Yükleme (PDF/Excel önizleme/
onay) akışı, KML yönetimi, Ortaklık/Komşuluk listesi ekranı, Kullanıcı Yönetimi ekranı. Bunlar
React mockup'ta var, Web'de henüz yok — mimari/kimlik doğrulama iskeleti (en riskli kısım) kuruldu,
kalan ekranlar aynı deseni (IApiIstemcisi + Razor Page) tekrarlayarak eklenecek.

**Geliştirme notu:** Hem Api hem Web, Development ortamında birbirine düz HTTP (localhost) ile
konuşacak şekilde ayarlandı — `dotnet dev-certs https --trust` adımına GEREK YOK.


### İlk kullanım akışı — kullanıcı oluşturma

1. `InitialAdmin` ile açılışta oluşan **tek** Admin hesabıyla `POST /api/auth/login` yapın
   (`KullaniciAdiVeyaEposta` = InitialAdmin:Eposta, `Sifre` = InitialAdmin:Sifre).
2. Dönen JWT ile artık **Admin** olarak diğer tüm işlemleri yapabilirsiniz. Yeni bir
   Personel/Patron/Admin hesabı eklemek için `POST /api/kullanici`:
   ```json
   { "eposta": "personel1@ornek.local", "sifre": "GucluBirSifre123!",
     "adSoyad": "Ayşe Yılmaz", "rol": "Personel" }
   ```
   `rol: "Patron"` ise ayrıca `kisiId` göndermelisiniz — hesabı hangi Kişi'ye (zaten `POST /api/kisi`
   ile oluşturulmuş olmalı) bağlayacağınızı belirtir; Patron girişinde `GET /api/kisi/me` bu
   bağlantı üzerinden çalışır.
3. `GET /api/kullanici` ile mevcut tüm hesapları/rollerini görebilirsiniz.

**Not:** Bu üç uç (`Olustur`/`Listele`/`Getir`) bu turda eklendi — daha önce yalnızca ilk Admin
seed'i ve login/refresh vardı, başka hesap oluşturmanın bir yolu yoktu.

## Testler

```bash
dotnet test tests/ArsaTapu.Tests
```

Kapsam: Gerçek ortaklık / komşuluk ayrımı; mülkiyet / KML tekilleştirme anahtarlarının birbirine
karışmadığı; PDF tablo yeniden oluşturma algoritması (satır kümeleme, sütun sınırı tespiti, boş
Bağımsız Bölüm No'nun sütun kaymasına yol açmadığı); satır -> DTO dönüşümünde Türkçe/İngilizce
sayı biçimleri; karşılaştırma motorunun iki ardışık yükleme arasında Yeni Alım/Satıldı/Zaten
Kayıtlı ayrımını doğru yaptığı ve YuklemeKaydi + KML tetikleme listesini doğru ürettiği;
Parsel Sorgu modülünde başarılı/başarısız sorgu, "tekrar dene"nin var olan kaydı güncellediği
(yeni satır oluşturmadığı), silme sonrası aynı parselin yeniden sorgulanabildiği, toplu sorguda
zaten çekilmiş parsellerin atlandığı ve manuel yüklemenin TKGM istemcisine hiç gitmediği;
`TkgmParselSorguIstemcisi`'nin il/ilçe/mahalle çözümlemesinin (sahte HTTP handler ile, gerçek HAR
şemasını taklit ederek) doğru çalıştığı, ilçe/mahalle listelerinin önbelleklendiği (aynı il/ilçe
için tekrar çekilmediği) ve BaseAddress+göreli-yol birleştirmesinin taban path'i düşürmediği
(bu, ilk yazımda bulunup düzeltilen gerçek bir hataydı — regresyon testiyle korunuyor).

## CI/CD — GitHub Actions ile gerçek derleme doğrulaması

Bu ortamda internet erişimi olmadığı için `dotnet build`/`dotnet restore` hiç gerçek şekilde
çalıştırılamadı — bunun yerine `.github/workflows/build.yml` eklendi, gerçek doğrulama GitHub
Actions üzerinde (repoyu push ettiğinizde otomatik) yapılır:

- **`build-test` job'ı**: checkout → .NET 9 kur → `dotnet restore` → `dotnet build --no-restore`
  → `dotnet test --no-build`.
- **`migration-test` job'ı** (`build-test` başarılı olursa çalışır): gerçek bir PostgreSQL 16
  container'ı (`services:`) ayağa kaldırır, ilk migration yoksa oluşturur
  (`dotnet ef migrations add InitialCreate`), ardından gerçek veritabanına uygular
  (`dotnet ef database update`). Oluşan migration dosyaları iş akışı sonunda "Artifacts"
  altında indirilebilir hale gelir — ilk başarılı çalıştırmadan sonra bunları indirip
  `src/ArsaTapu.DataAccess/Migrations/` altına commit etmeniz önerilir (migration dosyaları
  normalde kaynak kontrolünde tutulur, her CI çalışmasında yeniden üretilmez).

Migration job'ının appsettings/user-secrets'a ihtiyaç duymadan çalışabilmesi için
`ArsaTapuDbContextFactory.cs` (Api projesinde, `IDesignTimeDbContextFactory<ArsaTapuDbContext>`)
eklendi — yalnızca `ConnectionStrings__VarsayilanBaglanti` ortam değişkenini okur, Program.cs'in
tam host kurulumunu (Jwt:Key kontrolü dahil) atlar. Bu, EF Core'un CI/CD senaryoları için
önerdiği standart yaklaşımdır.

**Push etmek için:**
```bash
# 1) github.com'da yeni, BOŞ bir repo oluşturun (README eklemeden)
# 2) İndirdiğiniz zip'i açıp içindeki ArsaTapuTakip klasörüne girin
cd ArsaTapuTakip
git init
git add .
git commit -m "ilk yukleme"
git branch -M main
git remote add origin https://github.com/KULLANICI_ADINIZ/REPO_ADI.git
git push -u origin main
```
Push sonrası repo sayfasındaki **Actions** sekmesinde iş akışı otomatik başlar.

## Bu adımda yapılanlar

1. **Şema**: Kisi, YuklemeKaydi, Tasinmaz, ParselKml — hepsi audit alanları (CreatedAt/By,
   UpdatedAt/By, IsDeleted) + soft delete ile (Handbook madde 5).
2. **Tekilleştirme**: `IMulkiyetTekillestirmeService` (TasinmazNo+BagimsizBolumNo+ZeminHisseId)
   ve `IKmlTekillestirmeService` (Il+Ilce+Mahalle+Ada+Parsel) — ayrı servisler, ayrı anahtarlar.
3. **Ortaklık/Komşuluk**: `GET /api/ortaklik/gercek` ve `GET /api/ortaklik/komsuluk` — hesaplama
   tamamen Business katmanında, frontend mockup'taki mantığın birebir aynısı.
4. **Response formatı**: `ApiResponse<T>` zarfı (`success/data/message/errors`), teknik hata
   detayları `ExceptionHandlingMiddleware` tarafından loglanır, kullanıcıya asla sızdırılmaz.
5. **Yetkilendirme**: JWT + rol policy'leri (`YonetimVePersonel`, `SadeceYonetim`) controller
   seviyesinde; kayıt bazlı kapsam (Patron → yalnızca kendi KisiId'si, Personel → yalnızca kendi
   yüklediği kayıtları silebilir) `IYetkiKapsamService` üzerinden Business katmanında merkezi.

## Endpoint özeti

| Yöntem | Yol | Yetki |
|---|---|---|
| POST | /api/auth/login | Anonim |
| POST | /api/auth/refresh | Anonim |
| POST | /api/kullanici | **Yalnızca Admin** — yeni Admin/Personel/Patron hesabı oluşturur |
| GET | /api/kullanici | Yalnızca Admin — tüm hesapları listeler |
| GET | /api/kullanici/{id} | Yalnızca Admin |
| GET | /api/kisi | Admin, Personel |
| GET | /api/kisi/me | Patron (kendi profili) |
| GET | /api/kisi/{id} | Herkes (kapsam Business'ta) |
| POST/PUT | /api/kisi(/{id}) | Admin, Personel |
| DELETE | /api/kisi/{id} | Admin |
| GET | /api/tasinmaz | Herkes (Patron kendi verisiyle sınırlı) |
| POST/PUT | /api/tasinmaz(/{id}) | Admin, Personel |
| DELETE | /api/tasinmaz/{id} | Admin; Personel yalnızca kendi yüklediği |
| GET | /api/ortaklik/gercek | Herkes (Patron kendi payıyla sınırlı) |
| GET | /api/ortaklik/komsuluk | Herkes (Patron kendi payıyla sınırlı) |
| POST | /api/tasinmaz-yukleme/onizleme/pdf | Admin, Personel — **DB'ye yazmaz** |
| POST | /api/tasinmaz-yukleme/onizleme/excel | Admin, Personel — **DB'ye yazmaz** |
| POST | /api/tasinmaz-yukleme/onayla | Admin, Personel — karşılaştırma motorunu çalıştırır |
| POST | /api/tasinmaz-yukleme/excel-indir | Admin, Personel — satırları .xlsx olarak döner |
| GET | /api/parsel-kml | Admin, Personel — her kayıtta kaynak/deneysel bilgisi |
| POST | /api/parsel-kml/manuel-yukle | Admin, Personel — **birincil/güvenilir** yol |
| POST | /api/parsel-kml/sorgula | Admin, Personel — ikincil yol, tekli sorgu/tekrar dene |
| POST | /api/parsel-kml/toplu-sorgula | Admin, Personel — ikincil yol, hız sınırlayıcı ile sırayla |
| DELETE | /api/parsel-kml/{id} | Admin, Personel — silinen parsel yeniden sorgulanabilir |

## PDF/Excel parse & karşılaştırma motoru (Requirements madde 2, 3, 4.1)

Akış: `onizleme/pdf` veya `onizleme/excel` dosyayı parse eder, **hiçbir DB yazımı yapmadan**
Yeni Alım / Zaten Kayıtlı sınıflandırmasıyla birlikte önizleme döner. Kullanıcı kontrol edip
`onayla` ucuna aynı satırları (istediği gibi düzenlemiş/filtrelemiş olarak) gönderir; bu uç:

- Satırları **sunucu tarafında yeniden sınıflandırır** (istemcinin önizlemedeki etiketine güvenilmez),
- Yeni taşınmazları `Durum=Aktif` olarak ekler,
- Kişinin önceki aktif taşınmazlarından bu yüklemede görülmeyenleri `Durum=Satildi` yapar
  (silmez — Requirements madde 3),
- Bir `YuklemeKaydi` oluşturur,
- KML çekme motoruna **dokunmadan** (Requirements madde 5 — izole/ayrı adım) hangi Ada/Parsel'lerin
  sorgulanması gerektiğinin listesini (`KmlSorgulanmasiGerekenParseller`) üretir.

**Kısmi yükleme desteği** (`TasinmazOnayIstegiDto.TamPortfoyMu`, kullanıcı isteği üzerine eklendi):
bazı yüklemeler kişinin TÜM portföyü değil, yalnızca belirli bir il/ilçe içindir (ör. "sadece
Gaziantep/Şahinbey kayıtları"). `TamPortfoyMu=false` (**varsayılan**, daha güvenli taraf): "Satıldı"
karşılaştırması yalnızca dosyada GEÇEN il/ilçe kombinasyonlarıyla sınırlanır — dosyada hiç bahsi
geçmeyen il/ilçelerdeki mevcut aktif taşınmazlara HİÇ DOKUNULMAZ. `TamPortfoyMu=true`: eski/tam
davranış — dosyada görünmeyen HER aktif taşınmaz (il/ilçe fark etmeksizin) "Satıldı" sayılır;
yalnızca kullanıcı kişinin GERÇEKTEN tam ve güncel portföyünü yüklediğini biliyorsa gönderilmelidir.
Yanıtta `DegerlendirilenIlIlceler` alanı, kısmi modda hangi il/ilçelerin kapsama alındığını gösterir.

Modül dosyaları: `src/ArsaTapu.Business/TasinmazYukleme/`. PDF'e özgü kod tek dosyada izole:
`PdfSatirCikarici.cs` (PdfPig'e bağımlı TEK sınıf) + `Pdf/TabloSatirOlusturucu.cs` (PdfPig'den
bağımsız, saf/test edilebilir satır-sütun algoritması). PDF yapısı değişirse yalnızca
`PdfSatirCikarici.cs` (ve gerekirse tolerans değerleri için `TabloSatirOlusturucu.cs`) güncellenir.

**Gerçek bir WebTapu PDF örneğiyle doğrulandı (2026-08-04):** 30/30 gerçek taşınmaz kaydı
(anahtar alanlar — Taşınmaz No/Ada/Parsel/Bağımsız Bölüm No/Zemin Hisse ID — %100 doğru) başarıyla
çıkarıldı. Bu doğrulama sırasında **iki gerçek sorun** bulunup düzeltildi (sentetik testlerde hiç
görünmeyen, yalnızca gerçek dosyada ortaya çıkan türden):

1. **Filigran karışması**: PDF'te büyük, döndürülmüş bir filigran ("BİLGİ AMAÇLIDIR", ~100+ punto)
   gerçek 12pt tablo metniyle bazen AYNI KELİMEYE kaynaşıyordu (ör. Yüzölçüm+filigran+Ada tek
   kelime oluyordu: "241.51Ç1093"). Kelime seviyesinde filtrelemeyle çözülemeyeceği için,
   `PdfSatirCikarici` artık PdfPig'in `GetWords()`'u yerine KARAKTER seviyesinde (`Page.Letters`)
   okuyor, yüksekliğine göre filigranı ayıklıyor, kelimeleri sıfırdan temiz karakterlerden kuruyor
   (`TabloSatirOlusturucu.HarflerdenKelimeOlustur`).
2. **Çok satırlı Nitelik/İlçe değerleri**: Uzun bir Nitelik ("1 KATLI OTO GALERİ 2 MESKENLİ KARGİR
   BİNA") veya İlçe adı ("Şehitkamil") birden fazla görsel satıra yayılabiliyor; yalnızca ilk
   satırda Taşınmaz No/Ada/Parsel/Zemin Hisse ID basılı oluyor. `TabloSatirOlusturucu.SatirParcalariniBirlestir`
   bu "devam parçalarını" bir önceki gerçek satıra ekliyor — sayfa altbilgisi gibi ilgisiz gürültüyle
   karışmaması için yalnızca Y-mesafesi normal satır aralığına yakın parçalar birleştiriliyor.
   Bu satır kaydırması yüzünden İlçe/Mahalle adlarına bazen fazladan boşluk girebildiğinden
   (ör. "Şehitkamil" -> "ŞEHİTKAMİ L"), `TkgmParselSorguIstemcisi`'nin isim eşleştirmesine de
   boşluk-toleranslı bir yedek adım eklendi.

Bu iki düzeltme `TabloSatirOlusturucuTests.cs`'e gerçek örnekten kalibre edilmiş (ölçülen boşluk/
yükseklik oranlarıyla) testler olarak eklendi. Sütun tespiti artık **doğrulanmış** kabul edilebilir;
yine de farklı bir WebTapu ihracat sürümü (farklı yazı boyutu, farklı filigran, vb.) küçük ayarlar
gerektirebilir.

**Paket sürümleri:** `UglyToad.PdfPig` (0.1.8) ve `ClosedXML` (0.102.2) sürüm numaraları bu ortamda
NuGet.org'a erişim olmadığı için doğrulanamadı. `dotnet restore` bu sürümleri bulamazsa en güncel
kararlı sürümle değiştirin; kullanılan API'ler (`PdfDocument.Open`, `Page.GetWords()`,
`Word.BoundingBox`, `XLWorkbook`) her iki kütüphanenin de uzun süredir stabil kalan çekirdek
yüzeyi olduğundan küçük sürüm farklarında değişmesi beklenmez.

## Parsel Sorgu (TKGM) modülü — GERÇEK API İLE DOĞRULANDI (Requirements madde 5, 4.2, 4.3)

TKGM entegrasyonu, 2026-08-04 tarihli gerçek network trafiğinden (HAR kaydı) çıkarılan
doğrulanmış uç noktalara göre çalışır:

```
Taban adres: https://cbsapi.tkgm.gov.tr/megsiswebapi.v3.1/api/
1. İlçe listesi : GET idariYapi/ilceListe/{ilId}
2. Mahalle listesi : GET idariYapi/mahalleListe/{ilceId}
3. Parsel doğrulama : GET parsel/{mahalleId}/{ada}/{parsel}
4. KML indirme : GET parsel/download/{mahalleId}/{ada}/{parsel}/kml
Header: Referer: https://parselsorgu.tkgm.gov.tr/ — kimlik doğrulama/cookie/API key YOK.
```

**İl/İlçe/Mahalle adı -> TKGM iç ID'si:** Bu API isim değil TKGM'nin kendi iç ID'lerini istiyor.
İl listesi (81 kayıt, 1999'dan beri sabit) `IlKodlari.cs`'te SABİT tabloda tutulur — ayrı bir
ağ çağrısı gerektirmez. İlçe/mahalle listeleri (binlerce kayıt, daha az stabil) gerçek
`idariYapi/ilceListe` / `idariYapi/mahalleListe` uç noktalarından çekilip **`ITkgmIdCache`
(Singleton) ile önbelleğe alınır** — aynı il/ilçe için tekrar sorgu yapıldığında liste tekrar
çekilmez, yalnızca önbellekteki isim eşleştirmesi kullanılır (kullanıcı isteği: "TKGM sitesine
gereksiz yük binmesin").

**KML kaynağı:** `parsel/download/.../kml` uç noktası doğrudan indirilebilir bir .kml dosyası
döndürüyor — bu dosya sıfırdan yeniden İNŞA EDİLMEZ, olduğu gibi kullanılır; `KmlOlusturucu`
yalnızca `XDocument` ile description alanına Taşınmaz No / Bağımsız Bölüm bilgisini EKLER
(madde 4.2). KML ayrıştırılamazsa (beklenmeyen format) orijinal dosya DEĞİŞTİRİLMEDEN kaydedilir
— asıl geometri hiçbir durumda bozulmaz.

**Bulunan ve düzeltilen kritik hata:** İlk yazımda göreli istek yolları baştan `/` ile
yazılmıştı (ör. `/idariYapi/ilceListe/49`). .NET'in `HttpClient.BaseAddress` birleştirme kuralı
gereği bu, taban adresin `megsiswebapi.v3.1/api` kısmını SESSİZCE düşürürdü. Düzeltildi: göreli
yollar baştaki `/` olmadan yazılır, `Program.cs` taban adresin sonuna her zaman `/` ekler; bu
davranış `TkgmParselSorguIstemcisiTests` içinde regresyon testiyle de korunuyor.

### 1) Manuel yükleme — BİRİNCİL / GÜVENİLİR yol

`POST /api/parsel-kml/manuel-yukle` — kullanıcı kendi indirdiği .kml dosyasını yükler. TKGM
istemcisine hiç gidilmez, kayıt doğrudan `Kaynak=Manuel`, `Durum=Basarili` işaretlenir, API
yanıtında her zaman `Deneysel=false` döner (uyarı yok).

### 2) Otomatik sorgu — İKİNCİL yol (artık deneysel etiketi VARSAYILAN KAPALI)

`POST /api/parsel-kml/sorgula` (tekli — aynı zamanda "Tekrar dene" için) veya
`POST /api/parsel-kml/toplu-sorgula` (birden fazla parsel, hız sınırlayıcı aralığıyla sırayla —
hız sınırlama artık `TkgmParselSorguIstemcisi` içinde, HER gerçek TKGM isteği öncesi uygulanır,
önbellekten gelen eşleştirmeler beklemeden geçer). Zaten başarıyla çekilmiş parseller **mevcut
`IKmlTekillestirmeService` üzerinden** (yeniden yazılmadan) otomatik atlanır.

**Toplu sorguda "hepsi / belirli" seçimi** (kullanıcı isteği üzerine eklendi): her seferinde tüm
listeyi taramak zorunda değilsiniz. `TopluParselSorguIstegiDto.TumunuSecModu=true` + `KisiId`
gönderilirse, o kişinin KML'si eksik TÜM aktif parselleri sunucu tarafında otomatik bulunup
sorgulanır (satılmış taşınmazlar hariç). `TumunuSecModu=false` (**varsayılan**) ile yalnızca
`Parseller` alanında elle seçilen liste sorgulanır.

TKGM entegrasyonu artık gerçek HAR kaydıyla doğrulandığı için `deneysel`/`deneyselUyari` alanları
varsayılan olarak **kapalıdır** (`ParselSorgu:DeneyselModu` appsettings anahtarı, varsayılan
`false`). Gerçek kullanımda sorun görülürse **kod değişikliği olmadan**, yalnızca bu config
değeri `true` yapılarak "Doğrulanmadı, kontrol edin." uyarısı tüm otomatik sonuçlarda tekrar
gösterilebilir.

### Ortak davranışlar

- **Hata toleransı**: TKGM sorgusu başarısız olursa kayıt `Durum=Basarisiz` işaretlenir (silinmez);
  "Tekrar dene" bu VAR OLAN kaydı günceller, yeni satır oluşturmaz (unique index çakışmasını
  önlemek için sorgu öncesi anahtarla mevcut kayıt aranır).
- **Silme** (`DELETE /api/parsel-kml/{id}`): soft delete. Global sorgu filtresi sayesinde
  silinen kayıt `IKmlTekillestirmeService`'in "başarıyla çekilmiş" anahtar listesinden otomatik
  çıkar — aynı Ada/Parsel bir sonraki sınıflandırmada **otomatik olarak** yeniden "sorgulanması
  gereken" listesine döner (Requirements madde 4.3), ek kod gerekmez. Bunun DB seviyesinde de
  çalışması için `ParselKml`/`Tasinmaz`/`Kisi` unique index'leri bu adımda **filtreli (partial)**
  hale getirildi (yalnızca silinmemiş kayıtlar arasında benzersizlik) — aksi halde silinmiş bir
  kayıt fiziksel olarak tabloda kaldığından yeniden ekleme unique index hatası verirdi.
- **Dosya adlandırma** (`KmlDosyaDepoService`): `{İL}_{İLÇE}_{MAHALLE}_{ADA}_{PARSEL}.kml` — dosya
  adında Türkçe karakterler ASCII'ye çevrilir (Requirements örneği de düz ASCII: "SAHINBEY").
  Depolama kök dizini appsettings'ten (`KmlDepolama:KokDizin`) yapılandırılır (Handbook madde 14).

### Kalan doğrulanmamış nokta

Kod artık gerçek, yakalanmış network trafiğine göre yazıldı ve sahte HTTP handler'la (gerçek
yanıt şemasını taklit eden) test edildi (`TkgmParselSorguIstemcisiTests`). Ancak bu ortamda
hâlâ internet erişimi yok, yani kod gerçek TKGM sunucusuna karşı fiilen ÇALIŞTIRILAMADI —
gerçek çalıştırmada karşılaşılabilecek noktalar: TKGM'nin rate-limit/bot koruması olup olmadığı
(HAR'da görülmedi, ama tek oturumluk bir kayıtla kesin olduğu söylenemez) ve parsel bulunamadığında
sunucunun tam olarak nasıl yanıt verdiği (404 mü, boş gövde mi — kodda ikisi de "Basarisiz" olarak
ele alınıyor, ama gerçek davranış farklıysa mesaj netliği etkilenebilir). İlk gerçek çalıştırmada
bir sorun çıkarsa yalnızca `TkgmParselSorguIstemcisi.cs` güncellenir.

## Bu adımda kapsam dışı bırakılanlar

- KML dosyalarının ZIP olarak toplu indirilmesi (Requirements madde 7 — ayrı bir raporlama adımı)
- YuklemeKaydi için ayrı bir CRUD Controller'ı (tablo ve oluşturma akışı hazır, ayrı listeleme
  ucu istenirse eklenir)
