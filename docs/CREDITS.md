# Kaynaklar ve Lisanslar

| Ne | Kaynak | Lisans / Atıf |
|---|---|---|
| İl sınırları, akarsular, göller, komşu ülkeler | [Natural Earth](https://www.naturalearthdata.com/) (10m admin-0/admin-1, rivers, lakes) | Kamu malı |
| Gölgeli kabartma (`turkey_relief.png`) | [Terrain Tiles – AWS Open Data](https://registry.opendata.aws/terrain-tiles/) (Mapzen terrarium; SRTM, ETOPO1 vb. açık kaynaklar) | Kaynaklara atıf gerekir: "Terrain Tiles: Mapzen / AWS Open Data; SRTM (NASA), ETOPO1 (NOAA) ve diğerleri" |
| Dağ, volkan, ova, plato, kıyı, UNESCO, milli park, fay şekilleri | Elle çizim (`Tools/geo/features_manual.json`), KPSS ders haritaları referans alınarak | Proje içi |
| Kara hudut kapıları (ad, il, sınır ülkesi, durum) | T.C. Ticaret Bakanlığı "Kara Hudut Kapılarımız" listesi (13.05.2026); konumlar elle, sınır çizgisine oturtularak (`features_manual.json` → `borderGates`) | Bilgi amaçlı, kaynak gösterilerek |
| İklim grafiği verisi (`climate.json`) | MGM, İllerimize Ait Genel İstatistik Verileri (il merkezi istasyonları, uzun yıllar ortalaması); `Tools/content/fetch_mgm_climate.py` ile çekilir | Bilgi amaçlı, kaynak gösterilerek |
| Konu setleri (tarım, maden, enerji, nüfus) | TÜİK, MTA, ETKB, TPAO, MEB (her setin `source` alanında) | Bilgi amaçlı, kaynak gösterilerek |
| Ses efektleri | Kodla üretilir (`Core/SfxPlayer.cs`) | Proje içi |

> Uygulama içinde (Ayarlar > Hakkında) ve mağaza açıklamasında bu atıflar yer almalıdır.
