"""
Türkiye il sınırlarını (Natural Earth 10m admin-1, kamu malı) Unity'nin
doğrudan okuyabileceği, önceden üçgenlenmiş bir JSON'a dönüştürür.

Kullanım:
    pip install shapely mapbox_earcut
    python Tools/geo/build_provinces.py

Çıktı: Assets/_Project/Resources/Data/turkey_provinces.json
"""
import json
import math
import os

import mapbox_earcut as earcut
import numpy as np
from shapely.geometry import shape, Polygon, MultiPolygon
from shapely.ops import polylabel

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "ne_10m_admin1_turkey.geojson")
DST = os.path.join(HERE, "..", "..", "Assets", "_Project", "Resources", "Data", "turkey_provinces.json")

TARGET_WIDTH = 20.0  # Unity birimi cinsinden harita genişliği

NAMES = {
    1: "Adana", 2: "Adıyaman", 3: "Afyonkarahisar", 4: "Ağrı", 5: "Amasya", 6: "Ankara", 7: "Antalya",
    8: "Artvin", 9: "Aydın", 10: "Balıkesir", 11: "Bilecik", 12: "Bingöl", 13: "Bitlis", 14: "Bolu",
    15: "Burdur", 16: "Bursa", 17: "Çanakkale", 18: "Çankırı", 19: "Çorum", 20: "Denizli",
    21: "Diyarbakır", 22: "Edirne", 23: "Elazığ", 24: "Erzincan", 25: "Erzurum", 26: "Eskişehir",
    27: "Gaziantep", 28: "Giresun", 29: "Gümüşhane", 30: "Hakkari", 31: "Hatay", 32: "Isparta",
    33: "Mersin", 34: "İstanbul", 35: "İzmir", 36: "Kars", 37: "Kastamonu", 38: "Kayseri",
    39: "Kırklareli", 40: "Kırşehir", 41: "Kocaeli", 42: "Konya", 43: "Kütahya", 44: "Malatya",
    45: "Manisa", 46: "Kahramanmaraş", 47: "Mardin", 48: "Muğla", 49: "Muş", 50: "Nevşehir",
    51: "Niğde", 52: "Ordu", 53: "Rize", 54: "Sakarya", 55: "Samsun", 56: "Siirt", 57: "Sinop",
    58: "Sivas", 59: "Tekirdağ", 60: "Tokat", 61: "Trabzon", 62: "Tunceli", 63: "Şanlıurfa",
    64: "Uşak", 65: "Van", 66: "Yozgat", 67: "Zonguldak", 68: "Aksaray", 69: "Bayburt",
    70: "Karaman", 71: "Kırıkkale", 72: "Batman", 73: "Şırnak", 74: "Bartın", 75: "Ardahan",
    76: "Iğdır", 77: "Yalova", 78: "Karabük", 79: "Kilis", 80: "Osmaniye", 81: "Düzce",
}

REGIONS = {
    "Marmara": [10, 11, 16, 17, 22, 34, 39, 41, 54, 59, 77],
    "Ege": [3, 9, 20, 35, 43, 45, 48, 64],
    "Akdeniz": [1, 7, 15, 31, 32, 33, 46, 80],
    "İç Anadolu": [6, 18, 26, 38, 40, 42, 50, 51, 58, 66, 68, 70, 71],
    "Karadeniz": [5, 8, 14, 19, 28, 29, 37, 52, 53, 55, 57, 60, 61, 67, 69, 74, 78, 81],
    "Doğu Anadolu": [4, 12, 13, 23, 24, 25, 30, 36, 44, 49, 62, 65, 75, 76],
    "Güneydoğu Anadolu": [2, 21, 27, 47, 56, 63, 72, 73, 79],
}
REGION_OF = {p: r for r, plates in REGIONS.items() for p in plates}
assert len(REGION_OF) == 81


def load_provinces():
    data = json.load(open(SRC, encoding="utf-8"))
    feats = data["features"]
    geoms = [shape(f["geometry"]) for f in feats]
    return feats, geoms


def get_projection(geoms=None):
    """İllerin sınırından eşdikdörtgen projeksiyon üretir. build_features.py de aynısını kullanır."""
    if geoms is None:
        geoms = load_provinces()[1]
    minx = min(g.bounds[0] for g in geoms)
    miny = min(g.bounds[1] for g in geoms)
    maxx = max(g.bounds[2] for g in geoms)
    maxy = max(g.bounds[3] for g in geoms)
    lon0, lat0 = (minx + maxx) / 2, (miny + maxy) / 2
    kx = math.cos(math.radians(lat0))
    scale = TARGET_WIDTH / ((maxx - minx) * kx)

    def proj(lon, lat):
        return ((lon - lon0) * kx * scale, (lat - lat0) * scale)

    return proj, (minx, miny, maxx, maxy)


