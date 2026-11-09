"""
Müfredat eksikleri için EK konu setleri: konum, enlem-boylam, iklim (sıcaklık-yağış), dış kuvvetler, doğal afetler,
sanayi, ulaşım, göç ve yerleşme, bölgesel kalkınma projeleri.
Ayrı dosyaya yazılır (Resources/Data/topics_ek.json) ki topics.json ile birleştirme çakışması olmasın;
oyun iki dosyayı birlikte okur (TopicDatabase.Load). Bölümleri: Resources/Data/levels_ek.json.
Kullanım: python Tools/content/build_topics_ek.py
Not: Listeler KPSS kaynaklarında "öne çıkan iller"dir; yanlış yazılan il adı hata verir.
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "geo"))
from build_provinces import NAMES  # noqa: E402

DST = os.path.join(HERE, "..", "..", "Assets", "_Project", "Resources", "Data", "topics_ek.json")
PROVINCES_JSON = os.path.join(HERE, "..", "..", "Assets", "_Project", "Resources", "Data", "turkey_provinces.json")
PLATE = {v: k for k, v in NAMES.items()}
PARALLELS = (37, 38, 39, 40, 41)
MERIDIANS = (30, 32, 36, 40, 44)
S = "BİR İL SEÇ"

TOPICS = [
    # ---- Konum: uç noktalar ve güneş (matematik konumun sonuçları) ----
    ("konum", "en-dogu", "En Doğudaki İl", S, ("Iğdır",),
     "Türkiye'nin en doğu noktası Iğdır'ın Aralık ilçesindeki Dilucu (44° 48' D). Yerel saat en ileri buradadır.", "KPSS"),
    ("konum", "en-bati", "En Batıdaki İl", S, ("Çanakkale",),
     "En batı noktası Gökçeada'daki Avlaka Burnu (25° 40' D); karadaki en batı nokta Baba Burnu. İkisi de Çanakkale'dedir.", "KPSS"),
    ("konum", "en-kuzey", "En Kuzeydeki İl", S, ("Sinop",),
     "En kuzey noktası Sinop'taki İnceburun (42° 06' K). Kuzey Kutbu'na en yakın il Sinop'tur.", "KPSS"),
    ("konum", "en-guney", "En Güneydeki İl", S, ("Hatay",),
     "En güney noktası Hatay Yayladağı'ndaki Beysun köyü (35° 49' K). Ekvator'a en yakın il Hatay'dır.", "KPSS"),
    ("konum", "gunes-ilk", "Güneşin İlk Doğduğu İl", S, ("Iğdır",),
     "Dünya batıdan doğuya döndüğü için güneş Türkiye'de ilk olarak en doğudaki Iğdır'da doğar.", "KPSS"),
    ("konum", "gunes-son", "Güneşin En Son Battığı İl", S, ("Çanakkale",),
     "Güneş en son en batıdaki Çanakkale'de batar. Doğu ile batı arasında 19 boylam × 4 dk = 76 dakika yerel saat farkı vardır.", "KPSS"),
    ("konum", "golge-uzun", "Öğle Gölgesi En Uzun", S, ("Sinop",),
     "Kuzeye gidildikçe güneş ışınlarının geliş açısı küçülür; aynı anda öğle vakti cisimlerin gölgesi en uzun Sinop'ta olur.", "KPSS"),
    ("konum", "golge-kisa", "Öğle Gölgesi En Kısa", S, ("Hatay",),
     "En güneydeki Hatay'da güneş ışınları en büyük açıyla gelir; öğle gölgesi en kısadır. Türkiye'de ışınlar hiçbir zaman dik gelmez.", "KPSS"),
    ("konum", "gunduz-uzun", "21 Haziran'da Gündüzü En Uzun", S, ("Sinop",),
     "Kuzey Yarım Küre'de yaz gün dönümünde (21 Haziran) kuzeye gidildikçe gündüz uzar; en uzun gündüz Sinop'ta yaşanır. 21 Aralık'ta ise en kısa gündüz Sinop'tadır.", "KPSS"),

    # ---- İklim: sıcaklık ve yağış (il bazlı uç değerler) ----
    ("iklim2", "yagis-cok", "Yıllık Yağış En Fazla", S, ("Rize", "Artvin", "Trabzon", "Giresun"),
     "Nemli kuzey rüzgârları Doğu Karadeniz Dağları'na çarpıp yükselir (orografik yağış). Rize yılda 2.000 mm'yi aşan yağışla ilk sıradadır; yağış her mevsime dağılmıştır.", "MGM / KPSS"),
    ("iklim2", "yagis-az", "Yıllık Yağış En Az", S, ("Iğdır", "Konya", "Aksaray", "Karaman"),
     "Denizden uzak, çevresi dağlarla kapalı çukur alanlar az yağış alır. Iğdır ile Tuz Gölü–Konya havzası Türkiye'nin en kurak yerleridir (yaklaşık 250–350 mm).", "MGM / KPSS"),
    ("iklim2", "kis-ilik", "Kışları En Ilık", S, ("Antalya", "Mersin", "Hatay", "Adana"),
     "Enlemin düşük olması ve Toros Dağları'nın soğuk kuzey rüzgârlarını engellemesi nedeniyle kışlar en ılık Akdeniz kıyılarında geçer; Alanya–Anamur arasında muz yetişir.", "MGM / KPSS"),
    ("iklim2", "kis-soguk", "Kışları En Soğuk", S, ("Erzurum", "Kars", "Ardahan", "Ağrı"),
     "Yükseltinin fazla ve denizden uzak olması nedeniyle Erzurum–Kars yöresinde kışlar çok sert geçer; en düşük sıcaklıklar burada ölçülür, kar örtüsü yerde en uzun süre kalır.", "MGM / KPSS"),
    ("iklim2", "yaz-sicak", "Yazları En Sıcak", S, ("Şanlıurfa", "Diyarbakır", "Batman", "Şırnak", "Siirt"),
     "Güneydoğu Anadolu'ya güneyden sıcak ve kuru hava gelir; denizden uzaklık ve alçak çanak yapısı nedeniyle Türkiye'nin en yüksek yaz sıcaklıkları burada (Cizre, Silopi, Diyarbakır) ölçülür.", "MGM / KPSS"),

    # ---- Dış kuvvetlerin oluşturduğu yer şekilleri ----
    ("dis", "peri-bacasi", "Peri Bacaları", "GÖRÜLDÜĞÜ BİR İL SEÇ", ("Nevşehir", "Kayseri", "Aksaray"),
     "Volkanik tüflerin akarsu ve sel suları tarafından aşındırılmasıyla oluşur; üstteki sert kayaç başlık gibi kalır. Kapadokya (Göreme, Ürgüp) en bilinen örnektir.", "KPSS"),
    ("dis", "traverten", "Traverten", "GÖRÜLDÜĞÜ BİR İL SEÇ", ("Denizli",),
     "Kalsiyum bikarbonatlı sıcak suların yüzeye çıkıp çökeltmesiyle oluşan basamaklı karstik birikim şekli; en ünlü örnek Pamukkale'dir.", "KPSS"),
    ("dis", "obruk", "Obruk", "GÖRÜLDÜĞÜ BİR İL SEÇ", ("Konya", "Aksaray"),
     "Kalker ve jipsli arazide yer altındaki boşlukların tavanının çökmesiyle oluşan derin, dik kenarlı çukurlar; Konya Karapınar ve çevresinde yaygındır.", "KPSS"),
    ("dis", "magara", "Karstik Mağaralar", "BULUNDUĞU BİR İL SEÇ", ("Antalya", "Mersin", "Zonguldak", "Isparta"),
     "Kalkerin yer altı sularıyla erimesiyle oluşur; içinde sarkıt, dikit ve sütunlar bulunur (Damlataş, Karain, Cennet–Cehennem, Gökgöl, Zindan).", "KPSS"),
    ("dis", "kumul", "Kumul (Rüzgâr Birikimi)", "GÖRÜLDÜĞÜ BİR İL SEÇ", ("Konya",),
     "Kurak ve bitki örtüsü zayıf alanlarda rüzgârın taşıdığı kumların birikmesiyle oluşur; Karapınar'da kumulları durdurmak için ağaçlandırma yapılmıştır.", "KPSS"),
    ("dis", "buzul", "Buzul Şekilleri (Sirk Gölü)", "GÖRÜLDÜĞÜ BİR İL SEÇ", ("Rize", "Hakkari", "Ağrı", "Niğde"),
     "Yüksek dağlarda buzulların aşındırdığı çanaklarda sirk gölleri oluşur; Kaçkar, Cilo–Sat, Ağrı ve Aladağlar'da görülür. Ağrı Dağı'nda güncel buzul vardır.", "KPSS"),
    ("dis", "kanyon", "Kanyon Vadi", "BULUNDUĞU BİR İL SEÇ", ("Antalya", "Uşak", "Aksaray", "Muğla", "Kastamonu"),
     "Kalker gibi dirençli ama çatlaklı kayaçlarda akarsuyun derine aşındırmasıyla oluşan dik yamaçlı vadi: Köprülü (Antalya), Ulubey (Uşak), Ihlara (Aksaray), Saklıkent (Muğla), Valla (Kastamonu).", "KPSS"),
    ("dis", "delta", "Delta Ovası", "BULUNDUĞU BİR İL SEÇ", ("Samsun", "Adana", "Mersin", "Aydın", "İzmir"),
     "Akarsuyun taşıdığı alüvyonları denize döküldüğü yerde biriktirmesiyle oluşur: Bafra ve Çarşamba (Samsun), Çukurova (Adana), Silifke (Mersin), Balat (Aydın), Menemen (İzmir).", "KPSS"),
    ("dis", "polye", "Polye (Karstik Ova)", "GÖRÜLDÜĞÜ BİR İL SEÇ", ("Antalya", "Muğla", "Burdur", "Denizli"),
     "Kalkerli arazide erimeyle oluşan geniş, düz tabanlı çukurlar; tarım yapılır: Elmalı ve Korkuteli (Antalya), Muğla, Tefenni ve Kestel (Burdur), Acıpayam (Denizli).", "KPSS"),

    # ---- Doğal afetler ----
    ("afet", "deprem-az", "Deprem Riski En Az", S, ("Karaman", "Konya", "Kırklareli", "Edirne"),
     "Fay hatlarından uzak, eski ve sert kütleler az risklidir: Konya–Karaman çevresi, Taşeli Platosu, Ergene Havzası ve Mardin Eşiği.", "KPSS / AFAD"),
    ("afet", "heyelan", "Heyelan En Sık", S, ("Rize", "Trabzon", "Artvin", "Giresun", "Ordu"),
     "Heyelan en çok eğimin fazla, yağışın bol ve arazinin killi olduğu Doğu Karadeniz'de, özellikle ilkbaharda kar erimeleriyle görülür.", "KPSS / AFAD"),
    ("afet", "cig", "Çığ En Sık", S, ("Hakkari", "Bitlis", "Tunceli", "Bingöl", "Rize"),
     "Çığ, eğimli ve kar yağışının fazla olduğu dağlık alanlarda görülür: Doğu Anadolu'nun yüksek kesimleri ve Doğu Karadeniz dağları.", "KPSS / AFAD"),
    ("afet", "ruzgar-erozyonu", "Rüzgâr Erozyonu", "EN ŞİDDETLİ OLDUĞU BİR İL SEÇ", ("Konya", "Iğdır"),
     "Kurak ve bitki örtüsü zayıf alanlarda rüzgâr toprağı süpürür: Karapınar (Konya) ve Aralık (Iğdır) en bilinen örneklerdir.", "KPSS"),
    ("afet", "deprem-1939", "1939 Depremi", "MERKEZ ÜSSÜ OLAN İLİ SEÇ", ("Erzincan",),
     "27 Aralık 1939 Erzincan depremi (7,9), Kuzey Anadolu Fayı üzerindedir; Cumhuriyet tarihinin en büyük depremlerindendir.", "AFAD"),
    ("afet", "deprem-1999", "17 Ağustos 1999 Depremi", "MERKEZ ÜSSÜ OLAN İLİ SEÇ", ("Kocaeli",),
     "17 Ağustos 1999 Gölcük (Marmara) depremi, Kuzey Anadolu Fayı'nın batı kesiminde meydana gelmiştir.", "AFAD"),
    ("afet", "deprem-duzce", "12 Kasım 1999 Depremi", "MERKEZ ÜSSÜ OLAN İLİ SEÇ", ("Düzce",),
     "Gölcük depreminden üç ay sonra, yine Kuzey Anadolu Fayı üzerinde Düzce'de meydana gelmiştir.", "AFAD"),
    ("afet", "deprem-2011", "2011 Depremi", "MERKEZ ÜSSÜ OLAN İLİ SEÇ", ("Van",),
     "23 Ekim 2011 Van depremi (7,2) Doğu Anadolu'da meydana gelmiştir.", "AFAD"),
    ("afet", "deprem-2023", "6 Şubat 2023 Depremleri", "MERKEZ ÜSSÜ OLAN İLİ SEÇ", ("Kahramanmaraş",),
     "Pazarcık ve Elbistan merkezli iki büyük deprem Doğu Anadolu Fayı kuşağında meydana gelmiş, 11 ili etkilemiştir.", "AFAD"),

    # ---- Sanayi ----
    ("sanayi", "demir-celik", "Demir-Çelik Fabrikası", "BULUNDUĞU BİR İL SEÇ", ("Karabük", "Zonguldak", "Hatay"),
     "Karabük (1939, ilk fabrika; taşkömürüne yakınlık), Ereğli–Zonguldak (taşkömürü ve liman), İskenderun–Hatay (liman).", "KPSS"),
    ("sanayi", "rafineri", "Petrol Rafinerisi", "BULUNDUĞU BİR İL SEÇ", ("Kocaeli", "İzmir", "Kırıkkale", "Batman"),
     "İzmit (Kocaeli), Aliağa (İzmir), Kırıkkale ve Batman rafinerileri. Batman hammaddeye, diğerleri pazara ve limana yakınlık nedeniyle kurulmuştur.", "KPSS"),
    ("sanayi", "otomotiv", "Otomotiv Sanayisi", "EN GELİŞMİŞ OLDUĞU BİR İL SEÇ", ("Bursa", "Kocaeli", "Sakarya"),
     "Otomotiv fabrikaları pazara, limanlara ve yan sanayiye yakın Doğu Marmara'da toplanmıştır.", "KPSS"),
    ("sanayi", "aluminyum", "Alüminyum Tesisi", "BULUNDUĞU İLİ SEÇ", ("Konya",),
     "Seydişehir Alüminyum Tesisleri, boksit yataklarına (hammaddeye) yakınlık nedeniyle Konya'da kurulmuştur.", "KPSS"),
    ("sanayi", "cay", "Çay Fabrikaları", "YOĞUNLAŞTIĞI BİR İL SEÇ", ("Rize", "Trabzon", "Artvin", "Giresun"),
     "Çay yaprağı toplandıktan sonra kısa sürede işlenmelidir; fabrikalar hammaddenin yetiştiği Doğu Karadeniz'dedir.", "KPSS"),
    ("sanayi", "pamuklu", "Pamuklu Dokuma", "GELİŞMİŞ OLDUĞU BİR İL SEÇ", ("Adana", "Denizli", "Gaziantep", "Kahramanmaraş"),
     "Pamuklu dokuma sanayisi pamuğun yetiştiği Çukurova, Ege ve Güneydoğu'da gelişmiştir.", "KPSS"),
    ("sanayi", "gemi", "Gemi Yapımı (Tersane)", "GELİŞMİŞ OLDUĞU BİR İL SEÇ", ("İstanbul", "Yalova", "Kocaeli"),
     "Tersaneler Tuzla (İstanbul), Altınova (Yalova) ve İzmit Körfezi çevresinde toplanmıştır.", "KPSS"),
    ("sanayi", "lokomotif", "Lokomotif ve Vagon", "ÜRETİLDİĞİ BİR İL SEÇ", ("Eskişehir", "Sakarya", "Sivas"),
     "Demir yolu araçları Eskişehir (lokomotif), Sakarya (yolcu vagonu) ve Sivas'ta (yük vagonu) üretilir.", "KPSS"),

    # ---- Ulaşım ----
    ("ulasim", "liman", "Büyük Limanlar", "BULUNDUĞU BİR İL SEÇ", ("İstanbul", "Kocaeli", "İzmir", "Mersin", "Samsun", "Hatay"),
     "Limanların gelişmesinde hinterlant (art bölge) belirleyicidir: Mersin Çukurova ve İç Anadolu'ya, İzmir Ege'ye, Samsun Karadeniz ve İç Anadolu'ya açılır.", "KPSS"),
    ("ulasim", "boru-hatti", "Petrol Boru Hatlarının Ulaştığı Liman", "BULUNDUĞU İLİ SEÇ", ("Adana",),
     "Bakü–Tiflis–Ceyhan ve Kerkük–Yumurtalık ham petrol boru hatları Adana'da Akdeniz'e ulaşır.", "KPSS"),
    ("ulasim", "marmaray", "Marmaray", "BULUNDUĞU İLİ SEÇ", ("İstanbul",),
     "Marmaray (2013), İstanbul Boğazı'nın altından geçen demir yolu tüp tüneliyle Avrupa ve Asya yakalarını bağlar.", "KPSS"),
    ("ulasim", "osmangazi", "Osmangazi Köprüsü", "BAĞLADIĞI BİR İL SEÇ", ("Kocaeli", "Yalova"),
     "İzmit Körfezi'ni Dilovası (Kocaeli) ile Altınova (Yalova) arasında geçer; İstanbul–İzmir yolunu kısaltır.", "KPSS"),
    ("ulasim", "canakkale-koprusu", "1915 Çanakkale Köprüsü", "BULUNDUĞU İLİ SEÇ", ("Çanakkale",),
     "Çanakkale Boğazı'nı Lapseki ile Gelibolu arasında geçen, dünyanın en uzun orta açıklıklı asma köprüsü (2022).", "KPSS"),
    ("ulasim", "yht", "İlk Yüksek Hızlı Tren Hattı", "UÇLARINDAKİ BİR İL SEÇ", ("Ankara", "Eskişehir"),
     "Türkiye'nin ilk yüksek hızlı tren hattı 2009'da Ankara–Eskişehir arasında açılmıştır.", "KPSS"),
    ("ulasim", "feribot", "Van Gölü Feribotu", "BAĞLADIĞI BİR İL SEÇ", ("Bitlis", "Van"),
     "Demir yolu, Van Gölü'nü Tatvan (Bitlis) ile Van arasında feribotla geçer.", "KPSS"),
    ("ulasim", "gulek", "Gülek Boğazı", "BULUNDUĞU İLİ SEÇ", ("Mersin",),
     "Toroslar üzerinde, İç Anadolu'yu Çukurova'ya bağlayan tarihî geçit (Tarsus, Mersin).", "KPSS"),
    ("ulasim", "belen", "Belen Geçidi", "BULUNDUĞU İLİ SEÇ", ("Hatay",),
     "Nur (Amanos) Dağları üzerinde, İskenderun'u Antakya ve Güneydoğu'ya bağlar.", "KPSS"),
    ("ulasim", "zigana", "Zigana Geçidi", "BAĞLADIĞI BİR İL SEÇ", ("Trabzon", "Gümüşhane"),
     "Doğu Karadeniz Dağları üzerinde, Trabzon limanını Gümüşhane üzerinden Doğu Anadolu'ya bağlar.", "KPSS"),
    ("ulasim", "kop", "Kop Geçidi", "BAĞLADIĞI BİR İL SEÇ", ("Bayburt", "Erzurum"),
     "Bayburt ile Erzurum arasında; Trabzon–Erzurum–İran transit yolu üzerindedir.", "KPSS"),
    ("ulasim", "cubuk", "Çubuk Boğazı", "BAĞLADIĞI BİR İL SEÇ", ("Antalya", "Burdur"),
     "Batı Toroslar üzerinde, Antalya'yı Göller Yöresi ve İç Anadolu'ya bağlar.", "KPSS"),
    ("ulasim", "sertavul", "Sertavul Geçidi", "BAĞLADIĞI BİR İL SEÇ", ("Karaman", "Mersin"),
     "Orta Toroslar (Taşeli) üzerinde, Karaman'ı Silifke ve Akdeniz kıyısına bağlar.", "KPSS"),

    # ---- Göç ve yerleşme ----
    ("yerlesme", "goc-alan", "Göç Alan İller", S, ("İstanbul", "Kocaeli", "Bursa", "İzmir", "Ankara", "Antalya", "Tekirdağ"),
     "Sanayi, ticaret, turizm ve hizmetlerin geliştiği iller iş olanakları nedeniyle göç alır.", "TÜİK / KPSS"),
    ("yerlesme", "goc-veren", "Göç Veren İller", S, ("Ağrı", "Muş", "Kars", "Ardahan", "Erzurum"),
     "İklimin sert, tarım alanının dar ve iş olanaklarının az olduğu Doğu Anadolu illeri göç verir.", "TÜİK / KPSS"),
    ("yerlesme", "mevsimlik-tarim", "Mevsimlik Tarım İşçisi Göçü", "ALAN BİR İL SEÇ", ("Adana", "Ordu", "Giresun"),
     "Hasat döneminde Çukurova'ya (pamuk) ve Doğu Karadeniz'e (fındık) mevsimlik işçi göçü olur.", "KPSS"),
    ("yerlesme", "turizm-goc", "Yazın Turizm Nedeniyle Nüfusu Artan", S, ("Antalya", "Muğla", "Aydın"),
     "Akdeniz ve Ege kıyılarında yaz turizmi mevsimlik iş göçüne ve nüfus artışına yol açar.", "KPSS"),
    ("yerlesme", "maden-kenti", "Madencilikle Gelişen Kent", S, ("Zonguldak", "Batman"),
     "Zonguldak taşkömürü, Batman petrol sayesinde gelişmiş maden kentleridir.", "KPSS"),
    ("yerlesme", "idari", "İdari Fonksiyonuyla Gelişen Kent", S, ("Ankara",),
     "Ankara, başkent olduktan sonra idari fonksiyonuyla hızla büyümüştür.", "KPSS"),
    ("yerlesme", "daginik", "Dağınık Yerleşmenin Yaygın Olduğu", S, ("Rize", "Trabzon", "Artvin", "Giresun"),
     "Doğu Karadeniz'de su kaynaklarının bol, arazinin engebeli ve tarım alanlarının parçalı olması dağınık yerleşmeye yol açar.", "KPSS"),
    ("yerlesme", "kom", "Kom Yerleşmesinin Görüldüğü", S, ("Erzurum", "Kars", "Ağrı"),
     "Kom, Doğu Anadolu'da hayvancılık amacıyla kullanılan, ahır ve ağıllardan oluşan geçici yerleşmedir.", "KPSS"),
    ("yerlesme", "kerpic", "Kerpiç Meskenlerin Yaygın Olduğu", S, ("Konya", "Aksaray", "Kırşehir", "Karaman"),
     "Mesken malzemesini doğal çevre belirler: kurak İç Anadolu'da kerpiç, ormanlık Karadeniz'de ahşap, Akdeniz ve volkanik alanlarda taş.", "KPSS"),

    # ---- Bölgesel kalkınma projeleri ----
    ("proje", "gap", "GAP (Güneydoğu Anadolu Projesi)", "KAPSADIĞI BİR İL SEÇ",
     ("Adıyaman", "Batman", "Diyarbakır", "Gaziantep", "Kilis", "Mardin", "Siirt", "Şanlıurfa", "Şırnak"),
     "Türkiye'nin en büyük bölgesel kalkınma projesi; Fırat ve Dicle üzerindeki barajlarla sulama ve enerji üretimini amaçlar. 9 ili kapsar.", "GAP İdaresi"),
    ("proje", "dap", "DAP (Doğu Anadolu Projesi)", "KAPSADIĞI BİR İL SEÇ",
     ("Ağrı", "Ardahan", "Bingöl", "Bitlis", "Elazığ", "Erzincan", "Erzurum", "Hakkari", "Iğdır", "Kars", "Malatya", "Muş", "Tunceli", "Van"),
     "Doğu Anadolu'da hayvancılık, tarım ve sanayiyi geliştirmeyi, göçü azaltmayı amaçlar.", "DAP İdaresi"),
    ("proje", "dokap", "DOKAP (Doğu Karadeniz Projesi)", "KAPSADIĞI BİR İL SEÇ",
     ("Artvin", "Bayburt", "Giresun", "Gümüşhane", "Ordu", "Rize", "Samsun", "Tokat", "Trabzon"),
     "Doğu Karadeniz'de ulaşım, turizm (yayla) ve tarımı geliştirmeyi, göçü azaltmayı amaçlar.", "DOKAP İdaresi"),
    ("proje", "kop", "KOP (Konya Ovası Projesi)", "KAPSADIĞI BİR İL SEÇ",
     ("Konya", "Karaman", "Aksaray", "Niğde", "Nevşehir", "Yozgat", "Kırıkkale", "Kırşehir"),
     "Kurak Konya Havzası'nda sulamayı geliştirmeyi amaçlar; Mavi Tünel ile Göksu'dan Konya Ovası'na su aktarılır.", "KOP İdaresi"),
    ("proje", "zbk", "ZBK (Zonguldak–Bartın–Karabük) Projesi", "KAPSADIĞI BİR İL SEÇ", ("Zonguldak", "Bartın", "Karabük"),
     "Taşkömürü ve demir-çeliğe bağımlı bölgede ekonomiyi çeşitlendirmeyi amaçlar.", "KPSS"),
    ("proje", "yesilirmak", "Yeşilırmak Havza Gelişim Projesi", "KAPSADIĞI BİR İL SEÇ", ("Amasya", "Çorum", "Samsun", "Tokat"),
     "Yeşilırmak havzasında taşkın, erozyon ve kirliliği önleyerek doğal kaynakların dengeli kullanımını amaçlar.", "KPSS"),
]


def plates(*names):
    return [PLATE[n] for n in names]


def line_topics():
    """Enlem ve boylamların geçtiği illeri il sınırlarından hesaplar (turkey_provinces.json, projeksiyon geri çevrilir)."""
    from shapely.geometry import LineString, Polygon
    from shapely.ops import unary_union
    data = json.load(open(PROVINCES_JSON, encoding="utf-8"))
    lon0, lat0, kx, ky = data["projection"]
    shapes = {}
    for p in data["provinces"]:
        polys = []
        for r in p["rings"]:
            pts = r["points"]
            coords = [(pts[i] / kx + lon0, pts[i + 1] / ky + lat0) for i in range(0, len(pts), 2)]
            if len(coords) >= 3:
                polys.append(Polygon(coords).buffer(0))
        shapes[p["plate"]] = unary_union(polys)
    out = []
    for lat in PARALLELS:
        line = [25.5, lat, 45.0, lat]
        hit = sorted(pl for pl, g in shapes.items() if g.intersects(LineString([(line[0], lat), (line[2], lat)])))
        out.append({"id": f"enlem-{lat}", "set": "enlem", "title": f"{lat}° Kuzey Enlemi", "question": "GEÇTİĞİ BİR İL SEÇ",
                    "plates": hit, "line": line, "source": "Natural Earth il sınırları",
                    "info": f"{lat}° K paraleli Türkiye'yi batıdan doğuya keser. Türkiye 36°–42° K enlemleri arasındadır; "
                            "1 enlem yaklaşık 111 km olduğundan kuzey–güney uzunluğu yaklaşık 660 km'dir."})
    for lon in MERIDIANS:
        line = [lon, 35.7, lon, 42.2]
        hit = sorted(pl for pl, g in shapes.items() if g.intersects(LineString([(lon, line[1]), (lon, line[3])])))
        extra = " Türkiye'nin ortak saati (UTC+3) 45° D boylamına göredir." if lon == 44 else ""
        out.append({"id": f"boylam-{lon}", "set": "enlem", "title": f"{lon}° Doğu Boylamı", "question": "GEÇTİĞİ BİR İL SEÇ",
                    "plates": hit, "line": line, "source": "Natural Earth il sınırları",
                    "info": f"{lon}° D meridyeni Türkiye'yi kuzeyden güneye keser. Türkiye 26°–45° D boylamları arasındadır; "
                            "her boylam 4 dakikalık yerel saat farkı demektir." + extra})
    return out


def main():
    topics = [{"id": tid, "set": set_id, "title": title, "question": question, "plates": plates(*names),
               "info": info, "source": source} for set_id, tid, title, question, names, info, source in TOPICS]
    topics.extend(line_topics())
    with open(DST, "w", encoding="utf-8") as fp:
        json.dump({"topics": topics, "attributes": []}, fp, ensure_ascii=False, indent=1)
    from collections import Counter
    print("Yazıldı:", os.path.normpath(DST), dict(Counter(t["set"] for t in topics)))


if __name__ == "__main__":
    main()
