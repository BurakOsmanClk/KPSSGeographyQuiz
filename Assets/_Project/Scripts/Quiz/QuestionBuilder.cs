using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KPSSGeography.Data;
using KPSSGeography.Map;
using UnityEngine;

namespace KPSSGeography.Quiz
{
    /// <summary>Bölüm tanımından (levels.json) soru listesini üretir.</summary>
    public class QuestionBuilder
    {
        static readonly CultureInfo TR = new CultureInfo("tr-TR");

        readonly TurkeyMap map;
        readonly FeatureLayer features;
        readonly TopicDatabase topics;
        readonly McqDatabase mcq;
        readonly MapPalette palette;

        public QuestionBuilder(TurkeyMap turkeyMap, FeatureLayer featureLayer, TopicDatabase topicDb, McqDatabase mcqDb, MapPalette mapPalette)
        {
            map = turkeyMap;
            features = featureLayer;
            topics = topicDb;
            mcq = mcqDb;
            palette = mapPalette;
        }

        static string Up(string s) => s.ToUpper(TR);

        static List<T> Shuffled<T>(IEnumerable<T> source)
        {
            var list = new List<T>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        static List<T> Take<T>(IEnumerable<T> source, int count)
        {
            var s = Shuffled(source);
            return s.GetRange(0, Mathf.Clamp(count, 0, s.Count));
        }

        static string JoinNames(IEnumerable<string> names, int max = 5)
        {
            var list = names.ToList();
            string s = string.Join(", ", list.Take(max));
            return list.Count > max ? s + "…" : s;
        }

        public List<Question> Build(LevelDefinition level)
        {
            switch (level.Mode)
            {
                case QuizMode.FindProvince: return FindProvince(level);
                case QuizMode.Region: return Region(level);
                case QuizMode.Feature: return Feature(level);
                case QuizMode.Topic: return Topic(level);
                case QuizMode.Plate: return Plate(level);
                case QuizMode.Neighbor: return Neighbor(level);
                case QuizMode.Attribute: return Attribute(level);
                case QuizMode.FeatureChoice: return FeatureChoice(level);
                case QuizMode.RegionAll: return RegionAll(level);
                case QuizMode.Mcq: return Mcq(level);
                case QuizMode.BorderCountry: return BorderCountry(level);
                case QuizMode.Numbered: return Numbered(level);
                default: return new List<Question>();
            }
        }

        IEnumerable<ProvinceView> ProvincesIn(LevelDefinition level) =>
            map.Provinces.Where(p => level.IncludesRegion(p.Data.region));

        List<Question> FindProvince(LevelDefinition level) =>
            Take(ProvincesIn(level), level.questionCount).Select(p =>
            {
                var q = new Question { Prompt = "BU İLİ HARİTADA BUL", Title = Up(p.DisplayName), Type = AnswerType.Map,
                    Layer = MapLayer.Provinces, Reveal = p, AnswerLabel = p.DisplayName };
                q.Correct.Add(p);
                return q;
            }).ToList();

        List<Question> Region(LevelDefinition level) =>
            Take(ProvincesIn(level), level.questionCount).Select(p => new Question
            {
                Prompt = "BU İL HANGİ BÖLGEDE?", Title = Up(p.DisplayName), Type = AnswerType.Choice,
                Choices = Regions.All, ChoiceColors = Regions.All.Select(palette.RegionColor).ToArray(),
                CorrectChoice = System.Array.IndexOf(Regions.All, p.Data.region), Focus = p, AnswerLabel = p.Data.region
            }).ToList();

        /// <summary>Kıyı tipleri: her tip bir soru; o tipin bütün örnekleri (şerit ya da nokta) doğru cevaptır.</summary>
        List<Question> CoastTypes(LevelDefinition level)
        {
            var groups = features.Features.Where(f => f.Category == GeoFeature.CoastType).GroupBy(f => f.Data.subtype).ToList();
            return Take(groups, level.questionCount).Select(g =>
            {
                var first = g.First().Data;
                bool shape = first.subtype == "tombolo" || first.subtype == "lagun";
                var q = new Question
                {
                    Prompt = shape ? "BU KIYI ŞEKLİNİN BİR ÖRNEĞİNİ HARİTADA BUL" : "BU KIYI TİPİNİN BİR ÖRNEĞİNİ HARİTADA BUL",
                    Title = Up(string.IsNullOrEmpty(first.typeName) ? first.subtype : first.typeName),
                    Type = AnswerType.Map, Layer = MapLayer.Features, Info = first.typeInfo
                };
                q.Correct.AddRange(g);
                q.AnswerLabel = JoinNames(g.Select(f => f.DisplayName));
                return q;
            }).ToList();
        }

        List<Question> Feature(LevelDefinition level) => level.category == GeoFeature.CoastType ? CoastTypes(level) :
            Take(features.Features.Where(f => FeatureLayer.Matches(level.category, f.Category)), level.questionCount).Select(f =>
            {
                var q = new Question { Prompt = $"BU {GeoFeatureDatabase.CategoryNoun(level.category)} HARİTADA BUL", Title = Up(f.DisplayName),
                    Type = AnswerType.Map, Layer = MapLayer.Features, Reveal = f, Info = f.Data.info, AnswerLabel = f.DisplayName };
                q.Correct.Add(f);
                return q;
            }).ToList();

        List<Question> Topic(LevelDefinition level) =>
            Take(topics.InSet(level.set), level.questionCount).Select(t =>
            {
                var q = new Question { Prompt = t.question, Title = Up(t.title), Type = AnswerType.Map, Layer = MapLayer.Provinces,
                    Info = string.IsNullOrEmpty(t.source) ? t.info : $"{t.info}  (Kaynak: {t.source})" };
                foreach (int plate in t.plates)
                {
                    var p = map.GetByPlate(plate);
                    if (p != null) q.Correct.Add(p);
                }
                q.AnswerLabel = JoinNames(q.Correct.Select(c => c.DisplayName));
                return q;
            }).ToList();

        List<Question> Plate(LevelDefinition level)
        {
            int min = level.plateMin > 0 ? level.plateMin : 1, max = level.plateMax > 0 ? level.plateMax : 81;
            var pool = map.Provinces.Where(p => p.Data.plate >= min && p.Data.plate <= max);
            var list = new List<Question>();
            int i = 0;
            foreach (var p in Take(pool, level.questionCount))
            {
                string code = p.Data.plate.ToString("00");
                if (i++ % 2 == 0)
                {
                    var q = new Question { Prompt = "BU PLAKA HANGİ İLİN?", Title = code, Type = AnswerType.Map, Layer = MapLayer.Provinces,
                        Reveal = p, AnswerLabel = $"{code} {p.DisplayName}" };
                    q.Correct.Add(p);
                    list.Add(q);
                }
                else
                {
                    var options = new List<int> { p.Data.plate };
                    int guard = 0;
                    while (options.Count < 4 && guard++ < 100)
                    {
                        int d = Random.Range(1, 13) * (Random.value < 0.5f ? -1 : 1);
                        int v = Mathf.Clamp(p.Data.plate + d, 1, 81);
                        if (!options.Contains(v)) options.Add(v);
                    }
                    options = Shuffled(options);
                    list.Add(new Question
                    {
                        Prompt = "BU İLİN PLAKA KODU HANGİSİ?", Title = Up(p.DisplayName), Type = AnswerType.Choice,
                        Choices = options.Select(o => o.ToString("00")).ToArray(), CorrectChoice = options.IndexOf(p.Data.plate),
                        Focus = p, AnswerLabel = code
                    });
                }
            }
            return list;
        }

        List<Question> Neighbor(LevelDefinition level) =>
            Take(ProvincesIn(level).Where(p => p.Data.neighbors != null && p.Data.neighbors.Length > 0), level.questionCount).Select(p =>
            {
                // Sorulan il haritada parlatılmaz (konumu soruda verilmesin); doğru cevaptan sonra haritada belirir
                var q = new Question { Prompt = "BU İLE KOMŞU BİR İL SEÇ", Title = Up(p.DisplayName), Type = AnswerType.Map,
                    Layer = MapLayer.Provinces, Focus = p, FocusHidden = true, MarkOnCorrect = p };
                foreach (int n in p.Data.neighbors)
                {
                    var nv = map.GetByPlate(n);
                    if (nv != null) q.Correct.Add(nv);
                }
                q.AnswerLabel = JoinNames(q.Correct.Select(c => c.DisplayName), 9);
                return q;
            }).ToList();

        List<Question> Attribute(LevelDefinition level)
        {
            var attr = topics.Attribute(level.attribute);
            if (attr == null) return new List<Question>();
            var pairs = new List<(ProvinceView p, int choice)>();
            foreach (var g in attr.groups)
            foreach (int plate in g.plates)
            {
                var p = map.GetByPlate(plate);
                if (p != null) pairs.Add((p, g.choice));
            }
            var colors = attr.choices.Select((_, i) => attr.ChoiceColor(i)).ToArray();
            return Take(pairs, level.questionCount).Select(x => new Question
            {
                Prompt = attr.prompt, Title = Up(x.p.DisplayName), Type = AnswerType.Choice, Choices = attr.choices,
                ChoiceColors = colors, CorrectChoice = x.choice, Focus = x.p, AnswerLabel = attr.choices[x.choice],
                Info = attr.infos != null && x.choice < attr.infos.Length ? attr.infos[x.choice] : null
            }).ToList();
        }

        List<Question> FeatureChoice(LevelDefinition level)
        {
            var attr = topics.Attribute(level.attribute);
            if (attr == null) return new List<Question>();
            var colors = attr.choices.Select((_, i) => attr.ChoiceColor(i)).ToArray();
            var pool = features.Features.Where(f => f.Category == level.category && System.Array.IndexOf(attr.choices, f.Data.subtype) >= 0);
            return Take(pool, level.questionCount).Select(f => new Question
            {
                Prompt = attr.prompt, Title = Up(f.DisplayName), Type = AnswerType.Choice, Choices = attr.choices, ChoiceColors = colors,
                CorrectChoice = System.Array.IndexOf(attr.choices, f.Data.subtype), Focus = f, Info = f.Data.info, AnswerLabel = f.Data.subtype
            }).ToList();
        }

        // ================== Numaralı harita (ÖSYM tipi) ==================

        public static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI" };
        static readonly string[] Letters = { "A", "B", "C", "D", "E" };

        /// <summary>"plate:42" ya da "feature:kizilirmak" biçimindeki hedefi çözer.</summary>
        IMapTarget ResolveTarget(string spec)
        {
            if (string.IsNullOrEmpty(spec)) return null;
            if (spec.StartsWith("feature:")) return features.Find(spec.Substring(8));
            if (spec.StartsWith("plate:") && int.TryParse(spec.Substring(6), out int plate)) return map.GetByPlate(plate);
            return null;
        }

        /// <summary>Şekil ucu: "42" (il plakası, ilin merkezi) ya da "28.5,40.9" (boylam,enlem).</summary>
        Vector2? ResolvePoint(string spec)
        {
            spec = spec.Trim();
            if (spec.Contains(","))
            {
                var parts = spec.Split(',');
                if (parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float lon) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float lat))
                    return (Vector2)map.LonLatToMap(lon, lat);
                return null;
            }
            if (int.TryParse(spec, out int plate))
            {
                var p = map.GetByPlate(plate);
                if (p != null) return (Vector2)p.transform.position;
            }
            return null;
        }

