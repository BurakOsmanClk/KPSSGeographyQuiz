# Yol Haritası: v1.0 (6 hafta)

> **A** = Burak (oyun çekirdeği ve içerik) · **B** = Ortak (uygulama kabuğu ve platform) · **AB** = ortak iş
> Her madde bir GitHub Issue olur. Kabul kriteri karşılanmadan madde "Bitti" sayılmaz.

## M0: Omurga (Gün 1–4)
**Hedef:** İki kişi de aynı iskelet üzerinde, birbirine dokunmadan çalışmaya başlayabilsin.

| # | İş | Sahip | Kabul kriteri |
|---|---|---|---|
| 0.1 | Prototip editörde ve Android cihazda çalışıyor | AB | Telefonda 1 tur oynanabiliyor |
| 0.2 | Ortağı repoya collaborator olarak ekle, iki Claude Code'u kur (bkz. TOOLING.md) | A | Ortak branch açıp PR atabiliyor |
| 0.3 | `Core` sözleşmeleri: `Services`, `GameEvents`, servis arayüzleri, olay struct'ları | AB | PR iki onayla merge edildi |
| 0.4 | asmdef'ler ve klasör yapısı (`Data→Content`, `Quiz→Gameplay` taşıma) | A | Proje derleniyor, testler çalışıyor |
| 0.5 | `MapQuizSession` + `IQuestionSource` + `ScoreCalculator` (prototipten çıkarım) + EditMode testleri | A | İli Bul eskisi gibi oynanıyor, testler yeşil |
| 0.6 | `Boot → Menu → Game` sahne akışı, `AppBootstrap`, mock servisler | B | Menüdeki "Oyna" butonu oyunu açıyor, oyun bitince menüye dönülüyor |
| 0.7 | UI Toolkit temeli: `PanelSettings`, tema, font, `ScreenRouter`, safe area | B | Boş ana menü ekranı telefonda çentiğe taşmadan görünüyor |
| 0.8 | Branch koruması, issue etiketleri, Project board | B | `main`'e doğrudan push yapılamıyor |

## M1: Dikey dilim (Hafta 1–2)
**Hedef:** Telefonda "menü → seviye seç → oyna → sonuç → kayıt" döngüsü tamamlanmış olsun.

| # | İş | Sahip |
|---|---|---|
| 1.1 | Dokunmatik kontroller: dokun, pinch, sürükle, çift dokun; `IMapInput`; dokunma toleransı | A |
| 1.2 | Harita etiketleri (yakınlaştırınca il adları, sadece öğrenme modunda) | A |
| 1.3 | `LevelDefinition`/`LevelCatalog` SO'ları, İli Bul için 7 bölge seviyesi + "Tüm Türkiye" | A |
| 1.4 | `IHudModel` ve olaylar (HUD'un veri sözleşmesi) | A |
| 1.5 | `GameHud.uxml`: hedef, süre çubuğu, canlar, puan, seri, duraklat butonu | B |
| 1.6 | `LevelSelect` ve `Results` ekranları (yıldız animasyonu, tekrar oyna, menü) | B |
| 1.7 | `SaveService` (JSON, atomik yazma, versiyon) + yıldız/rekor kaydı | B |
| 1.8 | `AudioService` + `HapticsService` + ayarlar ekranı | B |
| 1.9 | Android build ayarları: paket adı, ikon, keystore, yatay ekran, IL2CPP/ARM64 | B |
| 1.10 | Prototipteki uGUI HUD'un kaldırılması | A |

## M2: İçerik ve ilerleme (Hafta 3–4)
**Hedef:** KPSS değeri: konu haritaları ve tekrar oynama motivasyonu.

| # | İş | Sahip |
|---|---|---|
| 2.1 | `TopicSet` SO + CSV içe aktarma aracı (**KPSS → İçeriği İçe Aktar**) | A |
| 2.2 | `TopicSetSource` (çoklu doğru il), yanlışta bilgi notu gösterimi | A |
| 2.3 | En az 15 konu seti (tarım, maden, enerji, turizm, nüfus) + kaynak bilgisi | A (veri) + B (çapraz kontrol) |
| 2.4 | `RegionSource` (Bölge Modu) | A |
| 2.5 | `DailyChallengeSource` (tarih tohumlu) | A |
| 2.6 | İlerleme: XP, oyuncu seviyesi, konu ustalığı %, seviye kilitleri | B |
| 2.7 | Günlük seri + günlük görev, istatistik ekranı (en çok karıştırılan iller) | B |
| 2.8 | Mod/konu seçim ekranları, ilk açılış yönlendirmesi (3 adımlı mini tutorial) | B |
| 2.9 | Juice turu: geçiş animasyonları, sonuç ekranı efektleri, sesler (`unity:craft-game-floors`) | AB |

## M3: Gelir ve servisler (Hafta 5)
| # | İş | Sahip |
|---|---|---|
| 3.1 | UGS projesi, Analytics olayları (huni), Remote Config anahtarları | B |
| 3.2 | LevelPlay: ödüllü reklam (devam et, seri dondur), geçiş reklamı (sıklık sınırlı), test modu | B |
| 3.3 | Unity IAP: `premium`, `no_ads`, geri yükleme, mağaza ekranı, premium kilitleri | B |
| 3.4 | Rıza ekranı (KVKK/GDPR), iOS ATT, gizlilik politikası (GitHub Pages) | B |
| 3.5 | Oyuna "devam et" akışı (reklam sonrası +1 can) ve premium içerik bayrakları | A |
| 3.6 | Performans: düşük seviye Android'de profil çıkarma, GC sıfırlama, ilk açılış süresi | A |

## M4: Cila ve yayın (Hafta 6)
| # | İş | Sahip |
|---|---|---|
| 4.1 | Hata avı (bug bash): iki kişi, iki cihaz, kontrol listesi | AB |
| 4.2 | iOS build (Mac + Xcode ya da Unity Build Automation), TestFlight | B |
| 4.3 | Google Play iç test kanalı → kapalı test (en az 12 test kullanıcısı / 14 gün kuralına dikkat!) | B |
| 4.4 | Mağaza sayfaları: başlık, açıklama (ASO), ekran görüntüleri, tanıtım görseli | AB |
| 4.5 | İçerik son kontrolü (her setin kaynağı doğrulandı) | AB |
| 4.6 | v1.0 yayın + sürüm etiketi (`v1.0.0`) | AB |

> ⚠️ **Google Play:** 2023 sonrası açılan **kişisel** geliştirici hesaplarında, üretime çıkmadan önce **en az 12 test kullanıcısıyla 14 gün kapalı test** zorunlu. Bu yüzden kapalı test M3 sonunda başlatılmalı, yoksa 6 haftalık hedef kayar.

## v1.1+ fikir havuzu
Liderlik tabloları ve bulut kaydı (UGS), Play Games / Game Center başarımları, çoktan seçmeli KPSS soru bankası, plaka ve komşu il modları, akarsu/dağ/göl geometrisi, haftalık turnuva, bildirimler (günlük seri hatırlatma).
