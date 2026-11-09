using System.Collections.Generic;
using KPSSGeography.Data;
using KPSSGeography.Map;
using UnityEngine;

namespace KPSSGeography.Quiz
{
    public enum AnswerType
    {
        /// <summary>Haritada il ya da yer şekli tıklanır.</summary>
        Map,
        /// <summary>Alttaki butonlardan seçilir.</summary>
        Choice
    }

    public enum MapLayer
    {
        Provinces,
        Features
    }

    /// <summary>
    /// Bütün oyun modlarının ortak soru modeli. Haritadan seçmeli sorularda kabul edilen hedefler <see cref="Correct"/>'tedir
    /// (birden fazla olabilir: konu setleri, komşular). Butonlu sorularda <see cref="Choices"/> ve <see cref="CorrectChoice"/> kullanılır.
    /// </summary>
    public sealed class Question
    {
        /// <summary>Üstteki küçük yönerge ("BU İLİ HARİTADA BUL").</summary>
        public string Prompt;
        /// <summary>Büyük başlık (il adı, konu, plaka ya da soru metni).</summary>
        public string Title;
        /// <summary>Uzun metin (KPSS sorusu): küçük yazı, satır kaydırma.</summary>
        public bool LongTitle;

        public AnswerType Type;
        public MapLayer Layer;
        public readonly List<IMapTarget> Correct = new List<IMapTarget>();
        /// <summary>Hepsini bul: sorudaki bütün doğru hedefler bulunmadan soru bitmez.</summary>
        public bool RequireAll;

        public string[] Choices;
        public Color[] ChoiceColors;
        public int CorrectChoice = -1;

        /// <summary>Soru boyunca nabız gibi vurgulanan hedef (bölge/komşu/iklim sorularında il, göl tipi sorusunda göl).</summary>
        public IMapTarget Focus;
        /// <summary>Focus soru sırasında haritada vurgulanmaz (ör. komşu iller: sorulan il yalnızca adıyla verilir).</summary>
        public bool FocusHidden;
        /// <summary>Tek doğru cevaplı harita sorularında doğru hedef (yanlışta gösterilir).</summary>
        public IMapTarget Reveal;
        /// <summary>KPSS sorusunda vurgulanacak yer şekli (sadece o gösterilir).</summary>
        public FeatureView ShowFeature;

        /// <summary>KPSS tipi öncül metni (giriş + maddeler); soru kâğıdı gibi solda ayrı bir panelde gösterilir.</summary>
        public string Body;

        /// <summary>Boss savaşı: sorunun ait olduğu raund (1, 2…).</summary>
        public int Round;
        /// <summary>Bu soru başlamadan önceki ek bekleme (ör. boss değişimi animasyonu için), saniye.</summary>
        public float PreDelay;
        /// <summary>İklim grafiği sorusu: soldaki panelde çizilecek il verisi (Body yerine).</summary>
        public ClimateCity Chart;

        /// <summary>Numaralı harita sorusu: haritada sırasıyla I, II, III… diye işaretlenen yerler.</summary>
        public readonly List<IMapTarget> Markers = new List<IMapTarget>();
        /// <summary>Numaralı harita: işaret yerine oklar/doğru parçaları (I, II… sırasıyla) ve doğru olanların sırası.</summary>
        public readonly List<MapShape> Shapes = new List<MapShape>();
        public readonly List<int> CorrectShapes = new List<int>();

        /// <summary>Doğru cevaptan sonra haritada boyanacak ek hedef (sınır komşusu sorusunda sorulan il).</summary>
        public IMapTarget MarkOnCorrect;

        public string Info;
        /// <summary>"Doğru cevap: ..." metni.</summary>
        public string AnswerLabel;

        public bool Accepts(IMapTarget target) => Correct.Contains(target);

        /// <summary>
        /// Yanlış cevapta (ya da süre bitince) doğru cevap gösterilmez mi? Haritadan seçmeli sorularda gösterilmez:
        /// doğru yeri göstermek sonraki sorular için ipucu olur. Yanlış tıklanan yerin adı ise söylenir (QuizHud). "Hepsini bul" hariç.
        /// </summary>
        public bool HidesAnswerOnMiss => Type == AnswerType.Map && !RequireAll;

        /// <summary>İsim etiketinin gösterileceği hedef.</summary>
        public IMapTarget TagTarget => Reveal ?? Focus ?? (Correct.Count == 1 ? Correct[0] : null);
    }
}
