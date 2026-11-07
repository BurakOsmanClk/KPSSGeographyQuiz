# B: Ortak (uygulama kabuğu ve platform) için görev promptları

> Her promptu ayrı bir oturumda ya da bir önceki PR merge edildikten sonra kullan. Önce [00_ONBOARDING.md](00_ONBOARDING.md) yapıştırılmış olmalı.

---

## B-M0.3 · Core sözleşmelerinin review'u
```
A'nın açtığı [contract] başlıklı Core sözleşmeleri PR'ını review et.
docs/ARCHITECTURE.md ile karşılaştır ve benim alanım açısından değerlendir: kayıt, ses, reklam, IAP, analitik,
sahne akışı. Bu arayüzlerle işlerimi rahatça yapabilir miyim? Eksik veya fazla olan bir şey var mı?
Bulduklarını PR'a yorum olarak yaz. Sorun yoksa onay ver.
```

## B-M0.6/0.7 · Sahne akışı ve UI Toolkit temeli
```
Önce unity:ui ve unity:ui-uitk skill'lerini yükle. Branch adı: b/app-shell.
1. App assembly: AppBootstrap (Boot sahnesinde). Görevleri: servisleri Services'e kaydetmek (şimdilik
   Mock versiyonları: MockAds, MockPurchase, MockAnalytics, MockRemoteConfig), kaydı yüklemek ve Menu'ye geçmek.
2. SceneFlow (ISceneFlow uygulaması): GoToMenu, StartLevel(LevelRequest). Geçişlerde kısa bir fade olsun.
3. UI Toolkit temeli: PanelSettings (ölçek: Scale With Screen Size 1920x1080, match 0.5), tema (.tss),
   ortak USS değişkenleri (renkler, köşe yuvarlaklığı, boşluklar), OFL lisanslı Türkçe destekli bir font (Nunito).
4. ScreenRouter (ekran yığını, Push/Pop, Android geri tuşu desteği) ve SafeArea bileşeni.
5. Basit MainMenu.uxml: "Oyna" butonu "Tüm Türkiye" İli Bul seviyesini açsın, ayrıca "Ayarlar" butonu olsun.
6. Boot, Menu ve Game sahnelerini oluşturup Build Profiles listesine ekleyen bir editör menüsü yaz:
   KPSS/Sahneleri Kur. Game.unity içeriği A'ya ait, sadece boş sahne + kamera oluştur.
Font dosyasını Unity'ye nasıl ekleyeceğimi (indirme linki ve import ayarları) adım adım yaz.
```

## B-M1.5/1.6 · HUD, seviye seçimi ve sonuç ekranı
```
Önce A'nın IHudModel sözleşmesinin merge edildiğini kontrol et. unity:ui-uitk ve unity:craft-game-floors
skill'lerini yükle. Branch adı: b/game-ui.
- GameHud.uxml/uss + GameHudController(IHudModel): hedef il kartı, süre çubuğu (yeşil→sarı→kırmızı),
  canlar, puan (sayarak artan), seri, duraklat butonu. Yatay ekranda başparmak erişimine uygun yerleşim.
- LevelSelect ekranı: LevelCatalog'u okuyan kartlar (yıldızlar ve kilit durumu).
- Results ekranı: SessionEnded olayını dinlesin. 1–3 yıldızı sırayla animasyonla göstersin, puan, isabet oranı,
  "en çok karıştırdığın iller", Tekrar Oyna / Menü butonları olsun.
- Pause ekranı: Devam / Yeniden başlat / Menü.
Görsel dil: GDD §7. Prototipteki renk paletini temel al.
```

## B-M1.7/1.8 · Kayıt, ses, titreşim, ayarlar
```
Branch adı: b/save-audio-settings.
- SaveService (ISaveService): JSON, persistentDataPath, atomik yazma (tmp + replace), yedek dosya,
  version alanı ve geçiş (migration) iskeleti. Uygulama arka plana alınınca (OnApplicationPause) kaydetsin.
- SessionEnded dinleyicisi: yıldızları ve rekoru güncellesin, LevelUnlocked yayınlasın.
- AudioService: AudioMixer (Music/SFX grupları, unity:audio-setup-mixers skill'iyle) ve basit müzik geçişi.
  Prototipteki SfxPlayer'ın kodla üretilen seslerini yer tutucu olarak koru.
- HapticsService: Android (Vibrator, kısa süre) ve iOS (UIImpactFeedbackGenerator, native plugin gerekirse
  basit bir .mm köprüsü) için. Editörde sessizce çalışmasın.
- Settings ekranı: ses, müzik, titreşim, "Hata bildir" (mailto), sürüm bilgisi.
- EditMode testleri: SaveService (bozuk dosyadan kurtarma, versiyon geçişi).
```

