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
- `python Tools/geo/build_provinces.py` illere `neighbors` (komşu plakalar) da ekler. `python Tools/geo/build_features.py` → `Resources/Data/turkey_features.json` (göller: Natural Earth + elle elips, tip bilgisiyle; barajlar akarsu çizgisine oturtulur; kıyı, UNESCO, milli park noktaları ve fay hatları `features_manual.json`'da). Akarsular ve göller Natural Earth'ten (`Tools/geo/ne_10m_rivers_turkey.geojson`, `ne_10m_lakes_turkey.geojson`); dağlar (sırt çizgisi), volkanlar ve masif araziler (nokta, `massifs`), ovalar ve platolar (alan) `Tools/geo/features_manual.json` içinde elle, [boylam, enlem] olarak tanımlı ve KPSS bilgi notları içerir. Yeni yer şekli eklemek için bu dosyaya ekleyip scripti çalıştır.
- Fiziki içerik listesi ve sınıflandırması: `docs/content/FIZIKI_COGRAFYA.md` (45 ova, 13 plato, 41 dağ, 10 volkan). Ova/plato için `features_manual.json`'da `polygon`, `polygons` ya da `ellipse` (merkez, rx/ry km, açı); delta/graben ovaları `PLAINS_FROM_RIVERS` ile akarsu yatağından üretilir. Her öğede `type` = KPSS oluşum türü.
- Kıyı tipleri: `features_manual.json` → `coastTypes` (+ `coastTypeInfo`: tip adı ve tanımı). Şerit tipleri (boyuna, enine, dalmaçya, kalanklı, limanlı) `segment` (from/to/via) ile verilir; çizgi `build_features.py`'da Türkiye kıyısından otomatik kesilir. Ria, tombolo, lagün nokta. Kategori `coasttype`, `subtype` = tip. Bölüm "Kıyı Tipleri ve Şekilleri" (SULAR VE KIYILAR): tip başına bir soru, o tipin bütün örnekleri doğru (`QuestionBuilder.CoastTypes`). Haliç ve kıyı oku kaynaklar tutarsız olduğu için yok.
- Unity'de `Map/FeatureLayer.cs` + `Map/FeatureView.cs` çizer. Göller her zaman görünür (zemin). Akarsu, dağ, volkan, ova ve plato **her biri yalnızca kendi bölümünde, tek başına** gösterilir; türleri karıştıran bölüm yapılmaz (kullanıcı tercihi).

## Görsel katmanlar ve erişilebilirlik
- Kabartma: `python Tools/geo/build_relief.py` (AWS Terrain Tiles, zoom 8, `Tools/geo/cache/` git'e girmez) → `Resources/Textures/turkey_relief.png`; `Map/ReliefLayer.cs` illerin üstüne yarı saydam çizer (yer şekli bölümlerinde güçlü). Atıflar `docs/CREDITS.md`.
- Komşu ülkeler (`countries`), dağ alanları (dış + koyu çekirdek, `innerVertices`), akarsu yatağından türetilen delta/graben ovaları ve bölge dış hatları (`regionOutlines`) veri scriptlerinde üretilir.
- `UI/MapLabelLayer.cs`: deniz/boğaz/ülke adları her zaman; il adları yakınlaşınca ve sadece cevabı ele vermeyen modlarda (yer şekli [miras hariç], konu, KPSS sorusu) ve "Haritayı incele" görünümünde.
- `Core/GameSettings.cs`: renk körü dostu palet (Okabe–Ito: doğru mavi, yanlış turuncu; `Theme.ApplyColorblind`, `MapPalette.ApplyColorblind`), büyük yazı (`UI/TextScaler.cs`, %15), efektler (`Map/MapEffects.cs`: bölge sınırı çizimi, ışıltı; HUD seri kenar parlaması).

## Yer şekli haritalarında şehir yok
- `feature` / `featurechoice` bölümlerinde (dağ, volkan, ova, plato, fay, akarsu, göl, baraj, boğaz/körfez, yarımada) il içi sınırlar ve il adları gizlenir; harita yalnızca Türkiye'nin dış hattıyla boş silüet olur (kullanıcı tercihi). **UNESCO ve milli park bölümleri hariç** (iller kalır). Kod: `CityQuizGame.HidesProvinces` → `TurkeyMap.SetBordersHidden` (dış hat `nationalOutline`, `build_provinces.py` üretir), il adları `QuizHud.ProvinceNamesAllowed`.

## Sınav modu
- Açma/kapama: bölüm seçim ekranı sağ altı (`LevelSelectScreen`), `GameSettings.ExamMode` (PlayerPrefs). Giriş ekranında harita her zaman renklidir; bölüm seçimi ve oyunda sınav ayarı geçerlidir (`AppFlow` → `ExamModeController.SetLook`).
- Görünüm: `Core/ExamLook.Blend` (0..1) tek ortak değerdir. `ProvinceView`, `FeatureView`, `FeatureLayer` (göl/ülke), `TurkeyMap` (deniz, gölge, kamera rengi), `ReliefLayer` ve `MapLabelLayer` bunu her karede okuyup renklerini soluk griye karıştırır. Renkler `MapPalette` "Sınav modu" başlığında. Yeni bir harita katmanı eklenirse `ExamLook.Blend`'i okumalı.
- Sinematik geçiş: `UI/ExamModeController.cs` (sinema şeritleri, buhar parçacıkları, sis, başlık kartı, `MapCameraController.ZoomPunch`, `Sfx.Vapor`). Menüler arası geçişte yalnızca yumuşak renk geçişi.
- Kurallar (`CityQuizGame.IsExam`): süre × `QuizConfig.examTimeScale` (0,8), puan × `examScoreMultiplier` (1,5), kalıcı boyama / hover / ışıltı / anlık bilgi notu / kontrol ipucu yok, ilerleme anahtarı `ProgressStore.ExamKey(id)` (`id@exam`; toplam yıldıza katılmaz). Bilgi notları "Haritayı incele"de okunur.

## Reklamlar
- Oyun kodu yalnızca `Ads/IAdService.cs` arayüzünü bilir. Şu an `Ads/MockAdService.cs` (ekranda test reklamı) bağlı.
- Strateji: **Dengeli** (kullanıcı seçimi). Yerleşimler: ödüllü = elenince "+1 can ile devam" (bölüm başına 1 kez); geçiş = sonuç ekranından ayrılırken, oturumda 2 bölümden sonra ve en az 180 sn arayla (`Ads/AdPolicy.cs`, ayarlar `QuizConfig`).
- Gerçek reklamlar: `unity:levelplay-unity-integration` skill'inin adımları izlenecek. **Ads Mediation (`com.unity.services.levelplay`) paketi `Packages/packages-lock.json`'da görünmeden LevelPlay kodu yazılmaz.** Sonra `IAdService`'i uygulayan bir LevelPlay servisi yazılıp `GameBootstrap`'ta mock yerine bağlanır.

## Sürüm
- Şu anki sürüm: **0.4.0 (Prototip)**. Kaynak: Unity Player Settings > Version (`bundleVersion`); oyunda `Core/AppInfo.cs` ile giriş ekranında ve Ayarlar'da gösterilir. Her sürümde `docs/CHANGELOG.md` güncellenir. Motor: Unity 6000.6.0f1.

## Prototip
- Herhangi bir sahnede Play → `GameBootstrap` otomatik oluşur (ya da **KPSS > Prototip Sahnesini Oluştur**). Her şey çalışma anında kurulur.
- Akış (`Core/AppFlow.cs`): Giriş ekranı → Bölüm karuseli → Oyun → Sonuç (Tekrar / Sonraki / Bölümler).
- Eğitim (`UI/StudyScreen.cs`): giriş ekranında OYNA'nın yanındaki EĞİTİM butonu. Solda konu listesi (başlık = konunun tamamı, alt satır = tür; ör. Dağlar → Kıvrım), sağda harita (panelin üstüne binmeyecek şekilde `FocusOn` viewport'u). Yerler adlarıyla (`MapLabelLayer.ShowStudy`) gösterilir, dokununca bilgi notu açılır; sağ üstteki "İSİMLERİ GİZLE/GÖSTER" butonu adları kapatır (kendini sınama, `SetStudyLabelsVisible`). Yeni konu = `BuildTopics()`'e bir satır.
- Bölümler `Resources/Data/levels.json` içinde, `groups` ile sekmelere ayrılır (İller, Bölgeler, Yer Şekilleri, Sular ve Kıyılar, İklim, Ekonomi, Beşeri, KPSS Soruları). `mode` değerleri:
  `find` (ili bul) · `region` (bölgesini seç) · `feature` + `category` (yer şekli bul: river, mountain, volcano, massif, plain, plateau, lake, dam, gulf, peninsula, unesco, park, fault) · `topic` + `set` (konu setindeki illerden biri: tarım, maden, enerji, nüfus) · `plate` + `plateMin/Max` · `neighbor` · `attribute` + `attribute` (climate, vegetation) · `featurechoice` + `category` + `attribute` (göl tipi `laketype`, ova türü `plaintype`, plato türü `plateautype`, dağ türü `mountaintype`; öğenin `subtype`'ı boşsa tür sorusunda sorulmaz) · `regionall` (bölgeyi tamamla: bölgenin bütün illeri karışık sırayla tek tek sorulur, bölge sınırı her zaman çizili; `timePerQuestion` soru başına) · `mcq` + `set` (A–E soru bankası) · `soon`.
- Tüm modlar tek soru modeliyle çalışır: `Quiz/Question.cs` (haritadan seçme / butonla seçme / hepsini bul) ve `Quiz/QuestionBuilder.cs` (bölümden soru üretir). Yeni mod = QuestionBuilder'a bir metot + levels.json.
- Konu setleri ve özellikler: `python Tools/content/build_topics.py` → `Resources/Data/topics.json` (il adları plakaya otomatik çevrilir). Soru bankası: `Resources/Data/questions.json` (`highlight`: `feature:<id>` ya da `plate:<no>`).
- Bölge, iklim/bitki örtüsü ve "hepsini bul" modlarında iller nötr renge çevrilir (`TurkeyMap.SetNeutral`): bölge renkleri cevabı ele vermemeli.
- İlerleme (yıldız, rekor, kilitler) `Meta/ProgressStore.cs` → PlayerPrefs. Yıldız: tamamlarsa 3 − hata (en az 1). Bir bölüm, önceki oynanabilir bölümden ≥1 yıldızla açılır. **Şu an geliştirme için tüm bölümler açık** (`GameBootstrap.UnlockAllLevelsForTesting = true`); yayından önce false yapılacak.
- Harita renkleri bölgelere göre (`Map/MapPalette.cs`); doğru bilinen il bölge renginin doygun haline boyanır.
