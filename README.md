# KPSS Geography Quiz

Unity 6 ile geliştirilen, KPSS Coğrafya konularını oyunlaştırarak çalıştırmayı amaçlayan bir eğitim oyunu.

## Gereksinimler
- **Unity 6** (Unity Hub üzerinden kurulur)
- **Git** + **Git LFS** (GitHub Desktop LFS'i kendiliğinden içerir)

## Kurulum
1. Repoyu klonla (GitHub Desktop: *File → Clone repository*).
2. Unity Hub → *Add → Add project from disk* → klonlanan klasörü seç.
3. Unity 6 ile aç.

## Prototipi çalıştırma
Unity'de herhangi bir sahnedeyken **Play** tuşuna bas. Giriş ekranı açılır → **OYNA** → bölüm seç.

- **16 bölüm:** İli Bul (bölge bölge, sonra Tüm Türkiye), Bölge Modu, Yer Şekilleri (akarsular, dağlar, volkanlar, ovalar, platolar, karma) ve 2 "Yakında".
- **İli Bul:** yazan ili haritada tıkla. **Bölge Modu:** parlayan ilin bölgesini alttaki butonlardan seç. **Yer Şekli:** adı yazan akarsu/dağ/volkan/ova/platoyu haritada bul; cevaptan sonra KPSS bilgi notu çıkar.
- Canın bitince reklam izleyip +1 canla devam edebilirsin (şimdilik test reklamı).
- Her soruda süre akar. Doğru cevap süreyi yeniler, yanlış ya da süre bitimi 1 can götürür. 3 canın biterse elenirsin.
- Yıldızlar: hatasız 3, bir hatayla 2, iki hatayla 1. Bir sonraki bölüm, öncekinden en az 1 yıldız alınınca açılır.
- Test için: **Ayarlar → Tüm bölümleri aç**.
Kontroller: sol tık ile il seç, tekerlek ile yakınlaştır, sağ tık + sürükle ile kaydır, orta tık ile görünümü sıfırla.
Puanlama, zamanlama ve harita renkleri **GameBootstrap** bileşeninden, bölümler `Assets/_Project/Resources/Data/levels.json` dosyasından değiştirilebilir.

Harita verisi: Natural Earth (kamu malı). Dağ, volkan, ova ve plato şekilleri KPSS ders haritalarına göre şematik olarak çizilmiştir (`Tools/geo/features_manual.json`).

## Klasör yapısı (Assets altında)
```
Assets/
  _Project/
    Scripts/       C# kodları
    Scenes/        Sahneler
    Prefabs/       Prefab'lar
    Data/          Soru bankası, ScriptableObject'ler
    Art/           Harita, ikon, UI görselleri
    Audio/         Ses ve müzik
    UI/            UI Toolkit / uGUI dosyaları
```

## Çalışma akışı
- Unity'de çalışmaya başlamadan önce **Pull**, bitirince **Commit + Push**.
- Unity'nin `Library/`, `Temp/`, `Logs/` klasörleri repoya gönderilmez (`.gitignore`).
- `.meta` dosyaları **mutlaka** commit edilir.