## B-M1.9 · Android build
```
unity:unity-cli skill'ini yükle. Branch adı: b/android-build.
- Player Settings: paket adı (com.<ad>.kpsscografya), yatay ekran (Landscape Left/Right), IL2CPP, ARM64,
  minimum API 24, hedef API (güncel Play gereksinimi), Splash, uygulama adı "KPSS Coğrafya".
- Keystore oluşturma adımlarını bana anlat. Keystore ve şifreler ASLA repoya girmesin, .gitignore'u kontrol et.
- Komut satırından build alan bir editör scripti yaz: KPSS/Build/Android (AAB + geliştirme için APK).
Bir sorun çıkarsa Unity Hub'dan "Android Build Support" modülünün kurulu olduğunu bana kontrol ettir.
```

## B-M2.6/2.7/2.8 · İlerleme, seri, istatistik, tutorial
```
Branch adı: b/progression.
- XP formülü (puan / 10 + yıldız bonusu), oyuncu seviyesi eğrisi, konu ustalığı (son 20 cevabın kayan ortalaması).
- Günlük seri (yerel tarih; saat dilimi hilelerine karşı basit kontrol), günlük görev (ör. "Bugün 2 tur oyna").
- Stats ekranı: toplam tur, isabet oranı, en çok karıştırılan 5 il, en uzun seri.
- İlk açılış: 3 adımlı mini tutorial (dokun / yakınlaştır / süre ve can). Bir kez gösterilsin, ayarlardan tekrar açılabilsin.
- A'nın konu setleri CSV'sini çapraz kontrol et: her setin kaynağını aç, illeri doğrula, sorunları PR'a yorum olarak yaz.
```

## B-M3 · Gelir ve servisler
```
Sırasıyla unity:build-live-game, unity:levelplay-unity-integration ve unity:implement-in-app-purchases
skill'lerini yükle. Her biri ayrı bir PR olsun.
1. b/ugs: UGS Analytics (olaylar: app_open, tutorial_done, session_start, session_end{level,stars,score},
   continue_offered/accepted, purchase) + Remote Config (interstitial_min_interval_sec, interstitial_first_after_sessions,
   time_per_question_override).
2. b/ads: LevelPlay. Ödüllü reklam: "devam et" (A'nın ContinueOffered olayı) ve seri dondurma.
   Geçiş reklamı: tur sonunda, Remote Config sınırlarıyla. Premium ya da no_ads varsa reklam gösterilmesin. Test modu açık olsun.
3. b/iap: Unity IAP. Ürünler: premium, no_ads (non-consumable). Shop ekranı, geri yükleme butonu (iOS zorunlu),
   PremiumChanged olayı.
4. b/consent: KVKK/GDPR rıza ekranı (ilk açılışta), iOS ATT istemi, rızanın LevelPlay ve UGS'ye iletilmesi.
   docs/privacy-policy.md taslağı (GitHub Pages'te yayınlanacak).
API anahtarı veya ID gibi gizli bilgileri koda gömme. Hangisinin nereye girileceğini bana ayrıca listele.
```

## B-M4 · Yayın
```
unity:unity-cli skill'ini yükle.
- iOS: Xcode projesi üretimi, imzalama, Info.plist (ATT açıklaması: NSUserTrackingUsageDescription, SKAdNetwork ID'leri),
  TestFlight'a yükleme adımları. Mac yoksa Unity Build Automation seçeneğini adım adım anlat.
- Google Play: iç test → kapalı test (12 test kullanıcısı / 14 gün kuralı) → üretim kontrol listesi;
  Data safety formu (reklam ID'si, analitik), içerik derecelendirmesi.
- Mağaza metinleri: Türkçe başlık (30 karakter), kısa açıklama (80), uzun açıklama (ASO anahtar kelimeleri:
  KPSS, coğrafya, harita, iller, Türkiye). A ile birlikte ekran görüntüsü listesi hazırla.
```
