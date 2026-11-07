# Gereksinimler, Yetenekler ve Araçlar

## 1. Gereken yetenekler ve kimin üstlendiği
| Yetenek | Neden gerekli | Kim | Claude'da yardımcı skill |
|---|---|---|---|
| C# / Unity programlama | Her şey | A + B | `unity:unity-cli`, `code-review`, `simplify` |
| Oyun tasarımı ve "his" (juice) | Tekrar oynanabilirlik | A (öncü) + B | `unity:craft-game-floors` |
| Mobil UI/UX | Menüler, HUD, safe area, tek elle kullanım | B | `unity:ui`, `unity:ui-uitk` |
| KPSS coğrafya içeriği ve doğrulama | Ürünün asıl değeri | A (hazırlar) + B (kontrol eder) | (web araştırması ve kaynak gösterme) |
| Veri hattı (CSV → SO) | İçeriğin hızlı üretilmesi | A | — |
| Ses ve titreşim | Geri bildirim | B | `unity:audio-setup-mixers`, `unity:optimize-audio` |
| Reklam ve IAP | Gelir | B | `unity:levelplay-unity-integration`, `unity:implement-in-app-purchases` |
| Analitik ve Remote Config | Ölçme ve ayarlama | B | `unity:build-live-game` |
| Mobil yayıncılık (imzalama, mağaza politikaları) | Yayın | B | `unity:unity-cli` (build) |
| Performans | Düşük seviye Android | A | `unity:optimize-audio`, `unity:manage-sprite-atlas`, (ops.) `unity-perf` |
| Mağaza sayfası ve ASO | İndirme sayısı | AB | — |
| Test ve QA | Kalite | AB | Unity Test Framework |

## 2. Hesaplar ve maliyetler
| Ne | Maliyet | Kim açar | Ne zaman |
|---|---|---|---|
| GitHub (repo, Issues, Projects) | Ücretsiz | A (ortağı collaborator ekler) | M0 |
| Unity hesabı + Unity Personal lisans | Ücretsiz (yıllık gelir < 200 bin $) | İkisi de | M0 |
| Unity Cloud / UGS projesi (Analytics, Remote Config) | Ücretsiz katman yeterli | B | M3 |
| LevelPlay (reklam mediasyonu) hesabı | Ücretsiz (gelir paylaşımlı) | B | M3 |
| Google Play Console geliştirici hesabı | **25 $ (tek seferlik)** | B | **M1'de aç** (kimlik doğrulama günler sürebiliyor) |
| Apple Developer Program | **99 $ / yıl** | B | M3 |
| **Mac + Xcode** (iOS build için zorunlu) | Var olan Mac / bulut Mac / Unity Build Automation | B | M4 |
| Test cihazları | En az 1 Android (tercihen düşük seviye) + 1 iPhone | İkisi de | M1 |
| Gizlilik politikası sayfası | GitHub Pages, ücretsiz | B | M3 |

