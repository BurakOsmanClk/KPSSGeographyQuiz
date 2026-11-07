# Oturum Başlangıç Promptu

> Her yeni Claude Code oturumunda **ilk mesaj** olarak yapıştır. `<ROL>` yerine `A` (Burak) ya da `B` (Ortak) yaz.

```
Bu repo, Unity 6 ile Android ve iOS için geliştirdiğimiz "KPSS Coğrafya" harita oyunu.
İki geliştiriciyiz ve her birimiz kendi Claude Code oturumumuzla çalışıyoruz. Ben <ROL> rolündeyim.

Başlamadan önce:
1. `git pull` yap ve şu dosyaları oku: CLAUDE.md, docs/GDD.md, docs/ARCHITECTURE.md, docs/WORK_SPLIT.md, docs/ROADMAP.md.
2. Açık PR'lara ve bana atanmış issue'lara bak (`gh pr list`, `gh issue list --label owner:<ROL>`).
3. Bana 5 maddeyi geçmeyen bir özet ver: projenin şu anki durumu, benim sıradaki işim, diğer geliştiricinin üzerinde çalıştığı ve beni etkileyebilecek işler.

Kurallar:
- Sadece rolümün sahip olduğu klasörlerde değişiklik yap (WORK_SPLIT.md §1). Core sözleşmelerini değiştirmen gerekirse dur ve bana sor.
- `main`'e doğrudan push yapma. `<rol-harfi-küçük>/<kisa-ad>` adında bir branch aç ve PR ile ilerle.
- Sahne (.unity) ve prefab YAML'larını elle düzenleme. Editörde yapılması gerekenleri bana adım adım anlat.
- Unity'ye özgü bir işe başlamadan önce ilgili `unity:*` skill'ini yükle.
- PR açıklamasında şu başlıklar olsun: "Ne değişti", "Nasıl test edilir", "Diğer geliştiriciyi etkileyen değişiklikler", "Sıradaki adım".
```