def main():
    feats, geoms = load_provinces()
    proj, (minx, miny, maxx, maxy) = get_projection(geoms)
    lon0, lat0 = (minx + maxx) / 2, (miny + maxy) / 2
    kx = math.cos(math.radians(lat0))
    scale = TARGET_WIDTH / ((maxx - minx) * kx)

    def ring_xy(coords):
        pts = [proj(x, y) for x, y in coords]
        if pts[0] == pts[-1]:
            pts = pts[:-1]
        return pts

    # Komşuluk: sınır paylaşan iller (küçük tampon, nokta temaslarını elemek için ortak sınır uzunluğu)
    plates = [int(f["properties"]["iso_3166_2"].split("-")[1]) for f in feats]
    neighbors = {p: [] for p in plates}
    buffered = [g.buffer(0.01) for g in geoms]
    for i in range(len(geoms)):
        for j in range(i + 1, len(geoms)):
            if not buffered[i].intersects(buffered[j]):
                continue
            shared = buffered[i].intersection(buffered[j]).area
            if shared > 0.0004:
                neighbors[plates[i]].append(plates[j])
                neighbors[plates[j]].append(plates[i])

    out = []
    for f, g in zip(feats, geoms):
        plate = int(f["properties"]["iso_3166_2"].split("-")[1])
        polys = list(g.geoms) if isinstance(g, MultiPolygon) else [g]

        verts, tris, rings = [], [], []
        for poly in polys:
            ring_list = [ring_xy(poly.exterior.coords)] + [ring_xy(i.coords) for i in poly.interiors]
            ring_list = [r for r in ring_list if len(r) >= 3]
            if not ring_list:
                continue
            flat = np.array([p for r in ring_list for p in r], dtype=np.float64)
            ends = np.cumsum([len(r) for r in ring_list]).astype(np.uint32)
            idx = earcut.triangulate_float64(flat, ends)
            base = len(verts) // 2
            for x, y in flat:
                verts += [round(float(x), 4), round(float(y), 4)]
            tris += [int(i) + base for i in idx]
            for r in ring_list:
                rings.append({"points": [round(c, 4) for p in r for c in p]})

        largest = max(polys, key=lambda p: p.area)
        lp = polylabel(largest, tolerance=0.001)
        lx, ly = proj(lp.x, lp.y)

        out.append({
            "plate": plate,
            "name": NAMES[plate],
            "region": REGION_OF[plate],
            "label": [round(lx, 4), round(ly, 4)],
            "neighbors": sorted(neighbors[plate]),
            "vertices": verts,
            "triangles": tris,
            "rings": rings,
        })

    # Bölge dış hatları (bölüm başında "sınır çizilme" animasyonu için)
    from shapely.ops import unary_union
    region_outlines = []
    for region in REGIONS:
        u = unary_union([g for f, g in zip(feats, geoms)
                         if REGION_OF[int(f["properties"]["iso_3166_2"].split("-")[1])] == region]).simplify(0.01)
        parts = list(u.geoms) if isinstance(u, MultiPolygon) else [u]
        rings = []
        for part in sorted(parts, key=lambda q: -q.area):
            if part.area < 0.02:
                continue
            pts = [proj(x, y) for x, y in list(part.exterior.coords)[:-1]]
            rings.append({"points": [round(c, 4) for p in pts for c in p]})
        region_outlines.append({"region": region, "rings": rings})

    out.sort(key=lambda p: p["plate"])
    assert [p["plate"] for p in out] == list(range(1, 82))
    bx0, by0 = proj(minx, miny)
    bx1, by1 = proj(maxx, maxy)
    result = {
        "source": "Natural Earth 10m admin-1 (public domain)",
        "bounds": [round(bx0, 4), round(by0, 4), round(bx1, 4), round(by1, 4)],
        "provinces": out,
        "regionOutlines": region_outlines,
        # Unity'de boylam/enlemden harita koordinatına çevirmek için: x = (lon - p[0]) * p[2], y = (lat - p[1]) * p[3]
        "projection": [round(lon0, 6), round(lat0, 6), round(kx * scale, 6), round(scale, 6)],
    }
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with open(DST, "w", encoding="utf-8") as fp:
        json.dump(result, fp, ensure_ascii=False, separators=(",", ":"))
    print("Yazıldı:", os.path.normpath(DST), "| il:", len(out),
          "| üçgen:", sum(len(p["triangles"]) // 3 for p in out))


if __name__ == "__main__":
    main()
