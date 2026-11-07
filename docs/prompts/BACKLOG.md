# Yapılacaklar Listesi: 53 Hazır Prompt

> **Nasıl kullanılır:** Her oturumun başında önce [00_ONBOARDING.md](00_ONBOARDING.md) içindeki promptu yapıştır, sonra buradan bir madde seç ve kod bloğundaki promptu yapıştır.
> **Etiketler:** **P0** = önce bunlar · **P1** = v1.0 için gerekli · **P2** = sonraki sürüm. **A** = Burak (oyun ve içerik) · **B** = Ortak (uygulama kabuğu ve platform) · **AB** = birlikte.
> **Genel kurallar:** Her madde ayrı branch'te ve ayrı PR olarak yapılır. Unity işinden önce ilgili `unity:*` skill'i yüklenir. `.meta` dosyası üretilmez. Sahne/prefab YAML'ı elle düzenlenmez.

> ✅ 23–34 tamamlandı (ilk sürüm). İçerik doğrulaması için 53. madde önemle önerilir.

## Önerilen ilk 12 (sırasıyla)
`1 → 9 → 11 → 10 → 2 → 6 → 47 → 48 → 42 → 35 → 23 → 53`

---

## 1. Temel ve teknik borç

### 1. [P0][AB] Test kontrol listesi ve hata avı
```
Projedeki mevcut tüm akışlar için docs/QA_CHECKLIST.md yaz:
- Giriş ekranı, bölüm karuseli, 3 oyun modu (İli Bul, Bölge, Yer Şekli), duraklatma, sonuç ekranı, devam teklifi, test reklamları ve ayarlar.
- Her madde: adım, beklenen sonuç, [ ] işaret kutusu. Uç durumları da ekle: oyun içinde pencere boyutu değiştirme, duraklatıp bölümler menüsüne dönme, reklam açıkken geri tuşu, ilerlemeyi sıfırlama.
Ben listeyi Unity'de ve telefonda uygulayıp sonuçları yazacağım. Sonra bulduğum hataları tek tek düzelt.
```

### 2. [P0][A] Klasör ve assembly yeniden yapılandırması (M0)
```
docs/ARCHITECTURE.md §1 ve §11'e göre yeniden yapılandır. Branch: a/restructure.
- Scripts/Data → Scripts/Content ve Scripts/Quiz → Scripts/Gameplay olarak taşı. CityQuizGame'in adını QuizSession yap.
  `git mv` kullan ve .meta dosyalarını da birlikte taşı, GUID'ler korunmalı.
- Core, Content, Map, Gameplay, Meta, Ads, UI ve Editor için .asmdef oluştur. Bağımlılıklar sadece aşağı doğru olsun.
  Gerekli yerlere Unity.InputSystem, Unity.RenderPipelines.Universal.Runtime ve UnityEngine.UI referanslarını ekle.
- Namespace'leri güncelle, CLAUDE.md'deki "Konvansiyonlar" bölümünü yeni duruma göre düzelt.
- Davranış birebir aynı kalmalı. Unity'de kontrol etmem gereken adımları madde madde yaz.
```

### 3. [P1][AB] Core sözleşmeleri ve mevcut koda uyarlama
```
docs/ARCHITECTURE.md §3'teki Services (service locator) ve GameEvents (struct tabanlı event bus) yapılarını yaz.
Mevcut kodda doğrudan referansla kurulan bağlantıları (AppFlow, GameBootstrap, QuizHud ↔ game) sırayla bu yapıya taşı:
SessionStarted, QuestionAnswered, SessionEnded ve ContinueOffered olaylarını yayınla.
PR başlığı [contract] olsun. Diğer geliştiricinin onayını iste.
```

### 4. [P1][A] EditMode testleri
```
Assets/_Project/Tests/EditMode altına test asmdef'i kur ve şu testleri yaz:
- Puan hesabı: süre bonusu ve seri çarpanının üst sınırı. Puanlamayı saf C# bir ScoreCalculator'a çıkar.
- Soru seçimi: tekrar olmamalı, bölge filtresi ve soru sayısı sınırı doğru çalışmalı.
- ProvinceView.Contains: Ankara, İstanbul ve Van merkez noktaları kendi illerinde olmalı.
- FeatureView.DistanceInPixels: nokta, çizgi ve alan türleri için.
- ProgressStore: kilit kuralı, yıldızın asla düşmemesi, rekor.
- AdPolicy: ilk N bölümde reklam çıkmamalı, asgari süre aralığı korunmalı.
Testleri Unity'de nasıl çalıştıracağımı (Window > General > Test Runner) yaz.
```

