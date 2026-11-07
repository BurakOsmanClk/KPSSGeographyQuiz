# CLAUDE.md

## Proje
KPSS Coğrafya konularına yönelik eğitim/quiz oyunu. Motor: **Unity 6**. Dil: C#. Oyun içi metinler Türkçe.

## Ekip ve dokümanlar
- İki geliştirici, her biri kendi Claude Code oturumuyla: **A = Burak** (Content, Map, Gameplay, Game.unity, Tools/) · **B = Ortak** (App, Meta, Services, UI, Boot/Menu sahneleri, build). `Core` ortaktır; değişiklik iki onay gerektirir.
- Oturum başında kullanıcı rolünü söyler. Sadece o rolün klasörlerinde değişiklik yap.
- Hedef: Android + iOS, ücretsiz + reklam + premium, v1.0 için 4–6 hafta.
- Önce şunları oku: `docs/GDD.md` (tasarım), `docs/ARCHITECTURE.md` (omurga ve sözleşmeler), `docs/WORK_SPLIT.md` (iş bölümü ve git kuralları), `docs/ROADMAP.md` (milestone'lar), `docs/TOOLING.md` (skill'ler ve araçlar). Görev promptları `docs/prompts/` altında.
- Git: `main`'e doğrudan push yok. `a/...`, `b/...`, `ab/...` branch'leri ve PR kullanılır. Sözleşme değiştiren PR'ların başlığına `[contract]`, ayar değiştirenlerin başlığına `[settings]` yazılır.
- Unity'ye özgü bir işe başlamadan önce ilgili `unity:*` skill'ini yükle.

## Çalışma düzeni
- Kullanıcı Unity Editor'ü kendi bilgisayarında çalıştırır; Claude bulut oturumunda çalışır ve Unity Editor'e erişimi yoktur.
- Değişiklikler GitHub reposu üzerinden paylaşılır: Claude kod/veri yazar ve push eder, kullanıcı pull edip Unity'de test eder.
- Claude yeni bir asset (script, json vb.) eklediğinde `.meta` dosyası oluşturmaz; Unity bunu kullanıcının makinesinde üretir ve kullanıcı commit eder. Sahne (`.unity`) ve prefab YAML'larını elle düzenlemekten kaçın; bunun yerine kullanıcıya Editor'de yapılacak adımları anlat.

## Konvansiyonlar
- Proje dosyaları `Assets/_Project/` altında.
- Namespace: `KPSSGeography` (alt namespace'ler: `.Core`, `.Content`, `.Map`, `.Gameplay`, `.Meta`, `.Services`, `.UI`, `.App`, editör kodu `.EditorTools`). Prototipteki `.Data` ve `.Quiz` M0'da `.Content` ve `.Gameplay` olarak taşınacak.
- Input: projede yalnızca **Input System** aktif (`activeInputHandler: 1`); eski `UnityEngine.Input` API'si kullanılmaz.
- UI hedefi: **UI Toolkit** (UXML/USS). Şu anki arayüz (giriş ekranı, bölüm karuseli, HUD, sonuç ekranı) kodla kurulan uGUI'dir; renkler `UI/Theme.cs`, ikonlar `UI/ShapeSprites.cs` (SDF ile üretilir). UI Toolkit'e geçişte bu tasarım referans alınır.
- Soru verileri koddan ayrı tutulur (JSON veya ScriptableObject).

## İl haritası verisi
- Kaynak: Natural Earth 10m admin-1 (kamu malı) → `Tools/geo/ne_10m_admin1_turkey.geojson`.
- `python Tools/geo/build_provinces.py` ile önceden üçgenlenmiş `Assets/_Project/Resources/Data/turkey_provinces.json` üretilir (plaka kodu, Türkçe ad, coğrafi bölge, etiket noktası, mesh, sınır halkaları).

## Fiziki harita verisi
- `python Tools/geo/build_features.py` → `Resources/Data/turkey_features.json`. Akarsular ve göller Natural Earth'ten (`Tools/geo/ne_10m_rivers_turkey.geojson`, `ne_10m_lakes_turkey.geojson`); dağlar (sırt çizgisi), volkanlar (nokta), ovalar ve platolar (alan) `Tools/geo/features_manual.json` içinde elle, [boylam, enlem] olarak tanımlı ve KPSS bilgi notları içerir. Yeni yer şekli eklemek için bu dosyaya ekleyip scripti çalıştır.
- Unity'de `Map/FeatureLayer.cs` + `Map/FeatureView.cs` çizer; göller ve akarsular her zaman dekor olarak görünür, diğerleri yalnızca kendi bölümünde.

## Reklamlar
- Oyun kodu yalnızca `Ads/IAdService.cs` arayüzünü bilir. Şu an `Ads/MockAdService.cs` (ekranda test reklamı) bağlı.
- Yerleşimler: ödüllü = elenince "+1 can ile devam" (bölüm başına 1 kez); geçiş = sonuç ekranından ayrılırken, oturumda 2 bölümden sonra ve en az 180 sn arayla (`Ads/AdPolicy.cs`, ayarlar `QuizConfig`).
- Gerçek reklamlar: `unity:levelplay-unity-integration` skill'inin adımları izlenecek. **Ads Mediation (`com.unity.services.levelplay`) paketi `Packages/packages-lock.json`'da görünmeden LevelPlay kodu yazılmaz.** Sonra `IAdService`'i uygulayan bir LevelPlay servisi yazılıp `GameBootstrap`'ta mock yerine bağlanır.

## Prototip
- Herhangi bir sahnede Play → `GameBootstrap` otomatik oluşur (ya da **KPSS > Prototip Sahnesini Oluştur**). Her şey çalışma anında kurulur.
- Akış (`Core/AppFlow.cs`): Giriş ekranı → Bölüm karuseli → Oyun → Sonuç (Tekrar / Sonraki / Bölümler).
- Bölümler `Resources/Data/levels.json` içinde (`mode`: `find` = İli Bul, `region` = Bölge Modu, `feature` = Yer Şekli (`category`: river | mountain | volcano | plain | plateau | mixed), `soon` = Yakında; `regions` boşsa tüm Türkiye).
- İlerleme (yıldız, rekor, kilitler) `Meta/ProgressStore.cs` → PlayerPrefs. Yıldız: tamamlarsa 3 − hata (en az 1). Bir bölüm, önceki oynanabilir bölümden ≥1 yıldızla açılır.
- Harita renkleri bölgelere göre (`Map/MapPalette.cs`); doğru bilinen il bölge renginin doygun haline boyanır.
