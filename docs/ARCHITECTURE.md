# Mimari: Oyunun Omurgası

> Amaç: iki geliştiricinin (ve iki Claude Code oturumunun) **birbirinin dosyasına dokunmadan** paralel çalışabilmesi.
> Bunun yolu: **net sınırlar (assembly'ler), net sözleşmeler (interface'ler), net sahiplik (klasörler).**

## 1. Katmanlar ve assembly'ler (asmdef)
```
                ┌──────────────────────────── App (Boot, sahne akışı, servis kurulumu) ───────────────────────────┐
                │                                                                                                  │
   ┌─────── UI (UI Toolkit ekranları, HUD) ───────┐         ┌────────── Services (Ads, IAP, Analytics, RemoteConfig) ──┐
   │                                              │         │                                                          │
   ├── Gameplay (MapQuizSession, modlar,          │         ├── Meta (Save, Progression, DailyChallenge, Achievements)│
   │   soru kaynakları, puanlama)                 │         │                                                          │
   ├── Map (harita kurulum, render, input)        │         │                                                          │
   └──────────────────────────┬───────────────────┘         └───────────────────────────┬──────────────────────────────┘
                              └──────────────── Content (veri modelleri, SO'lar) ──────────┘
                                                         │
                                                       Core (sözleşmeler, servis kaydı, event bus, yardımcılar)
```
**Kural:** bağımlılık sadece aşağı doğru gider. `Gameplay`, `Services`'i bilmez; olay (event) yayınlar, `App` ya da `Meta` dinler.

| Assembly | Klasör | Namespace | Sahibi |
|---|---|---|---|
| `KPSS.Core` | `Assets/_Project/Scripts/Core` | `KPSSGeography.Core` | **Ortak** (değişiklik = iki onay) |
| `KPSS.Content` | `.../Content` | `KPSSGeography.Content` | **A** |
| `KPSS.Map` | `.../Map` | `KPSSGeography.Map` | **A** |
| `KPSS.Gameplay` | `.../Gameplay` | `KPSSGeography.Gameplay` | **A** |
| `KPSS.Meta` | `.../Meta` | `KPSSGeography.Meta` | **B** |
| `KPSS.Services` | `.../Services` | `KPSSGeography.Services` | **B** |
| `KPSS.UI` | `.../UI` (+ `Assets/_Project/UI/` UXML/USS) | `KPSSGeography.UI` | **B** (oyun içi HUD: A ve B birlikte, bkz. §6) |
| `KPSS.App` | `.../App` | `KPSSGeography.App` | **B** |
| `KPSS.Editor` | `Assets/_Project/Editor` | `KPSSGeography.EditorTools` | İlgili özelliğin sahibi |
| `KPSS.Tests.EditMode` | `Assets/_Project/Tests/EditMode` | `KPSSGeography.Tests` | Herkes kendi kodunun testini yazar |

> Prototipteki `Data` ve `Quiz` klasörleri M0'da sırasıyla `Content` ve `Gameplay` olarak taşınır.

## 2. Sahneler
| Sahne | İçerik | Sahibi |
|---|---|---|
| `Boot.unity` | Sadece `AppBootstrap`: servisleri kurar, kayıt dosyasını yükler, Menu'ye geçer | B |
| `Menu.unity` | UI Toolkit menü ekranları | B |
| `Game.unity` | Kamera + `GameSceneEntry`: haritayı kurar, oturumu başlatır, HUD'u bağlar | A |

**Sahne çakışmalarına karşı:** sahnelerde neredeyse hiçbir şey olmaz. Her şey kodla kurulur ya da prefab/UXML'den yüklenir. Bir sahneyi yalnızca sahibi düzenler.