### 5. [P1][A] QuizHud'u parçalara ayır
```
UI/QuizHud.cs şu an yaklaşık 830 satır. Davranışı değiştirmeden şu bileşenlere ayır:
HudTopBar (hedef kartı, süre, puan, canlar), RegionChoiceBar, PauseScreen, ContinueOfferScreen, ResultScreen, MapNameTag, FloatingText.
QuizHud yalnızca bunları kurup oyun olaylarına bağlasın. Her bileşen kendi dosyasında olsun.
```

### 6. [P0][A] Performans ve çöp toplama (GC) temizliği
```
Her karede bellek ayıran (allocation) ve gereksiz iş yapan kodları temizle:
- LevelSelectScreen.LayoutCards: Array.Sort + Comparer<int>.Create her kare yeni nesne üretiyor. Önceden ayrılmış bir comparer kullan
  ya da sıralamayı sadece seçim değişince yap.
- QuizHud: timerText.ToString("0.0") her kare çalışıyor. Sadece görünen değer değişince güncelle. Puan metni için de aynısını yap.
- ProvinceView / FeatureView: 81 il ve 79 yer şekli her kare Update çalıştırıyor. Animasyonu olmayanların Update'ini atla
  ya da merkezi bir güncelleyiciye geç.
- MapPointer: fare hareket etmediyse FindAt çağrılmasın.
Sonra bana Profiler'da (Deep Profile kapalı) bakmam gereken 3 metriği ve hedef değerleri yaz.
```

### 7. [P1][B] Kayıt sistemini JSON'a taşıma
```
Meta/ProgressStore PlayerPrefs kullanıyor. Bir SaveService yaz: persistentDataPath/save.json, version alanı,
atomik yazma (tmp dosya + yerine taşıma), bozuk dosyada yedekten yükleme.
Mevcut "KPSS_Progress_v1" ve "KPSS_Muted" PlayerPrefs verilerini ilk açılışta yeni dosyaya aktar.
Uygulama arka plana alınınca (OnApplicationPause) kaydetsin. EditMode testi de yaz.
```

### 8. [P2][B] Gereksiz paketleri kaldırma
```
Packages/manifest.json'daki kullanılmayan paketleri listele (AI Assistant, AI Inference, AI Navigation, Collab Proxy vb.)
ve her biri için koddan referans verilip verilmediğini kontrol et. Kullanılmayanları kaldır.
PR başlığı [settings] olsun. Unity'yi açınca packages-lock.json'un yeniden oluşacağını ve onu da commit etmem gerektiğini yaz.
```

## 2. Mobil ve kontroller

### 9. [P0][A] Dokunmatik kontroller
```
Input System + EnhancedTouch ile IMapInput arayüzü yaz. Olaylar: Tap, Pan, PinchZoom, DoubleTap.
İki uygulaması olsun: TouchMapInput ve MouseMapInput (editör).
MapCameraController, MapPointer ve FeaturePointer doğrudan Mouse.current okumasın, IMapInput'u dinlesin.
- 10 dp'nin altındaki hareket Tap sayılsın. Pinch, iki parmağın orta noktasına doğru yaksın.
- Çift dokunma o noktaya yaklaştırsın, zaten yakınsa görünümü sıfırlasın.
- UI üzerindeki dokunuşlar haritaya geçmesin.
HUD'daki kontrol ipucu metni de mobilde "Dokun: seç • İki parmak: yakınlaştır" olsun.
```

