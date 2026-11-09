"""
MGM'nin il bazındaki uzun yıllar istatistiklerinden (mgm.gov.tr → Resmi İstatistikler → İllerimize Ait Genel
İstatistik Verileri) aylık ortalama sıcaklık ve aylık toplam yağış ortalamasını çeker ve
Assets/_Project/Resources/Data/climate.json'daki il merkezlerinin değerlerini günceller.

Kullanım:
    python Tools/content/fetch_mgm_climate.py

Ağ erişimi gerekir (mgm.gov.tr). Değerler MGM'nin "Ölçüm Periyodu" ortalamalarıdır (ör. 1930–2025).
"""
import html
import json
import os
import re
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
DST = os.path.join(HERE, "..", "..", "Assets", "_Project", "Resources", "Data", "climate.json")
URL = "https://www.mgm.gov.tr/veridegerlendirme/il-ve-ilceler-istatistik.aspx?k=A&m={}"
TR = str.maketrans({"İ": "I", "ı": "I", "Ş": "S", "ş": "S", "Ğ": "G", "ğ": "G", "Ü": "U", "ü": "U",
                    "Ö": "O", "ö": "O", "Ç": "C", "ç": "C"})


def fetch(name):
    key = name.translate(TR).upper()
    req = urllib.request.Request(URL.format(key), headers={"User-Agent": "kpss-geography-quiz/1.0"})
    with urllib.request.urlopen(req, timeout=30) as r:
        page = r.read().decode("utf-8", errors="replace")
    text = html.unescape(re.sub(r"\s+", " ", re.sub(r"<[^>]+>", " ", page)))

    def row(label):
        m = re.search(re.escape(label) + r" ((?:-?[\d.,]+ ){12})", text)
        if not m:
            raise ValueError(f"{name}: '{label}' satırı bulunamadı")
        return [round(float(v.replace(",", ".")), 1) for v in m.group(1).split()]

    period = re.search(r"Ölçüm Periyodu \( ?(\d{4}) ?- ?(\d{4}) ?\)", text)
    return row("Ortalama Sıcaklık (°C)"), row("Aylık Toplam Yağış Miktarı Ortalaması (mm)"), \
        (f"{period.group(1)}–{period.group(2)}" if period else "")


def main():
    data = json.load(open(DST, encoding="utf-8"))
    periods = set()
    for c in data["cities"]:
        temp, rain, period = fetch(c["name"])
        c["temp"], c["rain"] = temp, rain
        periods.add(period)
        print(f"{c['name']:<12} yıllık yağış {sum(rain):6.0f} mm  sıcaklık {min(temp):5.1f} … {max(temp):5.1f} °C  ({period})")
    data["source"] = ("MGM, İllerimize Ait Genel İstatistik Verileri: aylık ortalama sıcaklık ve aylık toplam yağış "
                      "ortalaması (il merkezi istasyonu, ölçüm periyodu " + ", ".join(sorted(p for p in periods if p)) + ").")
    with open(DST, "w", encoding="utf-8") as fp:
        json.dump(data, fp, ensure_ascii=False, indent=1)
    print("Yazıldı:", os.path.normpath(DST))


if __name__ == "__main__":
    main()
