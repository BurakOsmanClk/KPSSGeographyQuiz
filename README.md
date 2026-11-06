# KPSS Geography Quiz

Unity 6 ile geliştirilen, KPSS Coğrafya konularını oyunlaştırarak çalıştırmayı amaçlayan bir eğitim oyunu.

## Gereksinimler
- **Unity 6** (Unity Hub üzerinden kurulur)
- **Git** + **Git LFS** (GitHub Desktop LFS'i kendiliğinden içerir)

## Kurulum
1. Repoyu klonla (GitHub Desktop: *File → Clone repository*).
2. Unity Hub → *Add → Add project from disk* → klonlanan klasörü seç.
3. Unity 6 ile aç.

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