### 10. [P0][A] Küçük hedefler için dokunma toleransı ve büyüteç
```
Yalova, Kilis, Bartın gibi küçük iller ve küçük ovalar parmakla zor seçiliyor:
- İl modunda, dokunulan nokta hiçbir ilin içinde değilse 24 dp yarıçaptaki en yakın ili seç.
- Dokunulan noktada birden fazla aday varsa (sınıra çok yakın dokunuş) parmağın üstünde küçük bir büyüteç (lens)
  göster ve parmak kaldırılınca seç.
Bu davranışı ayarlardan açıp kapatılabilir yap.
```

### 11. [P0][B] Ekran yönü, çentik (safe area) ve en-boy oranları
```
Yatay ekran kilidi (Landscape Left/Right) ayarını yap ve tüm UI kökü için bir SafeArea bileşeni yaz.
HUD, karusel ve sonuç ekranını şu oranlarda kontrol et ve gerekirse yerleşimi esnet: 16:9, 19.5:9, 20:9 (telefon), 4:3 (tablet).
CanvasScaler matchWidthOrHeight değerini orana göre dinamik ayarla.
Bölge butonları 4:3'te sığmıyorsa iki satıra geçsin.
Unity Game penceresinde bu çözünürlükleri nasıl ekleyeceğimi yaz.
```

### 12. [P1][B] Android geri tuşu ve arka plana alma
```
- Android geri tuşu: oyundaysa duraklatma menüsünü açsın, menüdeyse bir önceki ekrana dönsün, giriş ekranındaysa
  "Çıkmak istiyor musun?" sorsun. Reklam ve devam penceresi açıkken geri tuşu bunları da düzgün kapatsın.
- Uygulama arka plana alınınca (OnApplicationPause/OnApplicationFocus) oyun otomatik duraklasın.
```

### 13. [P1][B] Titreşim (haptik) geri bildirimi
```
HapticsService yaz:
- Android: VibrationEffect, kısa ve orta şiddet.
- iOS: UIImpactFeedbackGenerator için küçük bir .mm native köprü.
- Editörde hiçbir şey yapmasın.
Doğru cevapta hafif, yanlışta "error", buton basışında çok hafif titreşim olsun. Ayarlara "Titreşim: Açık/Kapalı" ekle.
```

### 14. [P1][B] Android build ayarları
```
unity:unity-cli skill'ini yükle.
- Player Settings: Company/Product adı, paket adı com.<ad>.kpsscografya, IL2CPP, ARM64, min API 24, güncel hedef API,
  sürüm 0.2.0, yatay ekran.
- KPSS/Build/Android APK (geliştirme) ve AAB (yayın) menüleri.
- Keystore nasıl oluşturulur, adım adım anlat. Keystore ve şifreler asla repoya girmesin, .gitignore'u kontrol et.
```

## 3. Harita ve görsellik

### 15. [P1][A] Gölgeli kabartma (hillshade) zemin
```
Natural Earth'ün kamu malı "Shaded Relief" (SR_HR ya da NE1_HR) rasterinden Türkiye kesitini al.
Tools/geo altına bir script yaz: illerin projeksiyonuyla hizalanmış, 2048 px genişliğinde bir PNG üretsin
(Resources/Textures/turkey_relief.png). Bu PNG'yi Türkiye maskesiyle kırp.
Unity'de haritanın üstüne ve illerin altına, çarpma (multiply) etkisiyle yarı saydam bir katman olarak çiz.
Yer şekli bölümlerinde opaklık daha yüksek, il bölümlerinde düşük olsun.
PNG'nin import ayarlarını (sıkıştırma, mipmap) bana yaz.
```

### 16. [P1][A] Komşu ülkeler, deniz ve boğaz isimleri
```
Natural Earth admin-0'dan Türkiye'ye komşu ülkelerin sınırlarını al (Yunanistan, Bulgaristan, Gürcistan, Ermenistan,
Azerbaycan/Nahçıvan, İran, Irak, Suriye, Kıbrıs). Bunları açık gri ve tıklanamaz olarak çiz.
Denizlere (KARADENİZ, EGE DENİZİ, AKDENİZ, MARMARA DENİZİ) ve boğazlara (İstanbul, Çanakkale) italik, harf aralıklı,
dünya koordinatlı etiketler ekle. Etiketler yakınlaşma düzeyine göre boyut değiştirsin.
```

