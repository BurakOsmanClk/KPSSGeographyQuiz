# Veri Kaynakları Kataloğu

Oyundaki her verinin **nerede durduğu, nereden geldiği, hangi araçla üretildiği ve ne zaman güncellenmesi gerektiği**.
Kurallar: `CLAUDE.md` → "Veri düzeni". Veri ekleyen ya da güncelleyen herkes bu tabloyu da günceller.

Akış: **ham dosya** (`Data/raw/…`) → **araç** (`Tools/…`) → **oyun dosyası** (`Assets/_Project/Resources/Data/…`).
Denetim: `python Tools/content/check_data.py` (push etmeden önce 0 hata olmalı).

## Oyun dosyaları

| Oyun dosyası | İçerik | Üreten araç | Ham kaynak | Kaynak kurum / yıl | Güncelleme sıklığı | Durum |
|---|---|---|---|---|---|---|
| `turkey_provinces.json` | 81 il: sınır, plaka, bölge, komşular, dış hat | `Tools/geo/build_provinces.py` | `Tools/geo/ne_10m_admin1_turkey.geojson` | Natural Earth 10m (kamu malı) | Sabit | ✅ |
| `turkey_features.json` | Akarsu, göl, dağ, volkan, masif, ova, plato, kıyı, UNESCO, milli park, fay, baraj, komşu ülke, sınır kapısı | `Tools/geo/build_features.py` | `Tools/geo/features_manual.json` + Natural Earth (akarsu, göl, ülke) | Natural Earth; elle girilenler KPSS ders kaynakları; sınır kapıları Ticaret Bakanlığı 13.05.2026 | Sabit; sınır kapıları, UNESCO ve milli parklar **yılda bir kontrol** | ✅ |
| `Textures/turkey_relief.png` | Gölgeli kabartma | `Tools/geo/build_relief.py` | `Tools/geo/cache/` (git'e girmez) | AWS Terrain Tiles (SRTM, ETOPO1) | Sabit | ✅ |
| `topics.json` | Tarım, hayvancılık, arıcılık, maden, enerji, nüfus setleri; iklim ve bitki örtüsü grupları | `Tools/content/build_topics.py` (veri şimdilik **aracın içinde**) | — (elle) | Karışık: TÜİK, TEPGE, MTA, ETKB, MEB/KPSS; her kaydın `source` alanında | Tarım, enerji, nüfus **her yıl** | ⚠️ Nüfus **TÜİK ADNKS 2023**; birçok kayıtta yıl yok (`check_data.py` uyarıları) |
| `topics_ek.json` | Konum, enlem–boylam, dış kuvvetler, doğal afetler, sanayi, ulaşım, göç ve yerleşme, kalkınma projeleri, iklim (sıcaklık–yağış) | `Tools/content/build_topics_ek.py` | — (elle) | KPSS, AFAD, proje idareleri, Natural Earth; her kaydın `source` alanında | Göç: her yıl; diğerleri sabit | ⚠️ `kop` kimliği iki kez kullanılmış (Kop Geçidi, KOP Projesi) |
| `climate.json` | 11 il merkezinin aylık sıcaklık ve yağış ortalaması (iklim grafiği soruları) | `Tools/content/fetch_mgm_climate.py` (PR #24) | MGM sitesi (doğrudan çekilir) | MGM il istatistikleri, uzun yıllar (≈1927–2025) | Yılda bir (isteğe bağlı) | ✅ PR #24 birleşince |
| `questions.json` | A–E soru bankası; numaralı harita, maddeli, ok/güzergâh, toprak eşleştirme soruları | — (**elle**, özgün sorular) | — | Bilgiler KPSS kaynaklarından; soru metinleri özgün (telif) | Gerektikçe | ✅ |
| `levels.json` | Bölümler ve sekmeler | — (elle) | — | — | Gerektikçe | ✅ |
| `levels_ek.json` | Ek bölümler (çakışma olmasın diye ayrı dosya) | — (elle) | — | — | Gerektikçe | ✅ |
| `Textures/Bosses/*.png` | Boss görselleri (PR #22) | — | Canva ile üretildi | Canva | — | ✅ PR #22 birleşince |

## Ham dosyalar (`Data/raw/`)

| Dosya | Kurum | Verinin yılı | İndirildiği tarih | Kaynak adres | Kullanan araç |
|---|---|---|---|---|---|
| _(henüz yok)_ | | | | | |

## Güncelleme takvimi (hatırlatma)

| Ne | Ne zaman yayımlanır | Nereden |
|---|---|---|
| İl nüfusları (ADNKS) | Her yıl şubat başı | TÜİK Veri Portalı → "Adrese Dayalı Nüfus Kayıt Sistemi Sonuçları" → İstatistiksel Tablolar |
| Bitkisel / hayvansal üretim (il bazında) | Yılda bir (TÜİK takvimine bakın) | TÜİK Veri Portalı; Bölgesel İstatistikler (biruni.tuik.gov.tr) |
| İklim normalleri | Yeni normal dönemi nadiren | MGM → Resmi İstatistikler → İllerimize Ait Genel İstatistik Verileri |
| Enerji kurulu gücü, petrol / doğal gaz üretimi | Yılda bir | ETKB, EPDK, TEİAŞ yıllık raporları |
| UNESCO Dünya Mirası | Her yıl temmuz | Kültür ve Turizm Bakanlığı |
| Kara hudut kapıları | Değiştikçe | Ticaret Bakanlığı |

Not: TÜİK Veri Portalı'nın veri servisi ve İstatistik Yıllığı sitesi giriş (e-Devlet) istiyor; bu dosyaları bilgisayarınızdan
indirip `Data/raw/tuik/<yıl>/` altına koyun. MGM sayfaları girişsiz çekilebiliyor.