        /// <summary>"arrow:52>34|Fındık" ya da "line:42>6" biçimindeki şekli çözer.</summary>
        MapShape ResolveShape(string spec)
        {
            bool arrow = spec.StartsWith("arrow:");
            if (!arrow && !spec.StartsWith("line:")) return null;
            string body = spec.Substring(arrow ? 6 : 5);
            string caption = null;
            int bar = body.IndexOf('|');
            if (bar >= 0) { caption = body.Substring(bar + 1); body = body.Substring(0, bar); }
            var ends = body.Split('>');
            if (ends.Length != 2) return null;
            var a = ResolvePoint(ends[0]);
            var b = ResolvePoint(ends[1]);
            if (a == null || b == null) return null;
            return new MapShape { from = a.Value, to = b.Value, arrow = arrow, caption = caption };
        }

        /// <summary>
        /// Elle yazılmış soruda haritadaki numaralı yerleri ve doğru numaraları kurar.
        /// İşaret biçimleri: "plate:42", "feature:kizilirmak", "arrow:52>34|Fındık", "line:42>6" (uçlar plaka ya da "boylam,enlem").
        /// </summary>
        void AddMarkers(Question q, McqQuestion m)
        {
            if (m.markers == null || m.markers.Length == 0) return;
            if (m.markers[0].StartsWith("arrow:") || m.markers[0].StartsWith("line:"))
            {
                foreach (var spec in m.markers)
                {
                    var s = ResolveShape(spec);
                    if (s == null) Debug.LogWarning($"[KPSS] '{m.id}' sorusunda çizilemeyen şekil: {spec}");
                    else q.Shapes.Add(s);
                }
                if (m.answerMarkers != null) q.CorrectShapes.AddRange(m.answerMarkers);
                return;
            }
            foreach (var spec in m.markers)
            {
                var t = ResolveTarget(spec);
                if (t == null) Debug.LogWarning($"[KPSS] '{m.id}' sorusunda bulunamayan işaret: {spec}");
                q.Markers.Add(t);
            }
            q.Markers.RemoveAll(t => t == null);
            if (m.answerMarkers != null)
                foreach (int i in m.answerMarkers)
                    if (i >= 0 && i < q.Markers.Count) q.Correct.Add(q.Markers[i]);
        }

