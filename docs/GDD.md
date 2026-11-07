# Oyun Tasarım Dokümanı (GDD): KPSS Coğrafya

> Sürüm: v0.1 (taslak) · Hedef: **v1.0, 4–6 hafta** · Platform: **Android + iOS** · Model: **Ücretsiz + reklam + premium**

## 1. Vizyon
KPSS'ye hazırlanan adayların coğrafya konularını **Türkiye haritası üzerinde oynayarak** ezberlemesini sağlayan, kısa oturumlu (2–5 dk) bir mobil oyun.
Ders kitabını tekrar etmek yerine "Fındık en çok hangi illerde üretilir?" sorusunu haritada **tıklayarak** cevaplamak.

**Tek cümlelik özet:** "Seterra hızında, Duolingo ilerleme hissinde, KPSS müfredatına özel harita oyunu."

## 2. Hedef kitle
- 18–35 yaş arası KPSS (Lisans / Önlisans / Ortaöğretim) adayları.
- Toplu taşımada veya mola arasında telefondan oynayan, kısa tekrar yapmak isteyen kullanıcılar.
- Çocuklara yönelik değildir. Bu yüzden Google Play "Families" politikası kapsamına girmez.

## 3. Temel oyun döngüsü (core loop)
```
Konu/Seviye seç → Tur oyna (10–20 soru, süre + 3 can) → Sonuç (puan, yıldız, XP, doğru/yanlış özeti)
      ↑                                                                        ↓
      └──── İlerleme: yeni seviyeler açılır, konu ustalığı (%), günlük seri ←──┘
```
- **Oturum içi gerilim:** soru başına süre, 3 can, seri çarpanı.
- **Oturumlar arası motivasyon:** yıldızlar, konu ustalığı yüzdesi, günlük görev ve seri, rekor.
- **Öğrenme:** yanlış cevapta doğru il gösterilir ve kısa bir **bilgi notu** verilir (ör. "Rize: Türkiye çay üretiminin ~%60'ı").

## 4. Oyun modları (v1.0)
Bütün modlar aynı harita motorunu ve oturum sistemini (`MapQuizSession`) kullanır. Modlar arasındaki fark sadece **soru kaynağıdır**.

