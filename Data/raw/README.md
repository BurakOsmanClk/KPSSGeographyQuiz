# Ham veriler

Kurumlardan indirilen **orijinal** dosyalar burada, **hiç değiştirilmeden** saklanır. Oyun bu klasörü okumaz
(Unity `Assets/` dışında olduğu için içe aktarmaz). Oyunun okuduğu dosyalar `Tools/` altındaki araçlarla
bu ham dosyalardan üretilir.

## Klasör düzeni

```
Data/raw/<kurum>/<verinin yılı>/<açıklayıcı-ad>.<uzantı>
```

| Kurum klasörü | Kurum |
|---|---|
| `tuik` | Türkiye İstatistik Kurumu |
| `mgm` | Meteoroloji Genel Müdürlüğü |
| `mta` | Maden Tetkik ve Arama |
| `etkb` | Enerji ve Tabii Kaynaklar Bakanlığı / EPDK / TEİAŞ |
| `afad` | Afet ve Acil Durum Yönetimi Başkanlığı |
| `tarim` | Tarım ve Orman Bakanlığı (TEPGE dahil) |
| `ticaret` | Ticaret Bakanlığı (hudut kapıları) |
| `kultur` | Kültür ve Turizm Bakanlığı (UNESCO) |
| `meb` | MEB ders kitapları |
| `diger` | Yukarıdakilere girmeyenler (README'de açıklayın) |

Örnekler:

```
Data/raw/tuik/2025/adnks_il_nufus.xlsx
Data/raw/tuik/2024/bitkisel_uretim_il.xlsx
Data/raw/mgm/2025/il_istatistikleri.json
Data/raw/ticaret/2026/kara_hudut_kapilari_2026-05-13.pdf
```

## Kurallar

1. Dosya adı küçük harf, Türkçe karakter yok, boşluk yerine `_`.
2. Dosyanın içi **değiştirilmez**. Düzeltme gerekiyorsa aracın içinde yapılır ve aracın içinde yorumla açıklanır.
3. Her yeni dosya için `docs/content/VERI_KAYNAKLARI.md` kataloğuna bir satır eklenir (nereden, hangi tarihte
   indirildi, hangi araç kullanıyor).
4. Tercih sırası: **CSV / XLSX** > JSON > PDF. Tablolar mümkünse Excel olarak indirilir.
5. PDF, resim vb. büyük dosyalar Git LFS'e gider (`.gitattributes`). Claude'un bulut oturumu LFS'e yükleyemez;
   PDF'leri bilgisayarınızdan GitHub Desktop ile ekleyin.