## 3. Core sözleşmeleri (M0'da yazılır, sonra dondurulur)
```csharp
namespace KPSSGeography.Core
{
    // --- Servis kaydı (basit service locator) ---
    public static class Services
    {
        public static void Register<T>(T service) where T : class;
        public static T Get<T>() where T : class;
        public static bool TryGet<T>(out T service) where T : class;
    }

    // --- Olay yolu (event bus): modüller birbirini tanımadan haberleşir ---
    public static class GameEvents
    {
        public static void Publish<T>(T evt) where T : struct;
        public static void Subscribe<T>(System.Action<T> handler) where T : struct;
        public static void Unsubscribe<T>(System.Action<T> handler) where T : struct;
    }

    // --- Servis arayüzleri (uygulamaları Services/Meta'da, editörde sahte/mock versiyonları) ---
    public interface ISaveService      { PlayerSave Data { get; } void Save(); void ResetAll(); }
    public interface IAudioService     { void PlaySfx(SfxId id); void PlayMusic(MusicId id); bool Muted { get; set; } }
    public interface IHapticsService   { void Light(); void Medium(); void Error(); bool Enabled { get; set; } }
    public interface IAdsService       { bool IsRewardedReady { get; } void ShowRewarded(string placement, System.Action<bool> onDone);
                                         void TryShowInterstitial(string placement); }
    public interface IPurchaseService  { bool IsPremium { get; } bool IsNoAds { get; } void Buy(string productId, System.Action<bool> onDone);
                                         void Restore(); }
    public interface IAnalyticsService { void Log(string evt, params (string key, object value)[] p); }
    public interface IRemoteConfig     { int GetInt(string key, int def); float GetFloat(string key, float def); bool GetBool(string key, bool def); }
    public interface ISceneFlow        { void GoToMenu(); void StartLevel(LevelRequest request); }
}
```
**Olaylar (struct):** `SessionStarted`, `QuestionAnswered`, `SessionEnded(SessionResult)`, `LevelUnlocked`, `PremiumChanged`, `SettingsChanged`.
→ Örnek: `Gameplay` tur bittiğinde `SessionEnded` yayınlar. `Meta` yıldız ve XP'yi kaydeder, `Services` analitik olayını gönderir, `UI` sonuç ekranını açar.

## 4. Gameplay: modların omurgası
```csharp
namespace KPSSGeography.Gameplay
{
    public sealed class Question
    {
        public string Prompt;                 // "Fındık üretiminde öne çıkan bir il seç"
        public string ShortTarget;            // HUD'da büyük yazı: "FINDIK"
        public HashSet<int> CorrectPlates;    // 1 ya da daha fazla doğru il
        public string Explanation;            // yanlışta gösterilecek bilgi notu
    }

    public interface IQuestionSource      // her mod = bir soru kaynağı
    {
        string ModeId { get; }
        IReadOnlyList<Question> Build(LevelDefinition level, System.Random rng);
    }

    public sealed class MapQuizSession    // prototipteki CityQuizGame'in genelleştirilmiş hali
    {
        // Süre, can, seri, puanlama, durum makinesi. Girdi: IMapInput'tan gelen il seçimi.
        // Çıktı: GameEvents üzerinden QuestionAnswered / SessionEnded.
    }
}
```
**Soru kaynakları:** `FindProvinceSource` (İli Bul), `RegionSource`, `TopicSetSource` (konu haritaları), `DailyChallengeSource` (tarih tohumlu, karışık).
**Puanlama:** `ScoreCalculator` saf C# sınıfıdır, EditMode testleri yazılır.

## 5. Content: veri odaklı içerik
| Varlık | Tür | Açıklama |
|---|---|---|
| `turkey_provinces.json` | Resources | Geometri, plaka, ad, bölge (prototipte hazır) |
| `TopicSet` | ScriptableObject | `id, title, category, plates[], explanation, source, isPremium` |
| `LevelDefinition` | ScriptableObject | `id, modeId, topicSetId, questionCount, timePerQuestion, maxMistakes, starThresholds[3], unlockRequirement` |
| `LevelCatalog` | ScriptableObject | Tüm seviyelerin sıralı listesi (menü bu listeyi okur) |

- **İçerik akışı:** Google Sheets → CSV → `Tools/content/*.csv` → editör menüsü **KPSS → İçeriği İçe Aktar** → ScriptableObject'ler.
- CSV metin dosyası olduğu için Claude doğrudan düzenleyebilir, birleştirme (merge) çakışmaları da kolay çözülür.

