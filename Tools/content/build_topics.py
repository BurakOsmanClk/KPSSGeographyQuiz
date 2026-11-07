"""
İl bazlı konu setlerini (tarım, maden, enerji, nüfus) ve il özelliklerini (iklim, bitki örtüsü)
Resources/Data/topics.json olarak üretir. İl adları plaka koduna otomatik çevrilir (yazım hatası = hata verir).
Kullanım: python Tools/content/build_topics.py
Not: Listeler KPSS kaynaklarında "öne çıkan iller" olarak geçen illerdir; üretim sıralamaları yıllara göre değişebilir.
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "geo"))
from build_provinces import NAMES  # noqa: E402

DST = os.path.join(HERE, "..", "..", "Assets", "_Project", "Resources", "Data", "topics.json")
PLATE = {v: k for k, v in NAMES.items()}


def plates(*names):
    return [PLATE[n] for n in names]


Q_PROD = "ÜRETİMİNDE ÖNE ÇIKAN BİR İL SEÇ"
TOPICS = [
    # ---- Tarım I ----
    ("tarim1", "findik", "Fındık", Q_PROD, ("Ordu", "Giresun", "Samsun", "Sakarya", "Düzce", "Trabzon"),
     "Fındık Karadeniz kıyısında yetişir; üretimde Ordu ilk sıradadır.", "TÜİK"),
    ("tarim1", "cay", "Çay", Q_PROD, ("Rize", "Trabzon", "Artvin", "Giresun"),
     "Çay, bol ve her mevsim yağış isteyen Doğu Karadeniz kıyısında yetişir; Rize ilk sıradadır.", "TÜİK"),
    ("tarim1", "pamuk", "Pamuk", Q_PROD, ("Şanlıurfa", "Aydın", "Hatay", "Diyarbakır", "Adana"),
     "Pamuk sıcak ve sulanabilen ovalarda yetişir; GAP ile Şanlıurfa (Harran) ilk sıraya yükselmiştir.", "TÜİK"),
    ("tarim1", "zeytin", "Zeytin", Q_PROD, ("Aydın", "İzmir", "Muğla", "Manisa", "Balıkesir", "Hatay", "Bursa"),
     "Zeytin Akdeniz iklimine özgüdür; Ege kıyıları en önemli üretim alanıdır.", "TÜİK"),
    ("tarim1", "tutun", "Tütün", Q_PROD, ("Manisa", "İzmir", "Adıyaman", "Denizli", "Samsun"),
     "Tütün Ege (Manisa, İzmir), Güneydoğu (Adıyaman) ve Karadeniz'de (Samsun-Bafra) yetiştirilir.", "TÜİK / MEB"),
    ("tarim1", "seker-pancari", "Şeker Pancarı", Q_PROD, ("Konya", "Eskişehir", "Afyonkarahisar", "Kayseri", "Aksaray"),
     "Şeker pancarı sulanabilen iç bölge ovalarında yetişir; Konya ilk sıradadır. Taşıma maliyeti yüksek olduğundan fabrikalar üretim alanına yakın kurulur.", "TÜİK"),
    ("tarim1", "bugday", "Buğday", Q_PROD, ("Konya", "Ankara", "Şanlıurfa", "Diyarbakır", "Tekirdağ"),
     "Buğday en geniş alanda ekilen tahıldır; İç Anadolu (Konya, Ankara) ve Güneydoğu öne çıkar.", "TÜİK"),
    ("tarim1", "aycicegi", "Ayçiçeği", Q_PROD, ("Tekirdağ", "Edirne", "Kırklareli", "Konya"),
     "Ayçiçeği üretiminde Trakya (Ergene Ovası) öne çıkar.", "TÜİK"),
    # ---- Tarım II ----
    ("tarim2", "muz", "Muz", Q_PROD, ("Mersin", "Antalya"),
     "Muz, mikroklima özelliği gösteren Anamur (Mersin) ve Alanya–Gazipaşa (Antalya) kıyılarında yetişir.", "MEB"),
    ("tarim2", "kayisi", "Kayısı", Q_PROD, ("Malatya", "Elazığ", "Iğdır"),
     "Kuru kayısı üretiminde Malatya dünyada öne çıkar.", "TÜİK"),
    ("tarim2", "incir", "İncir", Q_PROD, ("Aydın", "İzmir", "Bursa"),
     "İncir üretiminde Büyük ve Küçük Menderes ovaları (Aydın) ilk sıradadır.", "TÜİK"),
    ("tarim2", "uzum", "Üzüm", Q_PROD, ("Manisa", "Denizli", "İzmir"),
     "Çekirdeksiz (sultaniye) üzümde Manisa ve Gediz Ovası öne çıkar.", "TÜİK"),
    ("tarim2", "antep-fistigi", "Antep Fıstığı", Q_PROD, ("Gaziantep", "Şanlıurfa", "Siirt", "Adıyaman"),
     "Antep fıstığı Güneydoğu Anadolu'nun kurak, sıcak yamaçlarında yetişir.", "TÜİK"),
    ("tarim2", "turuncgil", "Turunçgiller", Q_PROD, ("Mersin", "Adana", "Antalya", "Hatay"),
     "Portakal, limon ve mandalina kışları ılık geçen Akdeniz kıyılarında yetişir.", "TÜİK"),
    ("tarim2", "hashas", "Haşhaş", Q_PROD, ("Afyonkarahisar", "Uşak", "Denizli", "Burdur"),
     "Haşhaş, devlet izniyle (TMO denetiminde) belirli illerde ekilir; Afyonkarahisar öne çıkar.", "TMO / MEB"),
    ("tarim2", "gul", "Yağ Gülü", Q_PROD, ("Isparta", "Burdur"),
     "Gül yağı üretiminde Isparta dünyada öne çıkar.", "MEB"),
    ("tarim2", "elma", "Elma", Q_PROD, ("Isparta", "Karaman", "Niğde", "Antalya"),
     "Elma üretiminde Isparta ve Karaman ilk sıralardadır.", "TÜİK"),
    ("tarim2", "celtik", "Çeltik (Pirinç)", Q_PROD, ("Edirne", "Samsun", "Balıkesir", "Çorum"),
     "Çeltik bol su ister; Meriç–Ergene (Edirne) ve Bafra–Çarşamba (Samsun) ovaları öne çıkar.", "TÜİK"),
    # ---- Hayvancılık ----
    ("hayvan", "buyukbas", "Büyükbaş Hayvancılık", "YAYGIN OLDUĞU BİR İL SEÇ", ("Erzurum", "Kars", "Ardahan", "Konya"),
     "Büyükbaş hayvancılık gür çayırların bulunduğu Erzurum–Kars yöresinde yaygındır; Konya da öne çıkar.", "TÜİK / MEB"),
    ("hayvan", "kucukbas", "Küçükbaş Hayvancılık", "YAYGIN OLDUĞU BİR İL SEÇ", ("Van", "Konya", "Şanlıurfa", "Ağrı"),
     "Küçükbaş (koyun, keçi) bozkır alanlarında yaygındır; Van ve Konya öne çıkar.", "TÜİK / MEB"),
    ("hayvan", "ipekbocegi", "İpek Böcekçiliği", "YAPILDIĞI BİR İL SEÇ", ("Bursa", "Diyarbakır"),
     "İpek böcekçiliği dut ağacına bağlıdır; Bursa ve Diyarbakır öne çıkar.", "MEB"),
    ("hayvan", "aricilik", "Arıcılık (Bal)", "ÜRETİMDE ÖNE ÇIKAN BİR İL SEÇ", ("Ordu", "Muğla"),
     "Bal üretiminde Ordu ve çam balıyla Muğla öne çıkar.", "TÜİK"),
    # ---- Madenler ----
    ("maden", "bor", "Bor", "ÇIKARILDIĞI BİR İL SEÇ", ("Eskişehir", "Kütahya", "Balıkesir", "Bursa"),
     "Dünya bor rezervinin büyük kısmı Türkiye'dedir: Kırka (Eskişehir), Emet (Kütahya), Bigadiç (Balıkesir), Kestelek (Bursa).", "MTA / Eti Maden"),
    ("maden", "krom", "Krom", "ÇIKARILDIĞI BİR İL SEÇ", ("Elazığ", "Muğla", "Bursa", "Eskişehir"),
     "Krom: Guleman (Elazığ), Fethiye (Muğla), Orhaneli (Bursa).", "MTA"),
    ("maden", "bakir", "Bakır", "ÇIKARILDIĞI BİR İL SEÇ", ("Artvin", "Kastamonu", "Elazığ", "Rize"),
     "Bakır: Murgul (Artvin), Küre (Kastamonu), Maden (Elazığ), Çayeli (Rize).", "MTA"),
    ("maden", "demir", "Demir", "ÇIKARILDIĞI BİR İL SEÇ", ("Sivas", "Malatya", "Kayseri"),
     "Demir: Divriği (Sivas), Hekimhan (Malatya), Yahyalı (Kayseri).", "MTA"),
    ("maden", "linyit", "Linyit", "ÇIKARILDIĞI BİR İL SEÇ", ("Kahramanmaraş", "Manisa", "Kütahya", "Muğla", "Sivas"),
     "Linyit: Afşin–Elbistan (K.Maraş), Soma (Manisa), Seyitömer–Tunçbilek (Kütahya), Yatağan (Muğla), Kangal (Sivas).", "MTA / TKİ"),
    ("maden", "taskomuru", "Taşkömürü", "ÇIKARILDIĞI BİR İL SEÇ", ("Zonguldak", "Bartın"),
     "Taşkömürü yalnızca Zonguldak–Bartın (Amasra) havzasında çıkarılır.", "TTK / MTA"),
    ("maden", "boksit", "Boksit (Alüminyum)", "ÇIKARILDIĞI BİR İL SEÇ", ("Konya", "Antalya"),
     "Boksit: Seydişehir (Konya) ve Akseki (Antalya); Seydişehir'de alüminyum tesisi vardır.", "MTA"),
    ("maden", "mermer", "Mermer", "ÇIKARILDIĞI BİR İL SEÇ", ("Afyonkarahisar", "Balıkesir", "Denizli", "Bilecik"),
     "Mermer: Afyonkarahisar (İscehisar), Marmara Adası (Balıkesir), Denizli, Bilecik.", "MTA"),
    ("maden", "altin", "Altın", "ÇIKARILDIĞI BİR İL SEÇ", ("İzmir", "Uşak", "Erzincan", "Gümüşhane"),
     "Altın: Ovacık–Bergama (İzmir), Kışladağ (Uşak), İliç (Erzincan), Gümüşhane.", "MTA"),
    ("maden", "tuz", "Kaya Tuzu ve Göl Tuzu", "ÜRETİLDİĞİ BİR İL SEÇ", ("Çankırı", "Aksaray"),
     "Kaya tuzu Çankırı'da çıkarılır; göl tuzu Tuz Gölü'nden (Aksaray–Konya) elde edilir.", "MTA / MEB"),
    ("maden", "kukurt", "Kükürt", "ÇIKARILDIĞI BİR İL SEÇ", ("Isparta",),
     "Kükürt Keçiborlu'da (Isparta) çıkarılır.", "MTA"),
    # ---- Enerji ----
    ("enerji", "petrol", "Petrol", "ÇIKARILDIĞI BİR İL SEÇ", ("Batman", "Adıyaman", "Diyarbakır", "Şırnak"),
     "Petrol: Batman (Raman), Adıyaman, Diyarbakır ve son yıllarda Gabar (Şırnak).", "TPAO / ETKB"),
    ("enerji", "dogalgaz", "Doğal Gaz", "ÜRETİMDE ÖNE ÇIKAN BİR İL SEÇ", ("Kırklareli", "Tekirdağ", "Zonguldak"),
     "Doğal gaz: Trakya (Kırklareli–Hamitabat, Tekirdağ) ve Karadeniz'deki Sakarya Gaz Sahası (Filyos–Zonguldak).", "TPAO / ETKB"),
    ("enerji", "jeotermal", "Jeotermal Enerji", "SANTRALLERİNİN BULUNDUĞU BİR İL SEÇ", ("Denizli", "Aydın", "Manisa"),
     "Jeotermal santraller fay hatlarının yoğun olduğu Ege grabenlerinde: Kızıldere (Denizli), Germencik (Aydın).", "ETKB"),
    ("enerji", "ruzgar", "Rüzgâr Enerjisi", "KURULU GÜCÜN YÜKSEK OLDUĞU BİR İL SEÇ", ("İzmir", "Balıkesir", "Çanakkale", "Manisa"),
     "Rüzgâr santralleri en çok Ege ve Marmara kıyılarında (İzmir, Balıkesir, Çanakkale) kuruludur.", "ETKB"),
    ("enerji", "gunes", "Güneş Enerjisi", "BÜYÜK SANTRALLERİN BULUNDUĞU BİR İL SEÇ", ("Konya", "Karaman"),
     "Konya Karapınar'da Türkiye'nin en büyük güneş enerjisi santrali bulunur.", "ETKB"),
    ("enerji", "termik", "Termik Santral (Kömür)", "BULUNDUĞU BİR İL SEÇ", ("Kahramanmaraş", "Muğla", "Manisa", "Kütahya", "Zonguldak"),
     "Termik santraller kömür havzalarına yakın kurulur: Afşin–Elbistan, Yatağan, Soma, Seyitömer, Çatalağzı.", "ETKB"),
    # ---- Nüfus ----
    ("nufus", "en-kalabalik", "En Kalabalık İller", "İLK 5 İÇİNDEN BİR İL SEÇ", ("İstanbul", "Ankara", "İzmir", "Bursa", "Antalya"),
     "Nüfusu en fazla iller: İstanbul, Ankara, İzmir, Bursa, Antalya (TÜİK ADNKS 2023).", "TÜİK ADNKS 2023"),
    ("nufus", "en-az-nufus", "En Az Nüfuslu İller", "SON 5 İÇİNDEN BİR İL SEÇ", ("Bayburt", "Tunceli", "Ardahan", "Gümüşhane", "Kilis"),
     "Nüfusu en az iller: Bayburt, Tunceli, Ardahan, Gümüşhane, Kilis (TÜİK ADNKS 2023).", "TÜİK ADNKS 2023"),
    ("nufus", "yogunluk-yuksek", "Nüfus Yoğunluğu En Yüksek", "BİR İL SEÇ", ("İstanbul", "Kocaeli", "İzmir", "Gaziantep"),
     "Nüfus yoğunluğu sanayi ve ticaretin geliştiği illerde yüksektir; İstanbul ilk sıradadır.", "TÜİK ADNKS 2023"),
    ("nufus", "yogunluk-dusuk", "Nüfus Yoğunluğu En Düşük", "BİR İL SEÇ", ("Tunceli", "Ardahan", "Erzincan"),
     "Engebeli ve sert iklimli Doğu Anadolu'da nüfus yoğunluğu düşüktür; en az Tunceli'dir.", "TÜİK ADNKS 2023"),
    ("nufus", "dogurganlik", "Doğurganlık Hızı En Yüksek", "BİR İL SEÇ", ("Şanlıurfa", "Şırnak", "Ağrı", "Siirt", "Muş"),
     "Doğurganlık hızı Güneydoğu ve Doğu Anadolu'da yüksektir; Şanlıurfa ilk sıradadır.", "TÜİK Doğum İstatistikleri"),
    ("nufus", "yasli", "Yaşlı Nüfus Oranı En Yüksek", "BİR İL SEÇ", ("Sinop", "Kastamonu", "Giresun"),
     "Göç veren Karadeniz illerinde yaşlı nüfus oranı yüksektir; Sinop ilk sıradadır.", "TÜİK"),
]

ATTRIBUTES = [
    {
        "id": "climate",
        "prompt": "BU İLDE HANGİ İKLİM TİPİ GÖRÜLÜR?",
        "choices": ["Karadeniz", "Akdeniz", "Karasal", "Marmara (Geçiş)"],
        "colors": ["#4FAE6A", "#F2A541", "#C9A26B", "#7FB7E3"],
        "infos": [
            "Karadeniz iklimi: her mevsim yağışlı, yazlar serin, kışlar ılık. Doğal bitki örtüsü ormandır.",
            "Akdeniz iklimi: yazlar sıcak ve kurak, kışlar ılık ve yağışlı. Doğal bitki örtüsü makidir.",
            "Karasal iklim: yazlar sıcak ve kurak, kışlar soğuk ve kar yağışlı. Doğal bitki örtüsü bozkırdır.",
            "Marmara (geçiş) iklimi: Karadeniz, Akdeniz ve karasal iklim arasında geçiş özellikleri gösterir.",
        ],
        "groups": [
            ("Karadeniz", ("Düzce", "Zonguldak", "Bartın", "Kastamonu", "Sinop", "Samsun", "Ordu", "Giresun", "Trabzon", "Rize", "Artvin")),
            ("Akdeniz", ("İzmir", "Aydın", "Muğla", "Antalya", "Mersin", "Adana", "Hatay", "Osmaniye", "Manisa")),
            ("Karasal", ("Ankara", "Konya", "Eskişehir", "Kayseri", "Sivas", "Yozgat", "Kırşehir", "Nevşehir", "Niğde", "Aksaray",
                         "Karaman", "Kırıkkale", "Çankırı", "Erzurum", "Kars", "Ardahan", "Ağrı", "Van", "Muş", "Erzincan", "Malatya",
                         "Elazığ", "Bitlis", "Bingöl", "Diyarbakır", "Şanlıurfa", "Batman")),
            ("Marmara (Geçiş)", ("İstanbul", "Kocaeli", "Yalova", "Bursa", "Tekirdağ", "Sakarya")),
        ],
    },
    {
        "id": "vegetation",
        "prompt": "BU İLİN DOĞAL BİTKİ ÖRTÜSÜ HANGİSİDİR?",
        "choices": ["Orman", "Maki", "Bozkır", "Dağ Çayırı"],
        "colors": ["#3E9B5C", "#9BC53D", "#D9B26F", "#6FC1A6"],
        "infos": [
            "Orman: her mevsim yağış alan Karadeniz kıyılarında gür ormanlar (kayın, gürgen, ladin) yaygındır.",
            "Maki: yaz kuraklığına dayanıklı, her dem yeşil çalılar; Akdeniz ve Ege kıyılarında görülür.",
            "Bozkır: ilkbahar yağışlarıyla yeşerip yazın kuruyan otlar; İç Anadolu ve Güneydoğu'da yaygındır.",
            "Dağ çayırı: yaz yağışlarıyla gürleşen yüksek çayırlar; Erzurum–Kars yöresinde büyükbaş hayvancılığı besler.",
        ],
        "groups": [
            ("Orman", ("Düzce", "Zonguldak", "Bartın", "Kastamonu", "Sinop", "Ordu", "Giresun", "Trabzon", "Rize", "Artvin")),
            ("Maki", ("İzmir", "Aydın", "Muğla", "Antalya", "Mersin", "Hatay")),
            ("Bozkır", ("Ankara", "Konya", "Eskişehir", "Kırşehir", "Nevşehir", "Niğde", "Aksaray", "Karaman", "Kırıkkale",
                        "Yozgat", "Diyarbakır", "Şanlıurfa")),
            ("Dağ Çayırı", ("Erzurum", "Kars", "Ardahan")),
        ],
    },
    {
        "id": "laketype",
        "prompt": "BU GÖL OLUŞUMUNA GÖRE HANGİ TÜRDENDİR?",
        "choices": ["Tektonik", "Volkanik", "Karstik", "Heyelan set", "Alüvyal set", "Kıyı set", "Baraj (yapay)"],
        "colors": ["#7FB7E3", "#E07A5F", "#81B29A", "#C9A26B", "#F2CC8F", "#9AD1D4", "#B8B8D1"],
        "infos": [
            "Tektonik göl: yer kabuğu hareketleriyle oluşan çukurlarda birikir (Tuz, İznik, Burdur, Hazar).",
            "Volkanik göl: lav akıntılarının vadiyi kapatmasıyla (Van, Çıldır) ya da kraterlerde (Meke) oluşur.",
            "Karstik göl: kalkerli arazide erime çukurlarında oluşur (Salda, Avlan).",
            "Heyelan set gölü: heyelanla vadinin önünün kapanmasıyla oluşur (Tortum, Sera, Abant).",
            "Alüvyal set gölü: akarsuların taşıdığı alüvyonların önünü kapatmasıyla oluşur (Bafa, Marmara, Mogan).",
            "Kıyı set gölü (lagün): kıyı kordonlarının körfez ağzını kapatmasıyla oluşur (Küçükçekmece, Terkos, Akyatan).",
            "Baraj gölü: akarsu üzerine yapılan barajın arkasında oluşan yapay göldür (Atatürk, Keban).",
        ],
        "groups": [],
    },
]


def main():
    out_topics = []
    for set_id, tid, title, question, names, info, source in TOPICS:
        out_topics.append({"id": tid, "set": set_id, "title": title, "question": question,
                           "plates": plates(*names), "info": info, "source": source})
    out_attrs = []
    for a in ATTRIBUTES:
        groups = []
        for choice, names in a["groups"]:
            groups.append({"choice": a["choices"].index(choice), "plates": plates(*names)})
        out_attrs.append({"id": a["id"], "prompt": a["prompt"], "choices": a["choices"], "colors": a["colors"],
                          "infos": a["infos"], "groups": groups})
    with open(DST, "w", encoding="utf-8") as fp:
        json.dump({"topics": out_topics, "attributes": out_attrs}, fp, ensure_ascii=False, indent=1)
    print("Yazıldı:", os.path.normpath(DST), "| konu:", len(out_topics), "| özellik:", len(out_attrs))


if __name__ == "__main__":
    main()