        /// <summary>
        /// Numaralı harita bölümü. <c>set</c> virgülle ayrılmış konu setleri (tarim1, maden…) ve/veya soru bankası setleridir.
        /// Konu setlerinden otomatik soru üretilir: haritada 5 il I–V diye numaralanır, biri o konuda öne çıkan il,
        /// diğer dördü ise konunun illerinden, onların komşularından ve doğru ilin bölgesinden uzak illerdir (tartışmalı şık olmasın).
        /// Soru bankasındaki <c>markers</c>'lı sorular olduğu gibi eklenir ("I ve II" tipi şıklar).
        /// </summary>
        List<Question> Numbered(LevelDefinition level)
        {
            var sets = (level.set ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            var pool = new List<Question>();
            foreach (var m in mcq.questions.Where(m => System.Array.IndexOf(sets, m.set) >= 0 && m.markers != null && m.markers.Length > 0))
            {
                var q = new Question
                {
                    Prompt = string.IsNullOrEmpty(m.body) ? "HARİTADA NUMARALANDIRILAN YERLER" : "KPSS SORUSU",
                    Title = m.text, Body = m.body, LongTitle = true, Type = AnswerType.Choice,
                    Choices = m.options.Select((o, i) => $"{Letters[i]})  {o}").ToArray(), CorrectChoice = m.answer,
                    Info = m.explanation, AnswerLabel = $"{Letters[m.answer]}) {m.options[m.answer]}"
                };
                AddMarkers(q, m);
                if (q.Markers.Count > 0 || q.Shapes.Count > 0) pool.Add(q);
            }
            foreach (var t in topics.topics.Where(t => System.Array.IndexOf(sets, t.set) >= 0))
            {
                var q = NumberedFromTopic(t);
                if (q != null) pool.Add(q);
            }
            return Take(pool, level.questionCount);
        }

        Question NumberedFromTopic(TopicSet t)
        {
            var answers = t.plates.Select(map.GetByPlate).Where(p => p != null).ToList();
            if (answers.Count == 0) return null;
            var correct = answers[Random.Range(0, answers.Count)];

            // Tartışmalı olmasın: konunun illeri, onların komşuları ve doğru ilin bölgesi şık olamaz
            var banned = new HashSet<int>(t.plates);
            foreach (var a in answers)
                if (a.Data.neighbors != null) foreach (int n in a.Data.neighbors) banned.Add(n);
            var candidates = Shuffled(map.Provinces.Where(p => !banned.Contains(p.Data.plate) && p.Data.region != correct.Data.region));

            // Şıklar haritaya yayılsın (birbirine çok yakın işaret olmasın)
            var chosen = new List<ProvinceView> { correct };
            foreach (float minDist in new[] { map.Bounds.width * 0.16f, map.Bounds.width * 0.1f, 0f })
            {
                foreach (var c in candidates)
                {
                    if (chosen.Count >= 5) break;
                    if (chosen.Contains(c)) continue;
                    if (chosen.All(o => Vector2.Distance(o.transform.position, c.transform.position) >= minDist)) chosen.Add(c);
                }
                if (chosen.Count >= 5) break;
            }
            if (chosen.Count < 5) return null;

            var markers = Shuffled(chosen);
            int answer = markers.IndexOf(correct);
            var q = new Question
            {
                Prompt = "HARİTADA NUMARALANDIRILAN İLLER", Title = NumberedSentence(t), LongTitle = true, Type = AnswerType.Choice,
                Choices = Enumerable.Range(0, 5).Select(i => $"{Letters[i]})  {Roman[i]}").ToArray(), CorrectChoice = answer,
                Info = $"{Roman[answer]}: {correct.DisplayName}. {t.info}", AnswerLabel = $"{Letters[answer]}) {Roman[answer]} – {correct.DisplayName}"
            };
            q.Markers.AddRange(markers);
            q.Correct.Add(correct);
            return q;
        }

        /// <summary>Konu setinden ÖSYM üslubunda soru cümlesi.</summary>
        static string NumberedSentence(TopicSet t) => t.set switch
        {
            "tarim1" or "tarim2" => $"Numaralandırılan illerden hangisi {t.title} üretiminde ilk sıralarda yer alır?",
            "hayvan" => $"Numaralandırılan illerden hangisinde {t.title} yetiştiriciliği en yaygındır?",
            "maden" => $"Numaralandırılan illerden hangisinde {t.title} bakımından önemli yataklar bulunur?",
            "nufus" => $"Numaralandırılan illerden hangisi “{t.title}” arasında yer alır?",
            _ => $"Numaralandırılan illerden hangisi {t.title} bakımından öne çıkar?"
        };

        /// <summary>Sınır ilinin adı verilir, oyuncu komşu olduğu ülkeyi haritada seçer (birden fazlaysa herhangi biri).</summary>
        List<Question> BorderCountry(LevelDefinition level)
        {
            var countries = features.Features.Where(f => f.Category == GeoFeature.Country && f.Data.plates != null).ToList();
            var plates = countries.SelectMany(c => c.Data.plates).Distinct();
            var list = new List<Question>();
            foreach (int plate in Take(plates, level.questionCount))
            {
                var p = map.GetByPlate(plate);
                if (p == null) continue;
                var near = countries.Where(c => System.Array.IndexOf(c.Data.plates, plate) >= 0).ToList();
                string names = JoinNames(near.Select(c => c.DisplayName), 8);
                var q = new Question
                {
                    Prompt = near.Count > 1 ? "BU İLİN SINIR KOMŞUSU OLAN BİR ÜLKEYİ SEÇ" : "BU İLİN SINIR KOMŞUSU OLAN ÜLKEYİ SEÇ",
                    Title = Up(p.DisplayName), Type = AnswerType.Map, Layer = MapLayer.Features, MarkOnCorrect = p,
                    AnswerLabel = names, Info = $"{p.DisplayName}: {names} ile sınır komşusudur."
                };
                q.Correct.AddRange(near);
                list.Add(q);
            }
            return list;
        }

        /// <summary>
        /// Bölgeyi tamamla: bölgenin bütün illeri karışık sırayla tek tek sorulur (sınırı çizili bölge içinde).
        /// Bilinen il boyalı kalır; bölüm bitince bölge tamamen dolmuş olur.
        /// </summary>
        List<Question> RegionAll(LevelDefinition level)
        {
            string region = level.HasRegionFilter ? level.regions[0] : Regions.Marmara;
            return Shuffled(map.Provinces.Where(p => p.Data.region == region)).Select(p =>
            {
                var q = new Question { Prompt = $"{Up(region)}: BU İLİ HARİTADA BUL", Title = Up(p.DisplayName), Type = AnswerType.Map,
                    Layer = MapLayer.Provinces, Reveal = p, AnswerLabel = p.DisplayName };
                q.Correct.Add(p);
                return q;
            }).ToList();
        }

        List<Question> Mcq(LevelDefinition level)
        {
            string[] letters = { "A", "B", "C", "D", "E" };
            return Take(mcq.questions.Where(m => m.set == level.set), level.questionCount).Select(m =>
            {
                var q = new Question
                {
                    Prompt = "KPSS SORUSU", Title = m.text, LongTitle = true, Type = AnswerType.Choice,
                    Choices = m.options.Select((o, i) => $"{letters[i]})  {o}").ToArray(), CorrectChoice = m.answer,
                    Info = m.explanation, AnswerLabel = $"{letters[m.answer]}) {m.options[m.answer]}"
                };
                AddMarkers(q, m);
                if (!string.IsNullOrEmpty(m.highlight))
                {
                    if (m.highlight.StartsWith("feature:"))
                    {
                        var f = features.Find(m.highlight.Substring(8));
                        q.ShowFeature = f;
                        q.Focus = f;
                    }
                    else if (m.highlight.StartsWith("plate:") && int.TryParse(m.highlight.Substring(6), out int plate))
                    {
                        q.Focus = map.GetByPlate(plate);
                    }
                }
                return q;
            }).ToList();
        }
    }
}