### 17. [P1][A] Dağları KPSS haritalarındaki gibi alan olarak çizme
```
Dağlar şu an sırt çizgisi + üçgen sembolleriyle çiziliyor. KPSS haritalarındaki gibi kahverengi alanlar olarak çiz:
- features_manual.json'daki her dağ için "width" (km) alanı ekle.
- build_features.py'da çizgiyi buffer'layıp yumuşat ve alan olarak üret.
- Unity'de iç kısmı koyu, kenarlara doğru açılan bir renk geçişiyle çiz (köşe renkleri).
Tıklama alanı da poligon olsun. Önce/sonra karşılaştırma PNG'si üret.
```

### 18. [P1][A] Ova ve plato şekillerini gerçekçi yapma
```
features_manual.json'daki ova ve plato poligonları şematik elips gibi duruyor.
Her biri için bilinen sınırları (akarsu vadileri, kıyı çizgisi, çevreleyen dağlar) dikkate alarak 12–20 noktalı daha gerçekçi
şekiller çiz. Çukurova, kıyıya ve Seyhan/Ceyhan'a; Büyük Menderes, nehir vadisine; Konya Ovası, Tuz Gölü havzasına uysun.
Her şekli il sınırları, akarsular ve göllerle birlikte çizen bir kontrol PNG'si üret ve bana göster.
Emin olmadığın şekilleri "doğrulanacak" diye işaretle.
```

### 19. [P2][A] Yakınlaşınca il adları
```
Keşfet ve öğrenme modlarında yakınlaşma düzeyine göre il adlarını göster: uzakta yalnız büyük iller, yakında hepsi.
Etiketler üst üste binmesin (basit çakışma testi).
Oyun modlarında etiketler kapalı kalsın. Plaka kodunu küçük yazıyla eklemek için bir seçenek olsun.
```

### 20. [P1][A] Tur sonunda "haritayı incele"
```
Sonuç ekranına "HARİTAYI İNCELE" butonu ekle. Basınca kartlar kaybolsun, harita etkileşimli kalsın:
- Turda sorulan her ilin ya da yer şeklinin adı küçük etiketlerle görünsün (doğrular yeşil, yanlışlar kırmızı çerçeveli).
- Etikete dokununca bilgi notu açılsın.
Bir "Geri" butonu sonuç ekranına dönsün.
```

### 21. [P2][A] Renk körü dostu ve yüksek kontrast palet
```
MapPalette ve Theme için ikinci bir "Renk körü dostu" palet tanımla. Doğru/yanlış için yeşil/kırmızı yerine mavi/turuncu
kullan, desen ya da ikonla destekle (doğruda ✓, yanlışta ✕ işareti).
Ayarlara palet seçimi ve "büyük yazı" seçeneği ekle.
```

### 22. [P2][A] Harita animasyonları ve efektler
```
- Bölüm başlarken seçili bölgenin sınırını çizgi halinde "çizilerek" göster (0,6 sn).
- Doğru cevapta ilin ya da yer şeklinin etrafında kısa bir parçacık/ışıltı efekti olsun. ParticleSystem yerine
  hafif bir mesh/UI efekti tercih et.
- Seri 5'e ulaşınca ekran kenarında kısa bir parlama olsun.
Efektler ayarlardan kapatılabilsin.
```

## 4. Yeni içerik ve oyun modları

### 23. ✅ [P0][A] Tarım Ürünleri bölümleri
```
"Yakında" görünen Tarım Ürünleri bölümünü aç:
- Content altında TopicSet altyapısını kur: id, title, plates[], info, source.
- Tools/content/topic_sets.csv'den okuyan bir editör menüsü yaz: KPSS/İçeriği İçe Aktar.
- Yeni mod "topic": "Fındık üretiminde öne çıkan bir il seç". Kümedeki herhangi bir il doğru sayılsın. Yanlışta tüm doğru
  iller vurgulansın ve bilgi notu gösterilsin.
- İçerik: fındık, çay, pamuk, zeytin, tütün, şeker pancarı, buğday, ayçiçeği, muz, kayısı, incir, üzüm. Her ürün için
  kaynaklı (TÜİK/MEB) ilk 3–6 il.
- Bana tablo halinde özet ver: ürün, iller, kaynak, güven derecesi.
```

