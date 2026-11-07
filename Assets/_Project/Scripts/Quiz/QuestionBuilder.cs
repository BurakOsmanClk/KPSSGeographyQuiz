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

        List<Question> Feature(LevelDefinition level) =>
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
                var q = new Question { Prompt = "BU İLE KOMŞU BİR İL SEÇ", Title = Up(p.DisplayName), Type = AnswerType.Map,
                    Layer = MapLayer.Provinces, Focus = p };
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

        List<Question> RegionAll(LevelDefinition level)
        {
            string region = level.HasRegionFilter ? level.regions[0] : Regions.Marmara;
            var q = new Question { Prompt = $"{Up(region)} BÖLGESİNİN BÜTÜN İLLERİNİ BUL", Title = Up(region), Type = AnswerType.Map,
                Layer = MapLayer.Provinces, RequireAll = true };
            q.Correct.AddRange(map.Provinces.Where(p => p.Data.region == region));
            q.AnswerLabel = region;
            return new List<Question> { q };
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
