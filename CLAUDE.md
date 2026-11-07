# CLAUDE.md

## Proje
KPSS Coğrafya konularına yönelik eğitim/quiz oyunu. Motor: **Unity 6**. Dil: C#. Oyun içi metinler Türkçe.

## Çalışma düzeni
- Kullanıcı Unity Editor'ü kendi bilgisayarında çalıştırır; Claude bulut oturumunda çalışır ve Unity Editor'e erişimi yoktur.
- Değişiklikler GitHub reposu üzerinden paylaşılır: Claude kod/veri yazar ve push eder, kullanıcı pull edip Unity'de test eder.
- Claude yeni bir asset (script, json vb.) eklediğinde `.meta` dosyası oluşturmaz; Unity bunu kullanıcının makinesinde üretir ve kullanıcı commit eder. Sahne (`.unity`) ve prefab YAML'larını elle düzenlemekten kaçın; bunun yerine kullanıcıya Editor'de yapılacak adımları anlat.

## Konvansiyonlar
- Proje dosyaları `Assets/_Project/` altında.
- Namespace: `KPSSGeography` (alt namespace'ler: `.Core`, `.Map`, `.Quiz`, `.UI`, `.Data`, editör kodu `.EditorTools`).
- Input: projede yalnızca **Input System** aktif (`activeInputHandler: 1`); eski `UnityEngine.Input` API'si kullanılmaz.
- UI şimdilik kodla kurulan uGUI (legacy `Text` + `LegacyRuntime.ttf`), prefab/TMP gerektirmez.
- Soru verileri koddan ayrı tutulur (JSON veya ScriptableObject).

## İl haritası verisi
- Kaynak: Natural Earth 10m admin-1 (kamu malı) → `Tools/geo/ne_10m_admin1_turkey.geojson`.
- `python Tools/geo/build_provinces.py` ile önceden üçgenlenmiş `Assets/_Project/Resources/Data/turkey_provinces.json` üretilir (plaka kodu, Türkçe ad, coğrafi bölge, etiket noktası, mesh, sınır halkaları).

## Prototip
- Menü **KPSS > Prototip Sahnesini Oluştur** → `Assets/_Project/Scenes/CityQuizPrototype.unity` (kamera + `GameBootstrap`). Geri kalan her şey çalışma anında kurulur.
