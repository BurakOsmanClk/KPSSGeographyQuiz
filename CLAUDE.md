# CLAUDE.md

## Proje
KPSS Coğrafya konularına yönelik eğitim/quiz oyunu. Motor: **Unity 6**. Dil: C#. Oyun içi metinler Türkçe.

## Çalışma düzeni
- Kullanıcı Unity Editor'ü kendi bilgisayarında çalıştırır; Claude bulut oturumunda çalışır ve Unity Editor'e erişimi yoktur.
- Değişiklikler GitHub reposu üzerinden paylaşılır: Claude kod/veri yazar ve push eder, kullanıcı pull edip Unity'de test eder.
- Claude yeni bir asset (script, json vb.) eklediğinde `.meta` dosyası oluşturmaz; Unity bunu kullanıcının makinesinde üretir ve kullanıcı commit eder. Sahne (`.unity`) ve prefab YAML'larını elle düzenlemekten kaçın; bunun yerine kullanıcıya Editor'de yapılacak adımları anlat.

## Konvansiyonlar
- Proje dosyaları `Assets/_Project/` altında.
- Namespace: `KPSSGeography` (alt namespace'ler: `.Core`, `.Quiz`, `.UI`, `.Data`).
- Soru verileri koddan ayrı tutulur (JSON veya ScriptableObject).