### 24. ✅ [P0][A] Madenler ve Enerji bölümleri
```
Madde 23'teki altyapıyla "Madenler ve Enerji" bölümünü aç:
- Madenler: bor, krom, bakır, demir, linyit, taşkömürü, boksit, mermer.
- Enerji: hidroelektrik barajlar (Atatürk, Keban, Karakaya, Ilısu), termik, rüzgâr, jeotermal, güneş.
Barajlar için nokta türü yer şekli kullan. Kaynaklar: MTA, ETKB, TKİ, MEB. Emin olmadığın verileri ekleme.
```

### 25. ✅ [P1][A] Göller bölümü
```
Natural Earth'ten ek göller al (Burdur, Acıgöl, Salda, Hazar, Çıldır, Erçek, Manyas, İznik, Sapanca, Bafa vb.).
Natural Earth'te olmayanları features_manual.json'a elle ekle.
Yeni kategori "lake" ve "Göller" bölümü oluştur. Bilgi notunda göl tipini (tektonik, volkanik, karstik, set, kıyı set) belirt.
Göller zeminde zaten çiziliyor; bu bölümde tıklanabilir olsunlar.
```

### 26. ✅ [P1][A] Kıyı şekilleri bölümü
```
Boğazlar (İstanbul, Çanakkale), körfezler (İzmit, Gemlik, Edremit, İzmir, Kuşadası, Gökova, Antalya, İskenderun),
yarımadalar (Gelibolu, Kapıdağ, Biga, Datça, Bodrum, Sinop/İnceburun) ve adalar (Gökçeada, Bozcaada, Marmara Adası)
için yeni kategori "coast" oluştur. Nokta ya da alan olarak çiz. Bilgi notları KPSS'ye uygun olsun.
```

### 27. ✅ [P1][A] Plaka kodu modu
```
Yeni mod "plate":
- İki soru tipi olsun: (a) "34 hangi il?" (haritada bul), (b) "Konya'nın plaka kodu?" (4 seçenekli butonlar).
- Seçenekler mantıklı çeldiricilerden oluşsun: yakın numaralar, aynı bölgedeki iller.
- Seçenek butonları için RegionChoiceBar'ı genelleştir (ChoiceBar).
2 bölüm ekle: "Plakalar 1–40" ve "Plakalar 41–81".
```

### 28. ✅ [P1][A] Komşu iller modu
```
build_provinces.py'da il komşuluklarını hesapla (sınır paylaşan poligonlar) ve JSON'a "neighbors" alanı ekle.
Yeni mod: "Ankara'ya komşu bir il seç". Kümedeki her il doğru sayılsın. Yanlışta tüm komşular vurgulansın.
Zor versiyonu: "Hem Konya'ya hem Antalya'ya komşu olan il?"
```

### 29. ✅ [P1][A] İklim ve bitki örtüsü haritaları
```
İklim tipleri (Karadeniz, Akdeniz, karasal, Marmara geçiş) ve bitki örtüsü (orman, maki, bozkır, alpin çayır) için
şematik alanlar çiz (features_manual.json, "climate" ve "vegetation" kategorileri). KPSS ders kitabı haritalarına uygun olsun.
Soru: "Akdeniz iklimi alanını seç" ya da "Bu ilde hangi iklim görülür?" (seçenekli).
Kontrol PNG'si üret.
```

### 30. ✅ [P2][A] Nüfus ve şehirleşme
```
TÜİK verisine dayalı nüfus bölümü: en kalabalık 10 il, en az nüfuslu 10 il, nüfus yoğunluğu en yüksek iller,
en çok göç veren ve alan iller.
İl bazlı TopicSet'lerle yap. Haritayı nüfusa göre koroplet (renk tonu) olarak gösteren bir "bilgi haritası" görünümü de ekle.
Veri yılını ve kaynağı bilgi notuna yaz.
```

