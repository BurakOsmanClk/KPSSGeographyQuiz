"""
Gölgeli kabartma (hillshade) dokusu üretir: Assets/_Project/Resources/Textures/turkey_relief.png

Yükseklik verisi: AWS Open Data "Terrain Tiles" (terrarium PNG, zoom 8). Kaynaklar arasında SRTM (kamu malı),
ETOPO1 ve diğer açık veriler bulunur. Atıf: "Terrain Tiles – Mapzen / AWS Open Data" (bkz. docs/CREDITS.md).
Karolar Tools/geo/cache/ altına indirilir (git'e girmez).

Doku illerin projeksiyonuyla birebir hizalıdır: harita sınırı (bounds) = dokunun tamamı.
Renk: gölgeler koyu kahve (alfa = gölge şiddeti), güneş alan yamaçlar hafif beyaz. Türkiye dışında saydam.

Kullanım: python Tools/geo/build_relief.py
"""
import io
import math
import os
import urllib.request

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from shapely.geometry import Polygon, MultiPolygon
from shapely.ops import unary_union

from build_provinces import load_provinces, get_projection

HERE = os.path.dirname(os.path.abspath(__file__))
CACHE = os.path.join(HERE, "cache", "terrarium8")
DST = os.path.join(HERE, "..", "..", "Assets", "_Project", "Resources", "Textures", "turkey_relief.png")
Z = 8
OUT_W = 2048
URL = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png"


def lonlat_to_tile(lon, lat, z):
    n = 2 ** z
    x = (lon + 180.0) / 360.0 * n
    lat_r = math.radians(lat)
    y = (1.0 - math.log(math.tan(lat_r) + 1.0 / math.cos(lat_r)) / math.pi) / 2.0 * n
    return x, y


def fetch_tile(x, y):
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, f"{x}_{y}.png")
    if not os.path.exists(path):
        with urllib.request.urlopen(URL.format(z=Z, x=x, y=y), timeout=60) as r:
            data = r.read()
        with open(path, "wb") as f:
            f.write(data)
    img = np.asarray(Image.open(path).convert("RGB"), dtype=np.float64)
    return img[..., 0] * 256 + img[..., 1] + img[..., 2] / 256 - 32768


