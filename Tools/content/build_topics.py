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


Q_PROD = "EN FAZLA ÜRETİLDİĞİ BİR İL SEÇ"
Q_ANIMAL = "EN FAZLA YETİŞTİRİLDİĞİ BİR İL SEÇ"
TOPICS = [
    # ---- Tarım I: Tarla ürünleri (en fazla üretildiği iller) ----
    ("tarim1", "bugday", "Buğday", Q_PROD, ("Konya", "Ankara", "Şanlıurfa", "Diyarbakır"),
     "Buğday üretiminde Konya ilk sıradadır; İç Anadolu ve Güneydoğu'nun tahıl alanları öne çıkar.", "TÜİK / KPSS"),
    ("tarim1", "arpa", "Arpa", Q_PROD, ("Konya", "Ankara"),
     "Arpa, kurağa dayanıklı bir tahıldır; İç Anadolu'da Konya ve Ankara ilk sıralardadır.", "TÜİK / KPSS"),
    ("tarim1", "misir", "Mısır", Q_PROD, ("Konya", "Şanlıurfa", "Adana"),
     "2023'te mısır üretiminde Konya, Şanlıurfa ve Adana ilk sıralarda yer aldı.", "TÜİK verisi (basın, 2023)"),
    ("tarim1", "celtik", "Çeltik (Pirinç)", Q_PROD, ("Edirne", "Samsun", "Balıkesir"),
     "Çeltik bol su ister; Edirne (Meriç–Ergene) ilk sıradadır, Samsun (Bafra–Çarşamba) ve Balıkesir izler.", "TÜİK / KPSS"),
    ("tarim1", "seker-pancari", "Şeker Pancarı", Q_PROD, ("Konya", "Eskişehir", "Afyonkarahisar", "Kayseri"),
     "Şeker pancarı üretiminin yaklaşık üçte biri Konya'dadır; fabrikalar üretim alanlarına yakın kurulur.", "TÜİK / KPSS"),
    ("tarim1", "aycicegi", "Ayçiçeği", Q_PROD, ("Tekirdağ", "Edirne", "Konya", "Kırklareli"),
     "Ayçiçeği üretiminde Trakya (Tekirdağ, Edirne, Kırklareli) ve Konya öne çıkar.", "TÜİK / KPSS"),
    ("tarim1", "pamuk", "Pamuk", Q_PROD, ("Şanlıurfa", "Aydın", "Diyarbakır"),
     "Pamuk üretiminin yaklaşık %40'ı Şanlıurfa'dadır (GAP–Harran); Aydın ve Diyarbakır izler.", "Tarım ve Orman Bakanlığı TEPGE (2023)"),
    ("tarim1", "tutun", "Tütün", Q_PROD, ("Manisa", "Adıyaman", "İzmir", "Denizli"),
     "Tütün üretiminde Ege (Manisa, İzmir, Denizli) ve Güneydoğu'da Adıyaman öne çıkar.", "TÜİK / KPSS"),
    ("tarim1", "patates", "Patates", Q_PROD, ("Niğde", "Nevşehir", "Konya"),
     "Patates üretiminde Niğde–Nevşehir yöresi ve Konya öne çıkar.", "MEB / KPSS"),
    ("tarim1", "nohut", "Nohut", Q_PROD, ("Ankara", "Yozgat", "Kırşehir"),
     "Nohut üretiminde Ankara, Yozgat ve Kırşehir ilk sıralardadır.", "Tarım ve Orman Bakanlığı TEPGE (2022)"),
    ("tarim1", "kirmizi-mercimek", "Kırmızı Mercimek", Q_PROD, ("Şanlıurfa", "Diyarbakır", "Mardin"),
     "Kırmızı mercimek Güneydoğu'da yetişir; Diyarbakır, Şanlıurfa ve Mardin üretimin büyük kısmını karşılar.", "Tarım ve Orman Bakanlığı (2018)"),
    ("tarim1", "hashas", "Haşhaş", Q_PROD, ("Afyonkarahisar", "Uşak", "Denizli", "Burdur"),
     "Haşhaş yalnızca devlet izniyle (TMO denetiminde) belirli illerde ekilir; Afyonkarahisar ilk sıradadır.", "TMO / KPSS"),
    # ---- Tarım II: Meyve ve bahçe ürünleri (en fazla üretildiği iller) ----
    ("tarim2", "findik", "Fındık", Q_PROD, ("Ordu", "Giresun", "Samsun", "Sakarya"),
     "Fındık üretiminde Ordu ilk sıradadır; Giresun, Samsun ve Sakarya izler.", "TÜİK / KPSS"),
    ("tarim2", "cay", "Çay", Q_PROD, ("Rize", "Trabzon"),
     "Çay üretiminin büyük kısmı Rize'dedir; Trabzon ikinci sıradadır.", "TÜİK / KPSS"),
    ("tarim2", "zeytin", "Zeytin", Q_PROD, ("Aydın", "İzmir", "Muğla", "Manisa"),
     "Zeytin Akdeniz iklimine özgüdür; üretimde Aydın ilk sıradadır, Ege illeri öne çıkar.", "TÜİK / KPSS"),
    ("tarim2", "uzum", "Üzüm", Q_PROD, ("Manisa", "Denizli"),
     "Üzüm üretiminde Manisa (sultaniye) ilk sıradadır; Denizli izler.", "TÜİK / KPSS"),
    ("tarim2", "incir", "İncir", Q_PROD, ("Aydın", "İzmir"),
     "İncir üretiminin %55,9'u Aydın'da, %22'si İzmir'dedir.", "Tarım ve Orman Bakanlığı TEPGE (2025)"),
    ("tarim2", "kayisi", "Kayısı", Q_PROD, ("Malatya",),
     "Kayısı üretiminin merkezi Malatya'dır; Türkiye kuru kayısıda dünya lideridir.", "TEPGE / KPSS"),
    ("tarim2", "elma", "Elma", Q_PROD, ("Isparta", "Karaman", "Niğde"),
     "Elma üretiminde Isparta ilk sıradadır; Karaman ve Niğde izler.", "TÜİK / KPSS"),
    ("tarim2", "antep-fistigi", "Antep Fıstığı", Q_PROD, ("Gaziantep", "Şanlıurfa"),
     "Antep fıstığı üretiminde Gaziantep ve Şanlıurfa ilk iki sıradadır; en geniş alan Şanlıurfa'dadır.", "TEPGE / akademik çalışma"),
    ("tarim2", "muz", "Muz", Q_PROD, ("Mersin", "Antalya"),
     "Muz, mikroklima özelliği gösteren Anamur (Mersin) ve Alanya–Gazipaşa (Antalya) kıyılarında yetişir.", "MEB / KPSS"),
    ("tarim2", "turuncgil", "Turunçgiller", Q_PROD, ("Mersin", "Adana", "Antalya", "Hatay"),
     "Portakal, mandalina ve limon kışları ılık geçen Akdeniz kıyılarında yetişir.", "TÜİK / KPSS"),
    ("tarim2", "limon", "Limon", Q_PROD, ("Mersin",),
     "Limon üretiminde Mersin (özellikle Erdemli) ilk sıradadır.", "TBMM kayıtları / KPSS"),
    ("tarim2", "gul", "Yağ Gülü", Q_PROD, ("Isparta", "Burdur"),
     "Gül yağı üretiminde Isparta dünyada öne çıkar; Burdur izler.", "MEB / KPSS"),
    # ---- Hayvancılık (hayvan türüne göre en fazla yetiştirildiği iller) ----
    ("hayvan", "sigir", "Sığır (Büyükbaş)", Q_ANIMAL, ("Erzurum", "Kars", "Ardahan"),
     "Büyükbaş hayvancılık, yaz yağışlarıyla gürleşen çayırların bulunduğu Erzurum–Kars yöresinde yaygındır.", "MEB / KPSS"),
    ("hayvan", "koyun", "Koyun", Q_ANIMAL, ("Van", "Konya", "Şanlıurfa", "Diyarbakır"),
     "Koyun, bozkır ve step alanlarında yaygındır; küçükbaş sayısında Van, Konya ve Şanlıurfa ilk üçtedir.", "Tarım ve Orman Bakanlığı (2024) / KPSS"),
    ("hayvan", "kil-kecisi", "Kıl Keçisi", Q_ANIMAL, ("Mersin", "Antalya", "Şırnak"),
     "Kıl keçisi kalkerli, makilik Akdeniz yamaçlarında yaygındır; en çok keçi Mersin, Antalya ve Şırnak'tadır.", "TÜİK (2021)"),
    ("hayvan", "tiftik-kecisi", "Tiftik (Ankara) Keçisi", Q_ANIMAL, ("Ankara", "Eskişehir"),
     "Tiftik (Ankara) keçisi İç Anadolu'da Ankara ve Eskişehir çevresinde yetiştirilir; tiftik (mohair) elde edilir.", "MEB / KPSS"),
    ("hayvan", "manda", "Manda", Q_ANIMAL, ("Samsun", "Diyarbakır", "İstanbul"),
     "Manda sulak ve bataklık alanları sever; Samsun ve Diyarbakır başta olmak üzere İstanbul'da da yetiştirilir.", "Akademik çalışma (TÜİK verisi)"),
    # ---- Arıcılık ve ipek böcekçiliği ----
    ("ari", "kovan", "Arı (Kovan Sayısı)", "EN ÇOK KOVAN BULUNAN BİR İL SEÇ", ("Muğla", "Ordu", "Adana", "Mersin", "İzmir"),
     "Kovan sayısında Muğla, Ordu, Adana, Mersin ve İzmir ilk sıralardadır.", "Tarım ve Orman Bakanlığı (2023)"),
    ("ari", "bal-uretimi", "Bal Üretimi", "EN FAZLA BAL ÜRETEN BİR İL SEÇ", ("Ordu", "Adana", "Muğla", "Sivas", "İzmir"),
     "2023'te en fazla bal Ordu, Adana, Muğla, Sivas ve İzmir'de üretilmiştir.", "Tarım ve Orman Bakanlığı (2023)"),
    ("ari", "cam-bali", "Çam Balı", "ÇAM BALI ÜRETİMİNDE ÖNE ÇIKAN İLİ SEÇ", ("Muğla",),
     "Çam balı, çam ağaçlarının özsuyunu emen böceğin salgısından elde edilir ve Muğla'da yoğun üretilir.", "MEB / KPSS"),
    ("ari", "anzer-bali", "Anzer Balı", "ANZER BALININ ÜRETİLDİĞİ İLİ SEÇ", ("Rize",),
     "Anzer balı, Rize'nin İkizdere yöresindeki Anzer Yaylası'nda yüksek yayla çiçeklerinden elde edilir.", "MEB / KPSS"),
    ("ari", "ipekbocegi", "İpek Böceği", "YETİŞTİRİLDİĞİ BİR İL SEÇ", ("Bursa", "Bilecik", "Diyarbakır"),
     "İpek böceği dut yaprağıyla beslenir; Bursa ve Bilecik gelenekseldir, Diyarbakır da öne çıkar.", "MEB / KPSS"),
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
        "id": "plaintype",
        "prompt": "BU OVA OLUŞUMUNA GÖRE HANGİ TÜRDENDİR?",
        "choices": ["Delta", "Tektonik", "Karstik (polye)", "Göl tabanı"],
        "colors": ["#7FB7E3", "#81B29A", "#C3A6E0", "#F2CC8F"],
        "infos": [
            "Delta ovası: akarsuların denize döküldüğü yerde biriktirdiği alüvyonlarla oluşur (Çukurova, Bafra, Çarşamba, Silifke, Söke, Menemen).",
            "Tektonik ova: fay hatları boyunca çöken alanlarda oluşur; Ege'deki graben ovaları ve Doğu Anadolu'daki çöküntü ovaları bu türdendir.",
            "Karstik ova (polye): kalkerli arazide erime sonucu oluşan geniş çukurlardır (Elmalı, Muğla, Tefenni, Acıpayam, Kestel, Korkuteli).",
            "Göl tabanı ovası: eski bir gölün çekilmesi ya da dolmasıyla oluşur; Konya Ovası en bilinen örnektir.",
        ],
        "groups": [],
    },
    {
        "id": "plateautype",
        "prompt": "BU PLATO OLUŞUMUNA GÖRE HANGİ TÜRDENDİR?",
        "choices": ["Aşınım", "Tabaka", "Karstik", "Lav örtülü"],
        "colors": ["#B8B8D1", "#F2CC8F", "#81B29A", "#E07A5F"],
        "infos": [
            "Aşınım platosu: uzun süreli aşınmayla alçalıp düzleşmiş eski dağlık alanlardır (Çatalca–Kocaeli).",
            "Tabaka (örtü) platosu: yatay tortul tabakaların akarsularla yarılmasıyla oluşur (Bozok, Uzunyayla, Yazılıkaya, Gaziantep, Şanlıurfa).",
            "Karstik plato: kalkerli arazide oluşmuş, yüzeyde suyun az olduğu platolardır (Teke, Taşeli).",
            "Lav örtülü plato: geniş lav akıntılarının örttüğü düzlüklerdir (Erzurum–Kars, Ardahan).",
        ],
        "groups": [],
    },
    {
        "id": "mountaintype",
        "prompt": "BU DAĞ OLUŞUMUNA GÖRE HANGİ TÜRDENDİR?",
        "choices": ["Kıvrım", "Kırık (horst)"],
        "colors": ["#C9A26B", "#81B29A"],
        "infos": [
            "Kıvrım dağı: tortul tabakaların yan basınçla kıvrılmasıyla oluşur; Kuzey Anadolu Dağları ve Toroslar bu türdendir.",
            "Kırık dağı (horst): sert kütlelerin faylarla kırılıp yükselmesiyle oluşur; Ege'deki Kaz Dağı, Madra, Yunt, Bozdağlar, Aydın Dağları, Menteşe ve Uludağ bu türdendir.",
        ],
        "groups": [],
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
