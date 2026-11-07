"""
Fiziki harita katmanını üretir: akarsular ve göller (Natural Earth, kamu malı) +
elle çizilmiş dağlar, volkanlar, ovalar ve platolar (features_manual.json).

Kullanım:
    pip install shapely mapbox_earcut
    python Tools/geo/build_features.py

Çıktı: Assets/_Project/Resources/Data/turkey_features.json
Projeksiyon build_provinces.py ile aynıdır (iller ve yer şekilleri birebir örtüşür).
"""
import json
import math
import os

import mapbox_earcut as earcut
import numpy as np
from shapely.geometry import shape, LineString, MultiLineString, Polygon, MultiPolygon, Point
from shapely.ops import linemerge, unary_union, polylabel

from build_provinces import load_provinces, get_projection

HERE = os.path.dirname(os.path.abspath(__file__))
RIVERS_SRC = os.path.join(HERE, "ne_10m_rivers_turkey.geojson")
LAKES_SRC = os.path.join(HERE, "ne_10m_lakes_turkey.geojson")
LAKES_EU_SRC = os.path.join(HERE, "ne_10m_lakes_europe_turkey.geojson")
MANUAL_SRC = os.path.join(HERE, "features_manual.json")
DST = os.path.join(HERE, "..", "..", "Assets", "_Project", "Resources", "Data", "turkey_features.json")

# id, Türkçe ad, Natural Earth'teki adları, KPSS bilgi notu
RIVERS = [
    ("kizilirmak", "Kızılırmak", ["Kızılırmak", "Kiz?lirmak"], "Türkiye'nin en uzun akarsuyu (1.355 km). Sivas'tan doğar, Bafra'da Karadeniz'e dökülür."),
    ("yesilirmak", "Yeşilırmak", ["Yeşilırmak"], "Kelkit Çayı'nı alır; Çarşamba Ovası'nı oluşturarak Karadeniz'e dökülür."),
    ("kelkit", "Kelkit Çayı", ["Kelkit"], "Yeşilırmak'ın en önemli koludur."),
    ("cekerek", "Çekerek Çayı", ["Çekerek"], "Yeşilırmak'ın kollarındandır."),
    ("sakarya", "Sakarya", ["Sakarya"], "Porsuk'u alarak kuzeye akar ve Karadeniz'e dökülür; üzerinde Sarıyar ve Gökçekaya barajları vardır."),
    ("porsuk", "Porsuk Çayı", ["Porsuk"], "Sakarya Nehri'nin koludur; Eskişehir'den geçer."),
    ("delice", "Delice Irmağı", ["Delice"], "Kızılırmak'ın kollarındandır."),
    ("filyos", "Filyos Çayı", ["Fliyos"], "Batı Karadeniz'de, Zonguldak yakınlarında Karadeniz'e dökülür."),
    ("firat", "Fırat", ["Firat"], "Karasu ile Murat'ın birleşmesiyle oluşur; Keban, Karakaya ve Atatürk barajları üzerindedir. Basra Körfezi'ne ulaşır."),
    ("murat", "Murat Nehri", ["Murat"], "Fırat'ın en uzun koludur; Keban'da Karasu ile birleşerek Fırat'ı oluşturur."),
    ("dicle", "Dicle", ["Dicle"], "Elazığ'daki Hazar Gölü çevresinden doğar; Ilısu Barajı üzerindedir ve Fırat ile birleşerek Basra Körfezi'ne dökülür."),
    ("aras", "Aras", ["Aras"], "Bingöl Dağları'ndan doğar, doğu sınırı boyunca akarak Kura ile birleşir ve Hazar Denizi'ne ulaşır."),
    ("coruh", "Çoruh", ["Çoruh"], "Doğu Karadeniz'in en hızlı akan nehirlerindendir; Gürcistan'da (Batum) Karadeniz'e dökülür. Deriner ve Yusufeli barajları üzerindedir."),
    ("seyhan", "Seyhan", ["Seyhan"], "Ceyhan ile birlikte Çukurova'yı oluşturur; Akdeniz'e dökülür."),
    ("ceyhan", "Ceyhan", ["Ceyhan"], "Seyhan ile birlikte Çukurova'yı oluşturur; Aslantaş Barajı üzerindedir."),
    ("goksu", "Göksu", ["S. Göksu", "Goksu (northern branch)"], "Taşeli Platosu'ndan geçerek Silifke'de Akdeniz'e dökülür; ağzında delta oluşturmuştur."),
    ("buyuk-menderes", "Büyük Menderes", ["Büyük Menderes"], "Menderes (büklüm) yaparak akar; Ege Denizi'ne dökülür, ağzında delta oluşturmuştur."),
    ("kucuk-menderes", "Küçük Menderes", ["Küçük Menderes"], "Bozdağlar ile Aydın Dağları arasındaki grabende akar, Selçuk yakınında Ege'ye dökülür."),
    ("gediz", "Gediz", ["Gediz"], "Manisa ovalarını sular, İzmir Körfezi'nin kuzeyinde Ege Denizi'ne dökülür."),
    ("susurluk", "Susurluk Çayı", ["Susurluk"], "Simav Çayı'nı alır; Marmara Denizi'ne dökülen en önemli akarsudur."),
    ("meric", "Meriç", ["Evros"], "Türkiye–Yunanistan sınırını oluşturur; Enez yakınında Ege Denizi'ne dökülür."),
    ("ergene", "Ergene", ["Ergene"], "Trakya'nın en önemli akarsuyudur; Meriç'in koludur."),
]

