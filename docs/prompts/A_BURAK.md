# A: Burak (oyun çekirdeği ve içerik) için görev promptları

> Her promptu ayrı bir oturumda ya da bir önceki PR merge edildikten sonra kullan. Önce [00_ONBOARDING.md](00_ONBOARDING.md) yapıştırılmış olmalı.

---

## A-M0.3 · Core sözleşmeleri (ortak PR, B onaylayacak)
```
docs/ARCHITECTURE.md §3'teki Core sözleşmelerini uygula. Branch adı: ab/core-contracts.
- Assets/_Project/Scripts/Core altına şunları yaz: Services (service locator), GameEvents (tip güvenli event bus),
  servis arayüzleri (ISaveService, IAudioService, IHapticsService, IAdsService, IPurchaseService,
  IAnalyticsService, IRemoteConfig, ISceneFlow) ve olay struct'ları (SessionStarted, QuestionAnswered,
  SessionEnded, LevelUnlocked, PremiumChanged, SettingsChanged).
- Arayüzlerin kullandığı veri tiplerini de tanımla: PlayerSave iskeleti, SfxId, MusicId, LevelRequest, SessionResult.
  PlayerSave alanlarını ARCHITECTURE.md §8'e göre belirle.
- Uygulama yazma, sadece sözleşmeleri yaz. Her public üyeye 1 satırlık Türkçe XML yorumu ekle.
- Services ve GameEvents için EditMode testleri ekle (Assets/_Project/Tests/EditMode).
PR'ı açıp başlığına [contract] yaz. Açıklamada B'den onay iste.
```

## A-M0.4/0.5 · asmdef'ler + MapQuizSession'ın çıkarılması
```
docs/ARCHITECTURE.md §1, §4 ve §11'e göre yeniden yapılandırma yap. Branch adı: a/backbone-gameplay.
1. Klasörleri taşı: Scripts/Data → Scripts/Content, Scripts/Quiz → Scripts/Gameplay. Namespace'leri güncelle.
   Taşırken .meta dosyalarını da dosyalarla birlikte taşı (git mv), GUID'ler korunmalı.
2. Core, Content, Map, Gameplay, UI, App ve Tests.EditMode için .asmdef dosyalarını oluştur.
   Bağımlılıklar sadece aşağı doğru olsun. Input System ve URP referanslarını gerektiği yere ekle.
3. CityQuizGame'i genelleştir: MapQuizSession (durum makinesi, süre, can, seri), IQuestionSource,
   FindProvinceSource, saf C# bir ScoreCalculator. Oturum olaylarını GameEvents üzerinden yayınla.
4. EditMode testleri: ScoreCalculator, FindProvinceSource (tekrar yok, istenen sayıda soru),
   ProvinceView.Contains (birkaç bilinen il merkezi noktası).
5. Prototip oyun davranışı birebir aynı kalmalı.
Unity Editor'e erişimin varsa (unity:unity-cli) derlemeyi ve testleri çalıştır. Yoksa bana Editor'de
neyi kontrol edeceğimi madde madde yaz.
```

## A-M1.1 · Dokunmatik kontroller
```
Input System + EnhancedTouch kullan (fizik motoru gerekmez). Branch adı: a/touch-input.
GDD §9 ve ARCHITECTURE §7'ye göre:
- IMapInput arayüzü (Tap, Pan, PinchZoom, DoubleTap olayları) ve iki uygulaması: TouchMapInput (EnhancedTouch)
  ve MouseMapInput (editör).
- MapCameraController ve MapPointer doğrudan Mouse.current okumasın, IMapInput'u dinlesin.
- Sürükleme eşiği: 10 dp'nin altındaki hareket Tap sayılsın. Pinch, iki parmağın orta noktasına doğru yakınlaştırsın.
- Dokunma toleransı: dokunulan nokta hiçbir ilin içinde değilse 24 dp yarıçap içindeki en yakın ili seç.
- UI üzerindeki dokunuşlar haritaya geçmesin.
Kabul kriteri: Android cihazda Yalova ve Kilis yakınlaştırmadan bile seçilebilmeli, pinch ve sürükleme akıcı olmalı.
```

