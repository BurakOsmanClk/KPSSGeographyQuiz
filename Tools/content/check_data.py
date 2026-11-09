"""
Oyun verisi denetimi: Resources/Data altındaki bütün JSON dosyalarının birbirleriyle tutarlı olduğunu kontrol eder.
Veri ekledikten / bir build_*.py aracını çalıştırdıktan sonra ve push etmeden önce çalıştırın:

    python Tools/content/check_data.py

HATA  = oyunu bozar (bölüm açılmaz, soru üretilemez); push etmeden düzeltin. Çıkış kodu 1 olur.
UYARI = oyunu bozmaz ama veri düzeni kuralına uymuyor (ör. kaynak/yıl yok); docs/content/VERI_KAYNAKLARI.md'ye bakın.

Yalnızca okur; hiçbir dosyayı değiştirmez. Kurallar: CLAUDE.md "Veri düzeni" ve docs/content/VERI_KAYNAKLARI.md.
"""
import json
import os
import re
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
DATA = os.path.join(ROOT, "Assets", "_Project", "Resources", "Data")
# Her yıl değişen veriler: kaynağında yıl yazmalı (hangisinin eskidiği görülsün)
VOLATILE_SETS = {"tarim1", "tarim2", "hayvan", "ari", "enerji", "nufus"}  # sıralamalar her yıl değişir

errors, warnings = [], []


def err(msg):
    errors.append(msg)


def warn(msg):
    warnings.append(msg)


def load(name, required=True):
    path = os.path.join(DATA, name)
    if not os.path.exists(path):
        if required:
            err(f"{name}: dosya yok")
        return None
    try:
        with open(path, encoding="utf-8") as fp:
            return json.load(fp)
    except json.JSONDecodeError as e:
        err(f"{name}: geçersiz JSON ({e})")
        return None


