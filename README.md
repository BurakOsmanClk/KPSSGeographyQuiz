# KPSS Geography Quiz

Unity 6 ile geliştirilen, KPSS Coğrafya konularını oyunlaştırarak çalıştırmayı amaçlayan bir eğitim oyunu.

## Gereksinimler
- **Unity 6** (Unity Hub üzerinden kurulur)
- **Git** + **Git LFS** (GitHub Desktop LFS'i kendiliğinden içerir)

## Kurulum
1. Repoyu klonla (GitHub Desktop: *File → Clone repository*).
2. Unity Hub → *Add → Add project from disk* → klonlanan klasörü seç.
3. Unity 6 ile aç.

## Prototipi çalıştırma: İli Bul
1. Unity'de üst menüden **KPSS → Prototip Sahnesini Oluştur**.
2. **Play** tuşuna bas.

Kurallar: rastgele 20 il, her il için 10 sn. Doğru cevap süreyi yeniler, yanlış tıklama ya da süre bitimi 1 hata sayılır. 3 hatada elenirsin.
Kontroller: sol tık ile il seç, tekerlek ile yakınlaştır, sağ tık + sürükle ile kaydır, orta tık ile görünümü sıfırla.
Kurallar ve renkler sahnedeki `Game` objesinin **GameBootstrap** bileşeninden değiştirilebilir.

Harita verisi: Natural Earth (kamu malı).

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