### 31. ✅ [P2][A] Milli parklar ve UNESCO alanları
```
UNESCO Dünya Mirası listesindeki Türkiye alanları (Göreme, Pamukkale, Truva, Efes, Nemrut Dağı, Safranbolu, Göbeklitepe vb.)
ve öne çıkan milli parklar (Kaçkar, Köprülü Kanyon, Munzur, Uludağ, Olimpos-Beydağları vb.) için "heritage" kategorisi.
Nokta olarak çiz, bilgi notunda il ve özelliğini yaz. Kaynak: UNESCO, Tarım ve Orman Bakanlığı.
```

### 32. ✅ [P2][A] Fay hatları ve deprem bölgeleri
```
Kuzey Anadolu Fayı, Doğu Anadolu Fayı ve Batı Anadolu (Ege) graben sistemlerini çizgi olarak çiz (features_manual.json, "fault").
Kırmızı kesikli çizgi stili kullan. Bilgi notu KPSS'ye uygun olsun (yapı, etkilediği bölgeler).
Soru: "Kuzey Anadolu Fay Hattını seç".
```

### 33. ✅ [P1][A] Bölgenin illerini bulma
```
Bölge modunun ikinci tipi: "Ege Bölgesi'nin bütün illerini bul". Tek turda bölgedeki tüm iller bulunacak.
Her doğru il boyanır, yanlış tıklama can götürür, süre tur için toplamdır. 7 bölge için 7 kısa bölüm ekle.
Bölüm kartında ilerleme gösterilsin (ör. 5/8 il).
```

### 34. ✅ [P2][A] Çoktan seçmeli KPSS soru bankası
```
Resources/Data/questions.json formatını tanımla: id, topic, text, options[5], answer, explanation, mapHighlight (opsiyonel plaka/özellik id).
Yeni mod "mcq": A–E şıklı, süreli, harita destekli. Soru ilgili il ya da yer şeklini vurgulayabilsin.
Önce 30 örnek soru yaz (iklim, yer şekilleri, nüfus, ekonomi). Her biri doğru ve kaynaklı olsun. Belirsiz soruları ekleme.
```

## 5. Öğrenme ve ilerleme

### 35. [P0][A] Keşfet modu (serbest çalışma)
```
Giriş ekranına "KEŞFET" butonu ekle. Bu modda süre ve can olmasın, harita serbestçe gezilsin:
- Üstte kategori sekmeleri: İller, Bölgeler, Akarsular, Dağlar, Volkanlar, Ovalar, Platolar.
- Bir ile ya da yer şekline dokununca alttan bir bilgi kartı açılsın (ad, bölge/il, bilgi notu).
Öğrenme için en önemli mod bu; tasarımı GDD'deki görsel dile uygun olsun.
```

### 36. [P1][A] Yanlışlarım ve aralıklı tekrar
```
Her yanlış bilinen il ya da yer şeklini kaydet (id, tür, yanlış sayısı, son görülme). Bölümler ekranının başına
"YANLIŞLARIM" kartı ekle: en çok karıştırılan 10 öğeden kısa bir tur oluştursun.
Leitner kutuları kullan: doğru bilinen öğe bir üst kutuya çıksın, kutu arttıkça daha seyrek sorulsun.
Sonuç ekranında "Bu turda karıştırdıkların" listesi olsun.
```

### 37. [P1][B] Günlük meydan okuma ve seri
```
- Her gün herkese aynı 10 karışık soru: seed = yyyyMMdd. İller, bölgeler ve yer şekilleri karışık olsun.
- Giriş ekranında "GÜNÜN SORULARI" kartı, günlük seri sayacı (alev ikonu) ve bugünkü skor gösterilsin.
- Seri bozulmasın diye "seri dondurma" ödüllü reklamla alınabilsin.
Saat dilimi ve tarih değişimi hilelerine karşı basit bir kontrol ekle.
```

### 38. [P1][B] XP, seviye, ustalık ve istatistikler
```
- XP = puan/10 + yıldız bonusu. Oyuncu seviyesi eğrisi olsun, seviye atlayınca kutlama animasyonu çıksın.
- Kategori ustalığı %: son 20 cevabın kayan ortalaması.
- "İSTATİSTİKLER" ekranı: toplam tur, isabet oranı, en uzun seri, en çok karıştırılan 5 il/yer şekli,
  kategori bazlı ustalık çubukları.
```

