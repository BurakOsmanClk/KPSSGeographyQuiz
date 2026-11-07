# İş Bölümü ve Çalışma Kuralları

## 1. Roller
| | **Geliştirici A: Burak** | **Geliştirici B: Ortak** |
|---|---|---|
| **Alan** | **Oyun çekirdeği ve içerik** | **Uygulama kabuğu ve platform** |
| **Sahip olduğu kod** | `Content`, `Map`, `Gameplay`, `Game.unity`, içerik araçları (`Tools/`) | `App`, `Meta`, `Services`, `UI`, `Boot.unity`, `Menu.unity`, build ayarları |
| **Sorumluluklar** | Harita (dokunmatik kontrol, render, etiketler), oyun modları, soru kaynakları, puanlama, KPSS veri setleri, içerik içe aktarma aracı, oyun içi his (juice) | Sahne akışı, UI Toolkit ekranları, kayıt ve ilerleme, ses ve titreşim, reklam (LevelPlay), IAP, analitik, Remote Config, rıza ekranları, Android/iOS build, mağaza sayfaları |
| **Ortak işler** | `Core` sözleşmeleri, oyun içi HUD (A veri modelini, B görünümü yapar), içerik doğrulama (çapraz kontrol), cihaz testleri | ← aynı |

> Roller değiştirilebilir. Önemli olan her klasörün **tek bir sahibi** olması.

## 2. Altın kurallar
1. **Sahiplik:** Sahibi olmadığın klasördeki dosyayı değiştirme. Gerekirse PR aç ve sahibini reviewer ekle ya da ondan iste.
2. **Sözleşme önce gelir:** `Core` içindeki interface'ler ve olaylar değişecekse önce küçük bir PR açılır, **iki kişi de onaylar**, sonra uygulama yazılır.
3. **Sahneler:** Bir `.unity` dosyasını yalnızca sahibi düzenler. Sahnelere mümkün olduğunca az şey konur, gerisi kod, prefab ya da UXML ile kurulur.
4. **ProjectSettings ve Packages:** Bu dosyaları değiştiren PR'ın başlığına `[settings]` eklenir ve diğer kişiye haber verilir.
5. **`.meta` dosyaları:** Her zaman commit edilir. Claude bulutta `.meta` üretemez. Unity açıldığında oluşan `.meta` dosyalarını **dosyanın sahibi** commit eder.
6. **Küçük PR:** Bir PR tek bir iş olmalı, mümkünse 400 satırın altında, açıklamasında "nasıl test edilir" bölümü bulunmalı.

## 3. Git akışı
```
main  ←── PR (diğer kişinin onayı) ←── a/feature-adi   (Burak)
                                   ←── b/feature-adi   (Ortak)
```
- **`main` her zaman derlenir ve oynanır.** Doğrudan `main`'e push yapılmaz (M0 sonrası GitHub'da branch koruması açılır).
- **Branch adları:** `a/touch-input`, `b/save-system`, `ab/core-contracts` (ortak iş).
- **Commit mesajları:** İngilizce, emir kipinde kısa başlık (`Add pinch zoom to map camera`). Açıklama Türkçe olabilir.
- **PR akışı:**
  1. Branch'te çalış ve Unity'de test et.
  2. Claude ile `/code-review` çalıştır.
  3. PR aç.
  4. Diğer kişi (ve onun Claude'u) review yapar.
  5. Squash merge.
- **Günde en az bir kez** `main` çekilip kendi branch'ine birleştirilir (merge veya rebase).

## 4. Görev takibi: GitHub Issues + Projects
- **Board kolonları:** `Backlog → Bu hafta → Devam ediyor → Review → Bitti`.
- **Etiketler:** `owner:A`, `owner:B`, `area:map`, `area:ui`, `area:content`, `area:services`, `type:bug`, `type:feature`, `milestone:M0..M4`.
- **Issue şablonu:** amaç, kabul kriterleri (madde madde), etkilenen klasörler, bağımlılıklar.
- [ROADMAP.md](ROADMAP.md) içindeki her madde bir issue'ya dönüştürülür. Bunu Claude, GitHub eklentisi ya da `gh` ile yapabilir.

## 5. İki Claude Code oturumunun birlikte çalışması
İki Claude Code oturumu birbirleriyle doğrudan konuşamaz. Ortak hafızaları **repo**'dur:
| Ne | Nerede |
|---|---|
| Kalıcı kurallar | `CLAUDE.md` (iki Claude da her oturumda okur) |
| Tasarım ve mimari | `docs/GDD.md`, `docs/ARCHITECTURE.md` |
| Kim ne yapıyor | GitHub Issues ve Project board |
| Yapılan değişiklik ve devir notu | PR açıklaması ("Sıradaki adım", "Diğer tarafı etkileyen değişiklikler") |
| Hazır görev promptları | `docs/prompts/` |

**Her oturumun başında Claude'a şu söylenir:** "`git pull`, sonra `docs/` ve açık PR/Issue'lara bak. Ben **A** (veya **B**) rolündeyim."

## 6. İletişim ritmi
- **Her gün:** WhatsApp/Discord'da 3 satırlık not: dün ne yaptım, bugün ne yapacağım, neye takıldım.
- **Haftada bir (30 dk):** birlikte cihazda oynama, board'u güncelleme, bir sonraki haftanın issue'larını seçme.
- **Milestone sonunda:** Android build alınıp telefonda test edilir. Bulunan hatalar issue olarak açılır.