## 3. Unity paketleri (Package Manager)
| Paket | Durum | Amaç |
|---|---|---|
| Input System | ✅ yüklü | Dokunmatik, mouse, EnhancedTouch |
| Universal RP | ✅ yüklü | Render |
| Unity UI (uGUI) | ✅ yüklü | Prototip HUD (M1'de kaldırılacak) |
| UI Toolkit | ✅ yerleşik | Menüler ve HUD |
| Test Framework | ✅ yüklü | EditMode testleri |
| **Ads Mediation (LevelPlay)** | M3'te eklenecek | Reklam |
| **In-App Purchasing** | M3'te eklenecek | IAP |
| **Analytics + Remote Config** (UGS) | M3'te eklenecek | Ölçme |
| Mobile Notifications | v1.1 | Günlük seri hatırlatma |
| Localization | v1.1+ (ihtiyaç olursa) | Çoklu dil |
| ❌ AI Assistant / AI Inference / AI Navigation / Collab Proxy | Kaldırılabilir | Şablondan geldi, kullanılmıyor. Kaldırmak build boyutunu ve derleme süresini azaltır |

## 4. Claude Code: eklentiler (plugins), skill'ler, bağlayıcılar (connectors)
### 4.1 Önerilen kurulum (**ikiniz de**)
| Ne | Tür | Neden | Öncelik |
|---|---|---|---|
| **Unity** (Unity Technologies, resmi) | Plugin (skill paketi) | Unity 6'ya özel 30+ skill: UI Toolkit, LevelPlay, IAP, UGS, build/CLI, polish | **Zorunlu** |
| **GitHub** (resmi MCP sunucusu) | Plugin / connector | Claude'un issue, PR ve Project board'u yönetebilmesi, PR review | **Zorunlu** |
| **Google Drive** | Connector (hesabında kurulu, **bağlı değil**) | KPSS içerik tablolarını Google Sheets'te ortak tutup Claude'a okutmak | Önerilir (M2) |
| Figma | Connector | UI tasarımını Figma'da yaparsanız Claude oradan UXML/USS üretebilir | İsteğe bağlı |
| `unity-perf` (topluluk) | Plugin | C# performans, sıfır GC, kod standartları | İsteğe bağlı (M3) |
| Notion / Linear | Connector | Görev takibi. GitHub Projects yeterli olduğu için **gerek yok** | Gerek yok |

**Kurulum:** claude.ai → Settings → Plugins / Connectors (Claude Code'da `/plugin` komutu da var). Ortağın aynı eklentileri **kendi hesabına** kurmalı.

### 4.2 Hangi skill hangi işte kullanılacak
| Milestone | Skill | Kim |
|---|---|---|
| M0 | `unity:unity-cli` (derleme, test, build komut satırından), `unity:unity-package-management` (gereksiz paketleri kaldırma) | A, B |
| M0–M1 | `unity:ui` → `unity:ui-uitk` (UXML/USS, PanelSettings, runtime binding) | B |
| M1 | `unity:craft-game-floors` (sanat yönü, juice, menü ve kayıt standartları) | A, B |
| M1–M2 | `unity:audio-setup-mixers`, `unity:optimize-audio` | B |
| M2 | `unity:manage-sprite-atlas` (ikonlar) | B |
| M3 | `unity:levelplay-unity-integration`, `unity:implement-in-app-purchases`, `unity:build-live-game` | B |
| M3 | `unity:urp-postprocessing` (gerekirse hafif bloom/vignette) | A |
| Her PR | `code-review`, `simplify`, `security-review` (IAP ve rıza kodlarında) | A, B |

### 4.3 ⭐ En önemli öneri: Claude Code'u Unity'nin kurulu olduğu bilgisayarda çalıştırın
Şu anki bulut oturumu Unity Editor'ü **göremiyor**: derleme hatalarını, Console'u ve Play modunu. Bu yüzden her deneme "pull → dene → hatayı yapıştır" döngüsüyle ilerliyor.
- **Claude Code'u masaüstü uygulaması ya da CLI olarak** proje klasöründe açarsanız Unity eklentisinin `unity:unity-cli` skill'i açık Editor'ü sürebilir: derleme hatalarını okuyabilir, testleri çalıştırabilir, build alabilir.
- Bulut oturumları dokümantasyon, veri ve bağımsız C# işleri için hâlâ kullanışlı.

## 5. Ücretsiz varlık kaynakları (lisanslarına dikkat)
| Ne | Kaynak | Lisans |
|---|---|---|
| Harita verisi | Natural Earth | Kamu malı ✅ (zaten kullanılıyor) |
| Font | Google Fonts (Nunito, Inter, Baloo 2) | OFL ✅ |
| Ses efektleri | Kenney.nl, freesound (CC0 filtresiyle) | CC0 ✅ |
| İkonlar | Kenney, Material Symbols | CC0 / Apache 2.0 ✅ |
| Müzik | Kevin MacLeod (CC-BY, kaynak gösterilmeli), CC0 müzik paketleri | Kontrol et |