### 39. [P2][B] Başarımlar (rozetler)
```
20 adet yerel başarım: "81 ilin hepsini en az bir kez doğru bil", "Hatasız Tüm Türkiye", "10'lu seri",
"Tüm volkanları bul" vb. Kazanınca küçük bir bildirim (toast) çıksın, ayrıca bir Başarımlar ekranı olsun.
v1.1'de Google Play Games / Game Center'a bağlanacak şekilde soyutla.
```

### 40. [P1][A] Zorluk seviyeleri
```
Bölüm başlamadan önce Kolay / Normal / Zor seçimi:
- Kolay: +%50 süre, bölge sınırları ve ipucu açık.
- Normal: mevcut kurallar.
- Zor: −%40 süre, 2 can, hover vurgusu kapalı.
Yıldızlar zorluğa göre ayrı kaydedilsin ve kartta zorluk rozeti görünsün.
```

### 41. [P1][A] İpuçları ve jokerler
```
Oyun içinde 3 joker olsun:
- "Bölgeyi daralt": hedefin bulunduğu bölge dışını soldurur.
- "+5 saniye".
- "Yarıyı ele": yer şekli modunda adayların yarısını gizler.
Her bölümde birer hak olsun. Ek hak ödüllü reklamla alınsın (dengeli reklam stratejisine uygun, oyuncu isteğiyle).
Joker kullanılan turda en fazla 2 yıldız kazanılsın.
```

## 6. Arayüz, his ve ses

### 42. [P0][B] İlk açılış öğreticisi
```
İlk açılışta 3 adımlı etkileşimli bir öğretici göster:
1. "Bu ili bul" yazısını işaret et.
2. Haritada hedef ili parlatıp dokunmayı iste.
3. Süre ve canları açıkla.
Her modun ilk açılışında da 1 kartlık kısa açıklama çıksın. "Bir daha gösterme" tercihi kaydedilsin ve ayarlardan
sıfırlanabilsin.
```

### 43. [P1][B] Yazı tipi
```
LegacyRuntime fontu yerine OFL lisanslı Nunito (ya da Baloo 2) kullan.
uGUI'de kalacaksak TextMeshPro'ya geçişi planla: TMP Essentials, Türkçe karakter seti (ç ğ ı İ ö ş ü Ç Ğ Ö Ş Ü), fallback.
UIFactory.Label'ı yeni sisteme taşı. Font dosyasını nereden indirip nasıl import edeceğimi adım adım yaz.
```

### 44. [P1][B] Gerçek ses efektleri ve müzik
```
Kodla üretilen sesleri CC0 lisanslı gerçek seslerle değiştir (Kenney.nl ya da freesound CC0).
Gereken sesler: doğru, yanlış, tık, geri sayım, yıldız, seviye atlama, kaydırma.
Sakin bir menü müziği ve oyun müziği ekle. unity:audio-setup-mixers ile Music/SFX mikser grupları kur.
Ayarlarda ses ve müzik ayrı kaydırıcılar olsun. Lisans bilgilerini docs/CREDITS.md'ye yaz.
```

### 45. [P1][B] Bölüm ekranını gruplara ayırma
```
15+ bölüm tek karuselde uzun kalıyor. Karuselin üstüne sekmeler ekle: İLLER / BÖLGELER / YER ŞEKİLLERİ / EKONOMİ.
Her sekme kendi karuselini göstersin. Her sekmede toplam yıldız ilerlemesi görünsün.
levels.json'a "group" alanı ekle. Mevcut karusel davranışı (sürükleme, yaklaşan kart) korunsun.
```

### 46. [P2][B] UI Toolkit kararı ve geçiş
```
Mimari UI Toolkit diyor, mevcut arayüz ise kodla kurulan uGUI. İkisini mobil performans, bakım, Claude ile çalışma kolaylığı
ve font açısından karşılaştıran kısa bir karar notu yaz (docs/decisions/ui-framework.md).
UI Toolkit seçilirse unity:ui-uitk skill'ini yükle ve ilk olarak giriş ekranını Theme.cs renkleriyle UXML/USS'e taşı.
```

## 7. Gelir ve servisler (dengeli reklam stratejisi)