| Mod | Soru | Cevap | v1.0? |
|---|---|---|---|
| **İli Bul** | "KONYA'yı bul" | Tek il | ✅ (prototipi hazır) |
| **Bölge Modu** | "Bu il hangi bölgede?" / "Ege'nin illerini bul" | Bölge butonları / birden çok il | ✅ |
| **Konu Haritaları** | "Fındık üretiminde öne çıkan illerden birini seç" | Doğru kümedeki herhangi bir il | ✅ (KPSS'nin asıl değeri) |
| **Günlük Meydan Okuma** | Herkes için aynı (tarih tohumlu) 10 karışık soru | Karışık | ✅ |
| Komşu iller, plaka kodu | "34 hangi il?" / "Ankara'ya komşu bir il" | Tek il | v1.1 |
| Çoktan seçmeli KPSS soruları | Metin soru, A–E | Şık | v1.1+ |
| Akarsu, dağ, göl (çizgi/nokta verisi) | Ek geometri gerekir | Nokta/çizgi | v1.2+ |

### Konu Haritaları: v1.0 içerik hedefi (en az 15 set)
Her set il bazlıdır, ek geometri gerekmez. **Her veri kaynaklı olmalıdır** (bkz. §8).
- **Tarım:** fındık, çay, pamuk, zeytin, tütün, şeker pancarı, buğday, ayçiçeği, muz, kayısı, incir, üzüm
- **Hayvancılık:** büyükbaş, küçükbaş, ipek böceği, arıcılık
- **Madenler:** bor, krom, bakır, demir, linyit, taş kömürü, boksit, mermer
- **Enerji:** HES (barajlar), termik, rüzgâr, jeotermal, güneş
- **Sanayi ve turizm:** UNESCO Dünya Mirası alanları, milli parklar
- **Nüfus:** en kalabalık ve en az nüfuslu iller, göç alan ve veren iller

## 5. İlerleme sistemi
- **Seviye = (mod + konu + zorluk).** Her seviyede 1–3 yıldız kazanılır (puan eşiklerine göre).
- **Konu ustalığı %:** son N turda o konudaki illerde doğru cevap oranı.
- **XP ve oyuncu seviyesi:** her tur XP verir, seviye atlayınca kozmetik ödül (harita renk temaları) açılır.
- **Günlük seri:** her gün en az 1 tur. Seri dondurma, ödüllü reklamla alınabilir.
- **İstatistikler:** en çok karıştırılan iller, isabet oranı, toplam oynama süresi.
- **Başarımlar:** yerel başarımlar (v1.0). Game Center / Play Games entegrasyonu v1.1'de.

## 6. Gelir modeli
| Ürün | Detay |
|---|---|
| **Ödüllü reklam** | Elenince "+1 can ile devam" (tur başına 1 kez), seri dondurma, günlük görevde 2x XP |
| **Geçiş reklamı** | Tur sonlarında; ilk 3 turda gösterilmez, en az 3 dk aralık, Remote Config ile ayarlanır |
| **Premium (tek seferlik IAP)** | Reklamsız + tüm konu setleri + harita temaları |
| **Reklam kaldır (IAP)** | Sadece reklamları kaldırır (daha ucuz seçenek) |

- **Ücretsiz içerik:** İli Bul, Bölge Modu, Günlük Meydan Okuma ve her kategoriden 1–2 konu seti. Premium açılınca tüm setler gelir.
- **Uyum:** KVKK ve GDPR rıza ekranı, iOS ATT izni, gizlilik politikası sayfası (GitHub Pages).

## 7. Sanat, ses ve his
- **Stil:** düz (flat) vektör, yumuşak pastel tonlar, yuvarlak köşeli UI. Harita zaten kodla çiziliyor, ayrı harita görseli gerekmez.
- **Font:** Türkçe karakter desteği olan OFL lisanslı bir font (ör. Nunito, Inter veya Baloo 2).
- **Juice:** doğru cevapta ilin büyüyüp küçülmesi ve puanın uçması, yanlışta ekran sarsıntısı, haptik titreşim, sonuç ekranında yıldız animasyonu.
- **Ses:** CC0 efektler (ör. Kenney), sakin bir menü müziği. Ses ve titreşim ayarlardan kapatılabilir.
- **Yönlendirme:** yatay (landscape) ekran, çünkü Türkiye haritası yatayda daha iyi görünüyor. Güvenli alan (safe area / çentik) desteği olacak.

## 8. İçerik doğruluğu (kritik)
KPSS adayı yanlış bilgiyle çalışırsa oyun zarar verir. Bu yüzden:
- Her konu setinde `source` (kaynak) alanı zorunludur: MEB ders kitabı, TÜİK, MTA, ETKB, TKİ, UNESCO.
- Her set iki kişi tarafından kontrol edilir: hazırlayan + diğer geliştirici.
- Uygulama içinde "Hata bildir" butonu olur (v1.0: e-posta / form linki).

## 9. Kontroller (mobil)
- **Dokun:** il seç (sürükleme eşiğinin altındaki dokunuşlar seçim sayılır).
- **İki parmak (pinch):** yakınlaştır. **Tek parmakla sürükle:** kaydır. **Çift dokun:** o noktaya yakınlaştır ya da görünümü sıfırla.
- **Küçük iller için:** yakınlaştırınca il etiketleri görünür. İsteğe bağlı olarak dokunma noktasına en yakın il seçilebilir (tolerans).
- **Editörde test:** mouse ile aynı davranış (sol tık = dokun, tekerlek = pinch).

## 10. v1.0 kapsam dışı (bilinçli olarak)
Çok oyunculu mod, hesap sistemi, bulut kaydı, liderlik tabloları (v1.1 adayı), çoktan seçmeli soru bankası, akarsu ve dağ geometrisi, çoklu dil desteği.
