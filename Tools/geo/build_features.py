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
from shapely.geometry import shape, box, LineString, MultiLineString, Polygon, MultiPolygon, Point
from shapely.ops import linemerge, unary_union, polylabel, nearest_points

from build_provinces import load_provinces, get_projection

HERE = os.path.dirname(os.path.abspath(__file__))
RIVERS_SRC = os.path.join(HERE, "ne_10m_rivers_turkey.geojson")
LAKES_SRC = os.path.join(HERE, "ne_10m_lakes_turkey.geojson")
LAKES_EU_SRC = os.path.join(HERE, "ne_10m_lakes_europe_turkey.geojson")
MANUAL_SRC = os.path.join(HERE, "features_manual.json")
COUNTRIES_SRC = os.path.join(HERE, "ne_10m_neighbors_turkey.geojson")

# Dağ alanı genişlikleri (km); verilmeyenler varsayılan
MOUNTAIN_WIDTH_KM = {"guneydogu-toroslar": 42, "dogu-karadeniz": 30, "kure": 24, "bolkar": 26, "geyik": 24, "bey": 22,
                     "aladaglar": 20, "nur": 18, "uludag": 14, "kazdagi": 14, "madra": 13, "bozdaglar": 15, "aydin": 15,
                     "mentese": 18, "munzur": 22, "palandoken": 16, "allahuekber": 18, "sultan": 16, "yildiz": 20,
                     "cilo": 16, "sat": 12, "spil": 10, "yunt": 12, "murat-dagi": 12, "ganos": 10, "dedegol": 12,
                     "bingol": 18, "zigana": 16, "samanli": 14, "sundiken": 12}
DEFAULT_MOUNTAIN_KM = 20

# Akarsu yatağından türetilen ovalar: (akarsu id(leri), [boylam_min, enlem_min, boylam_max, enlem_max], genişlik km)
PLAINS_FROM_RIVERS = {
    "buyuk-menderes": (["buyuk-menderes"], [27.58, 37.4, 29.2, 38.0], 9),
    "gediz": (["gediz"], [27.15, 38.4, 28.6, 38.75], 8),
    "kucuk-menderes": (["kucuk-menderes"], [27.25, 37.95, 28.3, 38.3], 9),
    "soke": (["buyuk-menderes"], [27.2, 37.42, 27.58, 37.65], 14),
    "menemen": (["gediz"], [26.8, 38.5, 27.15, 38.75], 12),
    "silifke": (["goksu"], [33.8, 36.2, 34.15, 36.5], 12),
    "meric": (["meric"], [25.95, 40.6, 26.45, 41.1], 9),
    "eskisehir": (["porsuk"], [30.3, 39.7, 31.0, 39.88], 10),
    "erbaa": (["kelkit"], [36.45, 40.55, 37.1, 40.8], 7),
    "ergene": (["ergene"], [26.5, 41.0, 27.95, 41.5], 8),
    "adapazari": (["sakarya"], [30.25, 40.6, 30.75, 41.0], 10),
    "bafra": (["kizilirmak"], [35.55, 41.35, 36.25, 41.8], 15),
    "carsamba": (["yesilirmak"], [36.4, 41.05, 37.0, 41.4], 15),
    "cukurova": (["seyhan", "ceyhan"], [34.95, 36.6, 36.2, 37.15], 20),
    "igdir": (["aras"], [43.6, 39.75, 44.7, 40.15], 11),
    "mus": (["murat"], [41.15, 38.6, 41.95, 38.95], 10),
    "pasinler": (["aras"], [41.35, 39.8, 42.15, 40.1], 9),
}

COUNTRY_TR = {"Greece": "Yunanistan", "Bulgaria": "Bulgaristan", "Georgia": "Gürcistan", "Armenia": "Ermenistan",
              "Azerbaijan": "Azerbaycan", "Iran": "İran", "Iraq": "Irak", "Syria": "Suriye", "Cyprus": "Kıbrıs",
              "Northern Cyprus": "Kıbrıs", "Russia": "Rusya", "Ukraine": "Ukrayna", "Romania": "Romanya",
              "Lebanon": "Lübnan", "Israel": "İsrail", "Jordan": "Ürdün", "Palestine": "Filistin"}
