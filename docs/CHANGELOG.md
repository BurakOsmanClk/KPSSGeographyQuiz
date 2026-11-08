# Sürüm Geçmişi

Sürüm numarası Unity'de **Player Settings > Version** alanındadır (`ProjectSettings.asset > bundleVersion`) ve oyunda
giriş ekranının sağ altında ile **Ayarlar** penceresinde görünür (`Core/AppInfo.cs`).
Biçim: `ANA.KÜÇÜK.YAMA` — yeni özellik = KÜÇÜK +1, hata düzeltmesi = YAMA +1, mağaza yayını = 1.0.0.


## Yayınlanmamış
- İller sekmesi: Ege ve Akdeniz ayrı bölümler. Bölge bölümleri o bölgenin bütün illerini sorar (Marmara 11, Ege 8, Akdeniz 8,
  Karadeniz 18, İç Anadolu 13, Doğu Anadolu 14, Güneydoğu Anadolu 9; yedi bölge yedi ayrı bölüm). Tüm Türkiye 81 soru; Plakalar 01–40 ve 41–81 bütün plakalar.
- Komşu İller: sorulan il haritada parlatılmaz (yalnızca adı yazar); doğru cevaptan sonra haritada belirir.
## 0.4.0 — Prototip (şu anki sürüm)
- **Sınav modu:** bölüm seçim ekranından açılıp kapanır. Açılınca renkli, isimli harita "buharlaşarak" soluk gri, boş KPSS
  haritasına dönüşür (sinema şeritleri, yükselen buhar, kamera yaklaşması, başlık kartı).
- Sınavda: üzerine gelince vurgulama yok, doğru bilinen iller boyanmaz, deniz/ülke/il adları yok, anlık bilgi notu yok,
  süre %20 kısa, puan ×1,5; yıldız ve rekorlar normal moddan ayrı kaydedilir.
- Tarım soruları "en fazla üretildiği il" kalıbına çevrildi; Tarla Ürünleri (12) ve Meyve-Bahçe (12) bölümleri; yeni ürünler: arpa, mısır, patates, nohut, kırmızı mercimek, limon. Kaynaklar: `docs/content/TARIM.md`.
- Hayvancılık hayvan türüne göre soruluyor (sığır, koyun, kıl ve tiftik keçisi, manda); yeni "Arıcılık ve İpek Böceği" bölümü (kovan, bal, çam balı, Anzer balı, ipek böceği). Kaynaklar: `docs/content/HAYVANCILIK.md`.
- Yer şekli bölümlerinde (dağ, ova, plato, akarsu, göl vb.) il sınırları ve şehir adları gizlendi; harita sadece Türkiye'nin dış hattıyla boş silüet.
- Fiziki içerik genişletildi: 45 ova, 13 plato, 41 dağ; Ova/Plato/Dağ Türleri bölümleri.

## 0.3.0 — Prototip
- 41 bölüm, 8 sekme: plaka, komşu iller, "bölgeyi tamamla", iklim ve bitki örtüsü, göller ve göl tipleri, barajlar,
  kıyı şekilleri, fay hatları, tarım/hayvancılık/maden/enerji, nüfus, UNESCO ve milli parklar, A–E KPSS soruları.
- Gölgeli kabartma, komşu ülkeler, deniz/boğaz/il adları, alan biçimli dağlar, akarsu yatağından ovalar.
- "Haritayı incele", renk körü dostu palet, büyük yazı, efektler (bölge sınırı çizimi, ışıltı).
- Reklam altyapısı (test reklamı): +1 can ile devam, sıklık sınırlı geçiş reklamı.

## 0.2.0 — Prototip
- Giriş ekranı, kaydırmalı bölüm karuseli, yıldız ve kilit sistemi.
- Bölge Modu, yer şekli bölümleri (akarsu, dağ, volkan, ova, plato), bölge renkli harita.

## 0.1.0 — İlk prototip
- Türkiye il haritası, "İli Bul" modu: 20 il, süre, 3 can.