def main():
    feats, geoms = load_provinces()
    proj, (minx, miny, maxx, maxy) = get_projection(geoms)
    # Harita koordinatından boylam/enleme (projeksiyon doğrusal)
    x0, y0 = proj(0.0, 0.0)
    x1, _ = proj(1.0, 0.0)
    _, y1 = proj(0.0, 1.0)
    ax, ay = x1 - x0, y1 - y0
    bx0, by0 = proj(minx, miny)
    bx1, by1 = proj(maxx, maxy)
    out_h = int(round(OUT_W * (by1 - by0) / (bx1 - bx0)))

    # Karoları indir ve birleştir
    pad = 0.3
    tx0, ty0 = lonlat_to_tile(minx - pad, maxy + pad, Z)
    tx1, ty1 = lonlat_to_tile(maxx + pad, miny - pad, Z)
    xs = range(int(tx0), int(tx1) + 1)
    ys = range(int(ty0), int(ty1) + 1)
    mosaic = np.zeros((len(ys) * 256, len(xs) * 256))
    for j, ty in enumerate(ys):
        for i, tx in enumerate(xs):
            mosaic[j * 256:(j + 1) * 256, i * 256:(i + 1) * 256] = fetch_tile(tx, ty)
    print("karo:", len(xs) * len(ys))

    # Çıkış pikselleri için yükseklik örnekle (bilineer)
    px = (np.arange(OUT_W) + 0.5) / OUT_W * (bx1 - bx0) + bx0
    py = by1 - (np.arange(out_h) + 0.5) / out_h * (by1 - by0)  # üstten alta
    gx, gy = np.meshgrid(px, py)
    lon = (gx - x0) / ax
    lat = (gy - y0) / ay
    n = 2 ** Z
    mx = (lon + 180.0) / 360.0 * n
    lat_r = np.radians(lat)
    my = (1.0 - np.log(np.tan(lat_r) + 1.0 / np.cos(lat_r)) / np.pi) / 2.0 * n
    fx = (mx - xs[0]) * 256 - 0.5
    fy = (my - ys[0]) * 256 - 0.5
    ix = np.clip(np.floor(fx).astype(int), 0, mosaic.shape[1] - 2)
    iy = np.clip(np.floor(fy).astype(int), 0, mosaic.shape[0] - 2)
    wx, wy = np.clip(fx - ix, 0, 1), np.clip(fy - iy, 0, 1)
    elev = (mosaic[iy, ix] * (1 - wx) * (1 - wy) + mosaic[iy, ix + 1] * wx * (1 - wy) +
            mosaic[iy + 1, ix] * (1 - wx) * wy + mosaic[iy + 1, ix + 1] * wx * wy)
    elev = np.maximum(elev, 0)

    # Gölgelendirme (azimut 315°, yükseklik 45°, dikey abartı 2.5)
    deg_per_px_lon = (lon[0, -1] - lon[0, 0]) / (OUT_W - 1)
    deg_per_px_lat = (lat[0, 0] - lat[-1, 0]) / (out_h - 1)
    dx_m = deg_per_px_lon * 111320 * np.cos(np.radians(lat))
    dy_m = deg_per_px_lat * 110540
    exaggeration = 2.5
    dzdx = np.gradient(elev, axis=1) / dx_m * exaggeration
    dzdy = -np.gradient(elev, axis=0) / dy_m * exaggeration
    slope = np.arctan(np.hypot(dzdx, dzdy))
    aspect = np.arctan2(dzdy, -dzdx)
    az, alt = math.radians(315), math.radians(45)
    shade = np.sin(alt) * np.cos(slope) + np.cos(alt) * np.sin(slope) * np.cos(az - (math.pi / 2 - aspect))
    shade = np.clip(shade, 0, 1)
    flat = np.sin(alt)

    rgba = np.zeros((out_h, OUT_W, 4), dtype=np.float64)
    dark = shade < flat
    t_dark = np.clip((flat - shade) / flat, 0, 1)
    t_lit = np.clip((shade - flat) / (1 - flat), 0, 1)
    rgba[..., 0] = np.where(dark, 0.18, 1.0)
    rgba[..., 1] = np.where(dark, 0.12, 1.0)
    rgba[..., 2] = np.where(dark, 0.08, 0.95)
    rgba[..., 3] = np.where(dark, np.power(t_dark, 0.8) * 0.9, t_lit * 0.35)
    # Yüksek kesimlere çok hafif ek koyuluk (yükselti hissi)
    rgba[..., 3] = np.maximum(rgba[..., 3], np.where(dark, 0, 0) + np.clip((elev - 1500) / 3500, 0, 1) * 0.12)

    # Türkiye maskesi
    mask = Image.new("L", (OUT_W, out_h), 0)
    draw = ImageDraw.Draw(mask)
    land = unary_union(geoms)
    polys = list(land.geoms) if isinstance(land, MultiPolygon) else [land]

    def to_px(lonv, latv):
        X, Y = proj(lonv, latv)
        return ((X - bx0) / (bx1 - bx0) * OUT_W, (by1 - Y) / (by1 - by0) * out_h)

    for p in polys:
        draw.polygon([to_px(*c) for c in p.exterior.coords], fill=255)
        for hole in p.interiors:
            draw.polygon([to_px(*c) for c in hole.coords], fill=0)
    mask = mask.filter(ImageFilter.GaussianBlur(0.8))
    m = np.asarray(mask, dtype=np.float64) / 255.0
    rgba[..., 3] *= m

    img = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA")
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    img.save(DST, optimize=True)
    print("Yazıldı:", os.path.normpath(DST), img.size, os.path.getsize(DST) // 1024, "KB")


if __name__ == "__main__":
    main()
