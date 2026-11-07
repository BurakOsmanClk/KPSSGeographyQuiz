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
- UI hedefi: **UI Toolkit** (UXML/USS). Prototipteki kodla kurulan uGUI HUD geçicidir (M1'de kaldırılacak).
- Soru verileri koddan ayrı tutulur (JSON veya ScriptableObject).

## İl haritası verisi
- Kaynak: Natural Earth 10m admin-1 (kamu malı) → `Tools/geo/ne_10m_admin1_turkey.geojson`.
- `python Tools/geo/build_provinces.py` ile önceden üçgenlenmiş `Assets/_Project/Resources/Data/turkey_provinces.json` üretilir (plaka kodu, Türkçe ad, coğrafi bölge, etiket noktası, mesh, sınır halkaları).

## Prototip
- Menü **KPSS > Prototip Sahnesini Oluştur** → `Assets/_Project/Scenes/CityQuizPrototype.unity` (kamera + `GameBootstrap`). Geri kalan her şey çalışma anında kurulur.