# Kara sınırı olan komşular: "Komşu ülkeyi bul" bölümünde tıklanabilir alan olurlar.
# (Türkçe ad, oyunda görünen ad, etiket [boylam, enlem] — MapLabelLayer'daki ülke adı konumlarıyla aynı, KPSS notu)
LAND_NEIGHBORS = [
    ("Yunanistan", "Yunanistan", [25.35, 41.25], "Trakya'da Meriç Nehri sınırı oluşturur."),
    ("Bulgaristan", "Bulgaristan", [26.8, 42.25], "Trakya'da, Edirne ve Kırklareli ile komşudur."),
    ("Gürcistan", "Gürcistan", [43.0, 41.85], "Doğu Karadeniz'de Artvin ve Ardahan ile komşudur."),
    ("Ermenistan", "Ermenistan", [44.55, 40.55], "Kars ve Iğdır ile komşudur; sınırın bir bölümünü Arpaçay ve Aras oluşturur."),
    ("Azerbaycan", "Nahçıvan (Azerbaycan)", [45.25, 39.3], "Türkiye'nin en kısa kara sınırı Azerbaycan'a bağlı Nahçıvan iledir (Iğdır)."),
    ("İran", "İran", [44.95, 38.2], "Ağrı, Iğdır, Van ve Hakkari ile komşudur."),
    ("Irak", "Irak", [43.6, 36.85], "Şırnak ve Hakkari ile komşudur."),
    ("Suriye", "Suriye", [38.6, 36.25], "Türkiye'nin en uzun kara sınırı Suriye iledir (Hatay'dan Şırnak'a)."),
]
GATE_COUNTRY = {"Nahçıvan": "Azerbaycan"}  # kapı listesindeki sınır ülkesi → Natural Earth ülkesi

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
    ("buyuk-menderes", "Büyük Menderes", ["Büyük Menderes", "Byk Menderes"], "Menderes (büklüm) yaparak akar; Ege Denizi'ne dökülür, ağzında delta oluşturmuştur."),
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
    y_a, y_b = proj(0, 0)[1], proj(0, 1)[1]
    units_per_km = (y_b - y_a) / 110.57

    def to_xy(g):
        from shapely.ops import transform
        return transform(lambda x, y, z=None: proj(x, y), g)

    land_xy = to_xy(turkey).buffer(0)
    river_parts = {}

    def triangulate_xy(polys):
        verts, tris, outline = [], [], []
        for poly in polys:
            if poly.is_empty or poly.area <= 0:
                continue
            rings = [list(poly.exterior.coords)[:-1]] + [list(i.coords)[:-1] for i in poly.interiors]
            rings = [r for r in rings if len(r) >= 3]
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

    def polys_of(g):
        if g.is_empty:
            return []
        if isinstance(g, Polygon):
            return [g]
        return [q for q in getattr(g, "geoms", []) if isinstance(q, Polygon)]

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
        # İki Natural Earth kaynağından Türkiye içinde daha uzun olanı seç (çift çizgi olmasın)
        by_src = {}
        for f in cands:
            by_src.setdefault(f["properties"]["source"], []).append(f)

        def length_in(fs):
            return unary_union([shape(f["geometry"]) for f in fs]).intersection(clip).length

        if not by_src:
            print("UYARI: akarsu bulunamadı:", name)
            continue
        # Uzun kaynak ana çizgi; diğer kaynaktan yalnızca ana çizgide olmayan (tamamlayıcı) parçalar eklenir
        ordered = sorted(by_src.values(), key=length_in, reverse=True)
        primary = unary_union([shape(f["geometry"]) for f in ordered[0]])
        extra = []
        for fs in ordered[1:]:
            other = unary_union([shape(f["geometry"]) for f in fs]).difference(primary.buffer(0.04))
            extra += [g for g in (getattr(other, "geoms", None) or [other]) if not g.is_empty and g.length > 0.05]
        u = unary_union([primary] + extra)
        merged = linemerge(u) if isinstance(u, MultiLineString) else u
        clipped = merged.intersection(clip)
        parts = [clipped] if isinstance(clipped, LineString) else list(getattr(clipped, "geoms", []))
        parts = [p.simplify(0.008) for p in parts if isinstance(p, LineString) and p.length > 0.05]
        if not parts:
            print("UYARI: akarsu Türkiye içinde değil:", name)
            continue
        river_parts[rid] = parts
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
        ridge = to_xy(ls)
        w = MOUNTAIN_WIDTH_KM.get(m["id"], DEFAULT_MOUNTAIN_KM) * 1.4 * units_per_km
        outer = ridge.buffer(w / 2, quad_segs=6).simplify(0.01).intersection(land_xy)
        inner = ridge.buffer(w * 0.22, quad_segs=6).simplify(0.01).intersection(land_xy)
        overts, otris, ooutline = triangulate_xy(polys_of(outer))
        iverts, itris, _ = triangulate_xy(polys_of(inner))
        features.append({
            "id": m["id"], "name": m["name"], "category": "mountain", "kind": "polygon", "info": m["info"],
            "subtype": m.get("type", ""),
            "label": line_label([ls]),
            "lines": [{"points": flat(pts)}],
            "vertices": overts, "triangles": otris, "outline": ooutline,
            "innerVertices": iverts, "innerTriangles": itris,
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
            geom_xy = None
            if cat == "plain" and a["id"] in PLAINS_FROM_RIVERS:
                rids, bb, km = PLAINS_FROM_RIVERS[a["id"]]
                region = box(*bb)
                pieces = []
                for rid in rids:
                    for part in river_parts.get(rid, []):
                        seg = part.intersection(region)
                        if not seg.is_empty:
                            pieces.append(to_xy(seg).buffer(km * units_per_km / 2, quad_segs=8))
                if pieces:
                    u = unary_union(pieces)
                    geom_xy = u.buffer(0.06).buffer(-0.05)  # birleştir ve yumuşat
            if geom_xy is None and "ellipse" in a:
                e = a["ellipse"]
                # Küçük ovalar ekranda seçilebilsin diye 1.5 kat büyütülür (konum ve yön korunur)
                geom_xy = to_xy(ellipse(e["center"], e["rx"] * 1.5, e["ry"] * 1.5, e.get("angle", 0)))
            if geom_xy is None:
                rings = a["polygons"] if "polygons" in a else [a["polygon"]]
                geom_xy = unary_union([to_xy(Polygon(chaikin([tuple(p) for p in r], closed=True)).buffer(0)) for r in rings])
            geom_xy = geom_xy.simplify(0.008).intersection(land_xy)
            polys = polys_of(geom_xy)
            if not polys:
                print("UYARI: alan boş kaldı:", a["name"])
                continue
            verts, tris, outline = triangulate_xy(polys)
            largest = max(polys, key=lambda q: q.area)
            lp = polylabel(largest, tolerance=0.01)
            features.append({
                "id": a["id"], "name": a["name"], "category": cat, "kind": "polygon", "info": a["info"],
                "subtype": a.get("type", ""),
                "label": [r4(lp.x), r4(lp.y)],
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

    # ---------- Komşu ülkeler (soru alanı) ve kara hudut kapıları ----------
    country_geoms = {}
    for f in json.load(open(COUNTRIES_SRC, encoding="utf-8"))["features"]:
        tr = COUNTRY_TR.get(f["properties"]["name"])
        if tr is not None:
            g = shape(f["geometry"]).buffer(0)
            country_geoms[tr] = unary_union([country_geoms[tr], g]) if tr in country_geoms else g

    gates_by_country = {}
    for gate in manual.get("borderGates", []):
        gates_by_country.setdefault(gate["country"], []).append(gate["name"])

    # Hangi il hangi ülkeyle sınır komşusu? (ortak sınır en az 1 km)
    province_plates = [int(f["properties"]["iso_3166_2"].split("-")[1]) for f in feats]

    def border_plates(country_geom):
        zone = country_geom.buffer(0.01)
        return sorted(p for p, pg in zip(province_plates, geoms) if pg.boundary.intersection(zone).length * 111.0 > 1.0)

    for tr, shown, label, info in LAND_NEIGHBORS:
        g = country_geoms[tr]
        polys = [q for q in (list(g.geoms) if isinstance(g, MultiPolygon) else [g]) if q.area > 0.002]
        verts, tris, outline = triangulate(polys)
        gate_names = gates_by_country.get("Nahçıvan" if tr == "Azerbaycan" else tr, [])
        note = info + (f" Sınır kapıları: {', '.join(gate_names)}." if gate_names else "")
        features.append({
            "id": "ulke-" + tr.lower().replace("ı", "i").replace("ü", "u").replace("ö", "o").replace("ç", "c").replace("ş", "s").replace("ğ", "g").replace("i̇", "i"),
            "name": shown, "category": "country", "kind": "polygon", "subtype": tr, "info": note, "plates": border_plates(g),
            "label": [r4(c) for c in proj(*label)], "lines": [], "vertices": verts, "triangles": tris, "outline": outline,
        })

    # Kapılar: yaklaşık konum, o ülkeyle olan sınır çizgisinin en yakın noktasına oturtulur
    turkey_edge = turkey.boundary
    for gate in manual.get("borderGates", []):
        ctry = GATE_COUNTRY.get(gate["country"], gate["country"])
        shared = turkey_edge.intersection(country_geoms[ctry].buffer(0.03))
        approx = Point(*gate["point"])
        snapped = nearest_points(shared, approx)[0] if not shared.is_empty else approx
        moved_km = approx.distance(snapped) * 111.0
        if moved_km > 15:
            print(f"  UYARI: {gate['name']} sınıra {moved_km:.0f} km kaydırıldı, konumu kontrol edin.")
        x, y = proj(snapped.x, snapped.y)
        features.append({
            "id": gate["id"], "name": gate["name"], "category": "gate", "kind": "point", "subtype": gate["country"],
            "info": gate["info"], "label": [r4(x), r4(y)], "lines": [], "vertices": [], "triangles": [], "outline": [],
        })

    # ---------- Madenler: yataklar (site) ve fabrikalar (factory), nokta ----------
    # subtype = "<maden>-<tür>" (ör. demir-site): eğitim ekranı alt başlıkları ve oyun bunu kullanır; mineral ayrıca yazılır
    for m in manual.get("mines", []):
        x, y = proj(*m["point"])
        features.append({
            "id": m["id"], "name": m["name"], "category": "mine", "kind": "point",
            "subtype": f"{m['mineral']}-{m['kind']}", "mineral": m["mineral"],
            "info": m["info"], "label": [r4(x), r4(y)], "lines": [], "vertices": [], "triangles": [], "outline": [],
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

    # ---------- Komşu ülkeler (dekor) ----------
    countries = []
    for f in json.load(open(COUNTRIES_SRC, encoding="utf-8"))["features"]:
        name = COUNTRY_TR.get(f["properties"]["name"])
        if name is None:
            continue
        g = shape(f["geometry"]).buffer(0)
        polys = list(g.geoms) if isinstance(g, MultiPolygon) else [g]
        polys = [q for q in polys if q.area > 0.002]
        if not polys:
            continue
        verts, tris, outline = triangulate(polys)
        countries.append({"name": name, "vertices": verts, "triangles": tris, "outline": outline})

    for f in features:
        f.setdefault("innerVertices", [])
        f.setdefault("innerTriangles", [])

    result = {"source": "Natural Earth (kamu malı) + elle çizim (Tools/geo/features_manual.json)",
              "features": features, "lakes": lakes, "countries": countries}
    with open(DST, "w", encoding="utf-8") as fp:
        json.dump(result, fp, ensure_ascii=False, separators=(",", ":"))

    from collections import Counter
    print("Yazıldı:", os.path.normpath(DST))
    print(" ", dict(Counter(f["category"] for f in features)), "| göl:", len(lakes))


if __name__ == "__main__":
    main()