## 6. UI: UI Toolkit
- **Neden UI Toolkit:** UXML ve USS metin dosyaları. Claude doğrudan yazabiliyor, prefab YAML'ı gibi birleştirme çakışması çıkarmıyor, mobilde güvenli alan ve ölçekleme sorunsuz.
- **Ekranlar:** `MainMenu`, `ModeSelect`, `LevelSelect`, `Results`, `Settings`, `Shop`, `Stats`, `Pause`, `ConsentDialog`.
- **Altyapı:** `ScreenRouter` (ekran yığını) her ekranı bir `ScreenController` + `.uxml` çiftiyle açar.
- **Oyun içi HUD** (`GameHud.uxml`): B tasarlar ve yazar. A ise `IHudModel` arayüzünü sağlar (`Target`, `TimeRatio`, `Lives`, `Score`, `Streak`, olaylar). Böylece HUD, oyun mantığını bilmeden çalışır.
- **Font:** OFL lisanslı, Türkçe destekli font. UI Toolkit'te `FontAsset` + `PanelSettings` + tema dosyası (`.tss`).
- Prototipteki kodla kurulan uGUI HUD, M1 sonunda kaldırılır.

## 7. Input (mobil + editör)
- **Input System + EnhancedTouch:** dokunma, pinch ve sürükleme. Editörde mouse ile aynı davranış.
- **`IMapInput` arayüzü:** `event Action<Vector2> Tap`, `PinchZoom`, `Pan`. `MapCameraController` ve `MapPointer` bu arayüzü dinler.
- **Dokunma toleransı:** dokunulan nokta bir ilin içinde değilse en yakın il kenarına `N` piksel tolerans uygulanır (küçük iller için).

## 8. Kayıt (Save)
- `PlayerSave` (JSON, `Application.persistentDataPath/save.json`), içinde `version` alanı ile.
- **İçerik:** `levels{id → stars, bestScore}`, `topicMastery{id → %}`, `xp`, `level`, `streak{count, lastDay}`, `settings{sfx, music, haptics}`, `consent`, `stats`.
- **Güvenli yazma:** önce `save.tmp` dosyasına yazılır, sonra yerine taşınır. Bozuk dosyada yedekten yüklenir.
- **Satın alma durumu:** kayıt dosyasında değil, IAP servisinden (makbuz) okunur.

## 9. Servisler (B)
| Servis | Uygulama | Not |
|---|---|---|
| Reklam | **Unity LevelPlay** (Ads Mediation paketi) | Ödüllü + geçiş reklamı. Test modu; editörde sahte (mock) servis |
| Satın alma | **Unity IAP** | `premium` (non-consumable), `no_ads` (non-consumable), geri yükleme (restore) |
| Analitik | **Unity Gaming Services (UGS) Analytics** | Huni (funnel): tutorial → ilk tur → 3. tur → satın alma |
| Remote Config | **UGS Remote Config** | Geçiş reklamı sıklığı, süreler, fiyat testleri |
| Rıza (consent) | LevelPlay + UGS rıza API'leri | KVKK/GDPR, iOS ATT |

Her servisin editörde ve testlerde çalışan bir `Mock` versiyonu olur. `AppBootstrap`, platforma göre gerçek ya da sahte servisi kaydeder.

## 10. Test ve kalite
- **EditMode testleri:** `ScoreCalculator`, soru kaynakları (tekrar yok, doğru küme), kayıt (versiyon geçişi), `Contains()` (nokta-poligon testi).
- **Cihaz testi:** her milestone sonunda Android cihaza kurulup en az 10 dk oynanır.
- **Performans hedefi:** düşük seviye Android'de 60 FPS, ilk açılış 3 sn'nin altında, APK/AAB boyutu 60 MB'ın altında.

## 11. Klasör yapısı (hedef)
```
Assets/_Project/
  Scripts/{Core,Content,Map,Gameplay,Meta,Services,UI,App}/   (+ her birinde .asmdef)
  UI/{Screens,Components,Themes}/*.uxml|*.uss|*.tss
  Content/{TopicSets,Levels}/*.asset
  Resources/Data/turkey_provinces.json
  Art/{Icons,Fonts}/   Audio/{Sfx,Music}/
  Scenes/{Boot,Menu,Game}.unity
  Editor/   Tests/EditMode/
Tools/{geo,content}/
docs/
```