LAKE_NAMES = {
    "Lake Van": "Van Gölü", "Lake Tuz": "Tuz Gölü", "Beyşehir": "Beyşehir Gölü", "Eğirdir": "Eğirdir Gölü",
    "Keban Baraji": "Keban Baraj Gölü", "Ataturk Barajt": "Atatürk Baraj Gölü",
}


def r4(v):
    return round(float(v), 4)


def chaikin(pts, closed, iterations=2):
    for _ in range(iterations):
        out = []
        n = len(pts)
        rng = range(n) if closed else range(n - 1)
        if not closed:
            out.append(pts[0])
        for i in rng:
            a, b = pts[i], pts[(i + 1) % n]
            out.append((0.75 * a[0] + 0.25 * b[0], 0.75 * a[1] + 0.25 * b[1]))
            out.append((0.25 * a[0] + 0.75 * b[0], 0.25 * a[1] + 0.75 * b[1]))
        if not closed:
            out.append(pts[-1])
        pts = out
    return pts


def main():
    feats, geoms = load_provinces()
    proj, _ = get_projection(geoms)
    turkey = unary_union(geoms)
    clip = turkey.buffer(0.12)

    def flat(coords):
        return [r4(c) for lon, lat in coords for c in proj(lon, lat)]

    def line_label(lines):
        longest = max(lines, key=lambda l: l.length)
        p = longest.interpolate(0.5, normalized=True)
        return [r4(c) for c in proj(p.x, p.y)]

    def triangulate(polys):
        verts, tris, outline = [], [], []
        for poly in polys:
            rings = [list(poly.exterior.coords)[:-1]] + [list(i.coords)[:-1] for i in poly.interiors]
            rings = [[proj(lon, lat) for lon, lat in r] for r in rings if len(r) >= 3]
            arr = np.array([p for r in rings for p in r], dtype=np.float64)
            ends = np.cumsum([len(r) for r in rings]).astype(np.uint32)
            idx = earcut.triangulate_float64(arr, ends)
            base = len(verts) // 2
            for x, y in arr:
                verts += [r4(x), r4(y)]
            tris += [int(i) + base for i in idx]
            for r in rings:
                outline.append({"points": [r4(c) for p in r for c in p]})
        return verts, tris, outline

    features = []

    # ---------- Akarsular ----------
    river_data = json.load(open(RIVERS_SRC, encoding="utf-8"))["features"]
    for rid, name, aliases, info in RIVERS:
        cands = [f for f in river_data if f["properties"]["name"] in aliases]
        europe = [f for f in cands if f["properties"]["source"] == "ne_10m_rivers_europe"]
        chosen = europe or cands
        if not chosen:
            print("UYARI: akarsu bulunamadı:", name)
            continue
        u = unary_union([shape(f["geometry"]) for f in chosen])
        merged = linemerge(u) if isinstance(u, MultiLineString) else u
        clipped = merged.intersection(clip)
        parts = [clipped] if isinstance(clipped, LineString) else list(getattr(clipped, "geoms", []))
        parts = [p.simplify(0.008) for p in parts if isinstance(p, LineString) and p.length > 0.05]
        if not parts:
            print("UYARI: akarsu Türkiye içinde değil:", name)
            continue
        features.append({
            "id": rid, "name": name, "category": "river", "kind": "line", "info": info,
            "label": line_label(parts),
            "lines": [{"points": flat(p.coords)} for p in parts],
            "vertices": [], "triangles": [], "outline": [],
        })

    manual = json.load(open(MANUAL_SRC, encoding="utf-8"))

    # ---------- Dağlar ----------
    for m in manual["mountains"]:
        pts = chaikin([tuple(p) for p in m["line"]], closed=False)
        ls = LineString(pts)
        features.append({
            "id": m["id"], "name": m["name"], "category": "mountain", "kind": "line", "info": m["info"],
            "label": line_label([ls]),
            "lines": [{"points": flat(pts)}],
            "vertices": [], "triangles": [], "outline": [],
        })

    # ---------- Volkanlar ----------
    for v in manual["volcanoes"]:
        x, y = proj(*v["point"])
        features.append({
            "id": v["id"], "name": v["name"], "category": "volcano", "kind": "point", "info": v["info"],
            "label": [r4(x), r4(y)], "lines": [], "vertices": [], "triangles": [], "outline": [],
        })

    # ---------- Ovalar ve platolar ----------
    for key, cat in (("plains", "plain"), ("plateaus", "plateau")):
        for a in manual[key]:
            pts = chaikin([tuple(p) for p in a["polygon"]], closed=True)
            poly = Polygon(pts).buffer(0)
            verts, tris, outline = triangulate([poly])
            lp = polylabel(poly, tolerance=0.005)
            features.append({
                "id": a["id"], "name": a["name"], "category": cat, "kind": "polygon", "info": a["info"],
                "label": [r4(c) for c in proj(lp.x, lp.y)],
                "lines": [], "vertices": verts, "triangles": tris, "outline": outline,
            })

    # ---------- Göller (soru olarak) ----------
    ne_lakes = {"lakes": json.load(open(LAKES_SRC, encoding="utf-8"))["features"],
                "europe": json.load(open(LAKES_EU_SRC, encoding="utf-8"))["features"]}

    def find_ne_lake(item):
        pool = ne_lakes[item["ne_file"]]
        if "ne_name" in item:
            for f in pool:
                if f["properties"]["name"] == item["ne_name"]:
                    return shape(f["geometry"])
        if "ne_center" in item:
            pt = Point(*item["ne_center"])
            best = min(pool, key=lambda f: shape(f["geometry"]).distance(pt))
            return shape(best["geometry"])
        return None

    def ellipse(center, rx_km, ry_km, angle_deg, n=28):
        lon0, lat0 = center
        ky = 1 / 111.0
        kx = 1 / (111.0 * math.cos(math.radians(lat0)))
        a = math.radians(angle_deg)
        pts = []
        for i in range(n):
            t = 2 * math.pi * i / n
            x, y = rx_km * math.cos(t), ry_km * math.sin(t)
            xr, yr = x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a)
            pts.append((lon0 + xr * kx, lat0 + yr * ky))
        return Polygon(pts)

    for item in manual["lakes"]["items"]:
        if item["source"] == "ne":
            g = find_ne_lake(item)
            if g is None:
                print("UYARI: göl bulunamadı:", item["name"])
                continue
            g = g.simplify(0.003)
        else:
            g = ellipse(item["center"], max(item["rx"], 1.2), max(item["ry"], 1.0), item.get("angle", 0))
        polys = list(g.geoms) if isinstance(g, MultiPolygon) else [g]
        verts, tris, outline = triangulate(polys)
        largest = max(polys, key=lambda q: q.area)
        lp = polylabel(largest, tolerance=0.002)
        features.append({
            "id": "gol-" + item["id"], "name": item["name"], "category": "lake", "kind": "polygon",
            "subtype": item.get("type", ""), "info": item["info"],
            "label": [r4(c) for c in proj(lp.x, lp.y)],
            "lines": [], "vertices": verts, "triangles": tris, "outline": outline,
        })

    # ---------- Barajlar (akarsu üzerine oturtulur) ----------
    river_lines = {}
    for f in features:
        if f["category"] == "river":
            river_lines[f["id"]] = f["lines"]

    def snap_to_river(river_id, px, py):
        best, bd = (px, py), float("inf")
        for l in river_lines.get(river_id, []):
            pts = l["points"]
            for i in range(0, len(pts) - 2, 2):
                ax, ay, bx, by = pts[i], pts[i + 1], pts[i + 2], pts[i + 3]
                dx, dy = bx - ax, by - ay
                L = dx * dx + dy * dy
                t = 0 if L == 0 else max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / L))
                qx, qy = ax + dx * t, ay + dy * t
                d = (qx - px) ** 2 + (qy - py) ** 2
                if d < bd:
                    bd, best = d, (qx, qy)
        return best if bd < 0.6 ** 2 else (px, py)

    for dam in manual["dams"]:
        x, y = proj(*dam["point"])
        x, y = snap_to_river(dam["river"], x, y)
        features.append({
            "id": dam["id"], "name": dam["name"], "category": "dam", "kind": "point", "subtype": dam["river"],
            "info": dam["info"], "label": [r4(x), r4(y)], "lines": [], "vertices": [], "triangles": [], "outline": [],
        })

    # ---------- Kıyı şekilleri ve miras alanları (nokta) ----------
    coast_cat = {"strait": "gulf", "gulf": "gulf", "peninsula": "peninsula", "island": "peninsula", "cape": "peninsula"}
    for key, catmap in (("coast", coast_cat), ("heritage", {"unesco": "unesco", "park": "park"})):
        for c in manual[key]:
            x, y = proj(*c["point"])
            features.append({
                "id": c["id"], "name": c["name"], "category": catmap[c["subtype"]], "kind": "point",
                "subtype": c["subtype"], "info": c["info"], "label": [r4(x), r4(y)],
                "lines": [], "vertices": [], "triangles": [], "outline": [],
            })

    # ---------- Fay hatları ----------
    for fl in manual["faults"]:
        pts = chaikin([tuple(p) for p in fl["line"]], closed=False)
        features.append({
            "id": fl["id"], "name": fl["name"], "category": "fault", "kind": "line", "subtype": "",
            "info": fl["info"], "label": line_label([LineString(pts)]),
            "lines": [{"points": flat(pts)}], "vertices": [], "triangles": [], "outline": [],
        })

    for f in features:
        f.setdefault("subtype", "")

    # ---------- Göller (dekor) ----------
    lakes = []
    for f in json.load(open(LAKES_SRC, encoding="utf-8"))["features"]:
        g = shape(f["geometry"]).simplify(0.004)
        polys = list(g.geoms) if isinstance(g, MultiPolygon) else [g]
        verts, tris, outline = triangulate(polys)
        lakes.append({"name": LAKE_NAMES.get(f["properties"]["name"], f["properties"]["name"]),
                      "vertices": verts, "triangles": tris, "outline": outline})

    result = {"source": "Natural Earth (kamu malı) + elle çizim (Tools/geo/features_manual.json)",
              "features": features, "lakes": lakes}
    with open(DST, "w", encoding="utf-8") as fp:
        json.dump(result, fp, ensure_ascii=False, separators=(",", ":"))

    from collections import Counter
    print("Yazıldı:", os.path.normpath(DST))
    print(" ", dict(Counter(f["category"] for f in features)), "| göl:", len(lakes))


if __name__ == "__main__":
    main()