## A-M1.3/1.4 · Seviye verisi ve HUD sözleşmesi
```
Branch adı: a/levels-hud-model.
- Content: LevelDefinition ve LevelCatalog ScriptableObject'leri (ARCHITECTURE §5).
  Editör menüsü "KPSS/Varsayılan Seviyeleri Oluştur": 7 bölge + "Tüm Türkiye" için İli Bul seviyeleri.
  Bölgeye göre filtreleme FindProvinceSource'ta olsun.
- Gameplay: IHudModel arayüzü (TargetText, PromptText, TimeRatio, TimeRemaining, Lives, MaxLives, Score,
  Streak, Multiplier, QuestionIndex, QuestionCount + olaylar) ve MapQuizSession'ın bunu uygulaması.
  Bu bir sözleşme, PR başlığına [contract] yaz ve B'nin onayını iste.
- GameSceneEntry: Game.unity'de LevelRequest'i ISceneFlow'dan alıp oturumu başlatsın.
```

## A-M2.1/2.2/2.3 · Konu haritaları ve içerik hattı
```
Branch adı: a/topic-sets.
1. Content/TopicSet ScriptableObject: id, title, category, plates[], explanation, source, isPremium.
2. Tools/content/topic_sets.csv formatını tanımla (plakalar ";" ile ayrılsın) ve editör menüsünü yaz:
   "KPSS/İçeriği İçe Aktar". Bu menü CSV'den SO üretip günceller ve LevelCatalog'a seviye ekler.
3. TopicSetSource: soru "X'te öne çıkan bir il seç" şeklinde olsun, doğru kümedeki herhangi bir il doğru sayılsın.
   Yanlış cevapta doğru illerin hepsi kısa süre vurgulansın ve explanation gösterilsin.
4. GDD §4'teki listeden en az 15 konu seti hazırla. Her biri için:
   - Güvenilir kaynak kullan (MEB, TÜİK, MTA, ETKB, TKİ, UNESCO) ve source alanına yaz.
   - KPSS kaynaklarında öne çıkan illeri seç (genelde ilk 3–6 il). Emin olmadığın bir veriyi ekleme,
     "doğrulanacak" diye işaretle.
   - Bana tablo halinde özet ver: set, iller, kaynak, güven derecesi.
CSV'yi B çapraz kontrol edecek. PR açıklamasına kontrol listesini ekle.
```

## A-M2.4/2.5 · Bölge Modu ve Günlük Meydan Okuma
```
Branch adı: a/region-daily.
- RegionSource: iki soru tipi olsun: (a) "Bu il hangi bölgede?": il vurgulanır, cevap için 7 bölge butonu
  (HUD'a seçenek listesi sözleşmesi gerekir, IHudModel'e opsiyonel Choices ekle ve B'ye haber ver);
  (b) "Ege'nin bir ilini bul".
- DailyChallengeSource: System.Random(seed = yyyyMMdd) ile her gün herkese aynı 10 karışık soru.
- Testler: aynı tarih = aynı sorular, farklı tarih = farklı sorular.
```

## A-M3.5/3.6 · Devam akışı ve performans
```
Branch adı: a/continue-perf.
- MapQuizSession'a "ContinueWithExtraLife()" ekle. Elenince oturum "ContinueOffered" durumunda beklesin,
  karar UI ve Ads tarafından verilsin (olayla). Tur başına en fazla 1 kez.
- Premium içerik: LevelDefinition.isPremium ise ve IPurchaseService.IsPremium false ise oturum başlamasın, olay yayınlansın.
- Performans: Update'lerdeki gereksiz atamaları ve GC tahsislerini temizle (ProvinceView renk güncellemesi,
  string birleştirmeleri). Profil çıkarmak için bana adım adım Profiler talimatı ver.
```