### 47. [P0][B] Gerçek reklamlar (LevelPlay)
```
unity:levelplay-unity-integration skill'ini yükle ve adımlarını sırayla izle. Strateji: Dengeli.
1. Önce Packages/packages-lock.json'da com.unity.services.levelplay paketinin olduğunu doğrula.
   Paket yoksa dur ve bana kurulum adımlarını söyle.
2. Android/iOS bağımlılık çözümleme adımları.
3. App Key ve ad unit ID'lerini bana sor. Koda gömme, nereye girileceğini listele.
4. Ads/IAdService arayüzünü uygulayan bir LevelPlayAdService yaz (ödüllü + geçiş reklamı) ve GameBootstrap'ta MockAdService
   yerine bağla. Editörde mock servis kalabilsin.
Test Suite ile cihazda doğrulama adımlarını da yaz.
```

### 48. [P0][B] Rıza ekranı, ATT ve gizlilik politikası
```
- İlk açılışta KVKK/GDPR rıza ekranı göster. Tercih kaydedilsin ve ayarlardan değiştirilebilsin.
- Rıza LevelPlay'e init'ten önce iletilsin (skill'deki sürüme uygun API ile).
- iOS'ta ATT izni istensin. NSUserTrackingUsageDescription metnini Türkçe yaz.
- docs/privacy-policy.md taslağı (toplanan veriler: reklam ID'si, analitik) ve GitHub Pages'te yayınlama adımları.
Bunun hukuki danışmanlık olmadığını not et.
```

### 49. [P1][B] Uygulama içi satın alma
```
unity:implement-in-app-purchases skill'ini yükle.
- Ürünler: "premium" (reklamsız + gelecekteki tüm içerik) ve "no_ads" (sadece reklamsız), ikisi de non-consumable.
- Mağaza ekranı ve geri yükleme butonu (iOS zorunlu) olsun.
- Premium kullanıcıya geçiş reklamı gösterme. Ödüllü reklam yerine doğrudan ödül ver.
- AdPolicy'yi buna göre güncelle.
```

### 50. [P1][B] Analitik ve Remote Config
```
unity:build-live-game skill'ini yükle.
- UGS Analytics olayları: app_open, tutorial_done, level_start{id}, level_end{id, stars, score, mistakes},
  continue_offered/accepted, interstitial_shown, purchase.
- Remote Config anahtarları: interstitial_after_levels, interstitial_min_interval, continue_enabled, soru süreleri.
  AdPolicy ve QuizConfig bu değerleri uzaktan alsın, yoksa varsayılanları kullansın.
```

## 8. Yayın ve kalite

### 51. [P1][AB] Mağaza hazırlığı
```
- Uygulama ikonu: SDF ikonlarıyla uyumlu, Türkiye silueti + KPSS rozeti, 1024 px.
- Splash ekranı.
- 8 ekran görüntüsü planı: hangi ekran, hangi başlık metni.
- Google Play ve App Store için Türkçe başlık (30 karakter), kısa açıklama (80) ve uzun açıklama.
  ASO anahtar kelimeleri: KPSS, coğrafya, harita, iller, Türkiye.
- Google Play kapalı test (12 test kullanıcısı / 14 gün) planı ve takvimi.
```

### 52. [P2][B] Günlük hatırlatma bildirimleri
```
Mobile Notifications paketiyle günlük seri hatırlatması ekle. Kullanıcının genelde oynadığı saatte gönderilsin
("Bugünkü 10 soru seni bekliyor!"). İlk açılışta değil, 3. oturumdan sonra izin istensin. Ayarlardan kapatılabilsin.
```

### 53. [P0][AB] İçerik doğrulama
```
KPSS adayı yanlış bilgiyle çalışmamalı. Şunları tek tek kaynaklarla (MEB ders kitabı, MTA, TÜİK, resmi kurum siteleri) kontrol et:
- Tools/geo/build_features.py içindeki akarsu bilgi notları.
- Tools/geo/features_manual.json içindeki dağ, volkan, ova ve plato bilgi notları ve konumları.
- levels.json açıklamaları.
Her madde için "doğru / düzeltilmeli / emin değilim" tablosu çıkar, düzeltmeleri yap. Emin olunmayanları bana listele.
```