def main():
    provinces = load("turkey_provinces.json")
    features = load("turkey_features.json")
    topics = load("topics.json")
    topics_ek = load("topics_ek.json", required=False)
    levels = load("levels.json")
    levels_ek = load("levels_ek.json", required=False)
    questions = load("questions.json")
    climate = load("climate.json", required=False)

    # ---- İller ----
    plates = set()
    if provinces:
        plates = {p["plate"] for p in provinces.get("provinces", [])}
        if plates != set(range(1, 82)):
            err(f"turkey_provinces.json: plakalar 1–81 değil (eksik: {sorted(set(range(1, 82)) - plates)})")

    # ---- Yer şekilleri ----
    feature_ids, categories, subtypes = set(), set(), {}
    if features:
        seen = {}
        for f in features.get("features", []):
            fid, cat = f.get("id"), f.get("category")
            categories.add(cat)
            subtypes.setdefault(cat, set()).add(f.get("subtype", ""))
            if (fid, cat) in seen:
                err(f"turkey_features.json: aynı kategoride tekrar eden id '{fid}' ({cat})")
            seen[(fid, cat)] = True
            feature_ids.add(fid)
            if not f.get("name"):
                err(f"turkey_features.json: '{fid}' adsız")
            if not f.get("info"):
                warn(f"turkey_features.json: '{fid}' ({cat}) bilgi notu boş")
            for p in f.get("plates", []) or []:
                if p not in plates:
                    err(f"turkey_features.json: '{fid}' geçersiz plaka {p}")

    # ---- Konu setleri ----
    topic_sets, attributes = set(), set()
    for name, db in (("topics.json", topics), ("topics_ek.json", topics_ek)):
        if not db:
            continue
        ids = set()
        for t in db.get("topics", []):
            tid = t.get("id")
            if tid in ids:
                warn(f"{name}: tekrar eden konu id '{tid}' (oyunu bozmaz ama karışıklık yaratır; farklı id verin)")
            ids.add(tid)
            topic_sets.add(t.get("set"))
            bad = [p for p in t.get("plates", []) if p not in plates]
            if bad:
                err(f"{name}: '{tid}' geçersiz plaka {bad}")
            if not t.get("plates"):
                err(f"{name}: '{tid}' hiç il içermiyor")
            src = t.get("source", "")
            if not src:
                warn(f"{name}: '{tid}' kaynak yok")
            elif t.get("set") in VOLATILE_SETS and not re.search(r"(19|20)\d\d", src):
                warn(f"{name}: '{tid}' her yıl değişen bir veri ama kaynağında yıl yok ('{src}')")
        for a in db.get("attributes", []) or []:
            attributes.add(a.get("id"))
            n = len(a.get("choices", []))
            for g in a.get("groups", []) or []:
                if not 0 <= g.get("choice", -1) < n:
                    err(f"{name}: özellik '{a.get('id')}' geçersiz seçenek {g.get('choice')}")
                bad = [p for p in g.get("plates", []) if p not in plates]
                if bad:
                    err(f"{name}: özellik '{a.get('id')}' geçersiz plaka {bad}")

    # ---- Soru bankası ----
    question_sets = set()
    if questions:
        ids = set()
        for q in questions.get("questions", []):
            qid = q.get("id")
            if qid in ids:
                err(f"questions.json: tekrar eden soru id '{qid}'")
            ids.add(qid)
            question_sets.add(q.get("set"))
            opts = q.get("options", [])
            if not 0 <= q.get("answer", -1) < len(opts):
                err(f"questions.json: '{qid}' cevap şıkların dışında")
            refs = [q.get("highlight") or ""] + list(q.get("markers") or [])
            for r in refs:
                if r.startswith("feature:") and r[8:] not in feature_ids:
                    err(f"questions.json: '{qid}' bilinmeyen yer şekli '{r}'")
                if r.startswith("plate:") and int(r[6:]) not in plates:
                    err(f"questions.json: '{qid}' geçersiz plaka '{r}'")
                if r.startswith(("arrow:", "line:")):
                    body = r.split(":", 1)[1].split("|")[0]
                    for end in body.split(">"):
                        if "," not in end and (not end.isdigit() or int(end) not in plates):
                            err(f"questions.json: '{qid}' şeklin ucu geçersiz '{end}'")
            for i in q.get("answerMarkers") or []:
                if not 0 <= i < len(q.get("markers") or []):
                    err(f"questions.json: '{qid}' answerMarkers dışarıda ({i})")

    # ---- İklim ----
    if climate:
        for c in climate.get("cities", []):
            if c.get("plate") not in plates:
                err(f"climate.json: '{c.get('name')}' geçersiz plaka")
            if len(c.get("temp", [])) != 12 or len(c.get("rain", [])) != 12:
                err(f"climate.json: '{c.get('name')}' 12 aylık değer yok")
        if not climate.get("source"):
            warn("climate.json: kaynak yok")

    # ---- Bölümler ----
    level_ids = set()
    for name, db in (("levels.json", levels), ("levels_ek.json", levels_ek)):
        if not db:
            continue
        groups = {g.get("id") for g in db.get("groups", [])}
        if levels and name == "levels_ek.json":
            groups |= {g.get("id") for g in levels.get("groups", [])}
        for lv in db.get("levels", []):
            lid, mode = lv.get("id"), lv.get("mode")
            if lid in level_ids:
                err(f"{name}: tekrar eden bölüm id '{lid}'")
            level_ids.add(lid)
            if lv.get("group") not in groups:
                err(f"{name}: '{lid}' bilinmeyen sekme '{lv.get('group')}'")
            cat, s, attr = lv.get("category"), lv.get("set"), lv.get("attribute")
            if mode in ("feature", "featurechoice") and cat not in categories:
                err(f"{name}: '{lid}' bilinmeyen kategori '{cat}'")
            if mode in ("featurechoice", "attribute") and attr not in attributes:
                err(f"{name}: '{lid}' bilinmeyen özellik '{attr}'")
            if mode == "topic" and s not in topic_sets:
                err(f"{name}: '{lid}' bilinmeyen konu seti '{s}'")
            if mode == "mcq" and s not in question_sets:
                err(f"{name}: '{lid}' soru bankasında set yok '{s}'")
            if mode == "numbered":
                for part in (s or "").split(","):
                    part = part.strip()
                    if part.startswith("match:"):
                        if part[6:] not in attributes:
                            err(f"{name}: '{lid}' bilinmeyen eşleştirme özelliği '{part}'")
                    elif part == "iklim-grafik":
                        if not climate:
                            err(f"{name}: '{lid}' climate.json yok")
                    elif part not in topic_sets and part not in question_sets:
                        err(f"{name}: '{lid}' bilinmeyen set '{part}'")

    for w in warnings:
        print("UYARI:", w)
    for e in errors:
        print("HATA :", e)
    print(f"\n{len(errors)} hata, {len(warnings)} uyarı. "
          f"({len(level_ids)} bölüm, {len(topic_sets)} konu seti, {len(question_sets)} soru seti, {len(feature_ids)} yer şekli)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
