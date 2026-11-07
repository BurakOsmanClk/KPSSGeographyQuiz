using System.Collections;
using System.Collections.Generic;
using KPSSGeography.Core;
using UnityEngine;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Tüm ekranların temeli: tam ekran bir kök, CanvasGroup ile yumuşak açılıp kapanma
    /// ve anahtar bazlı (aynı anahtardaki eski animasyonu durduran) tween yardımcıları.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        readonly Dictionary<string, Coroutine> running = new Dictionary<string, Coroutine>();
        readonly Dictionary<RectTransform, Vector2> restPositions = new Dictionary<RectTransform, Vector2>();

        protected RectTransform Root { get; private set; }
        protected CanvasGroup Group { get; private set; }
        public bool IsVisible { get; private set; }

        protected void CreateRoot(Transform parent, string name)
        {
            Root = UIFactory.Rect(name, parent).Stretch();
            transform.SetParent(Root, false);
            Group = Root.gameObject.AddComponent<CanvasGroup>();
            SetVisibleImmediate(false);
        }

        public virtual void Show()
        {
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
            IsVisible = true;
            Group.blocksRaycasts = true;
            Group.interactable = true;
            Animate("visibility", Tween.Run(0.25f, t => Group.alpha = Tween.EaseOutCubic(t)));
        }

        public virtual void Hide()
        {
            if (!IsVisible && !Root.gameObject.activeSelf) return;
            IsVisible = false;
            Group.blocksRaycasts = false;
            Group.interactable = false;
            float start = Group.alpha;
            Animate("visibility", Tween.Run(0.2f, t =>
            {
                Group.alpha = Mathf.Lerp(start, 0f, t);
                if (t >= 1f) Root.gameObject.SetActive(false);
            }));
        }

        protected void SetVisibleImmediate(bool visible)
        {
            StopAnim("visibility");
            IsVisible = visible;
            Group.alpha = visible ? 1f : 0f;
            Group.blocksRaycasts = visible;
            Group.interactable = visible;
            Root.gameObject.SetActive(visible);
        }

        // ---------- Tween yardımcıları ----------

        protected void Animate(string key, IEnumerator routine)
        {
            StopAnim(key);
            if (!isActiveAndEnabled) return;
            running[key] = StartCoroutine(routine);
        }

        protected void StopAnim(string key)
        {
            if (running.TryGetValue(key, out var c) && c != null) StopCoroutine(c);
            running.Remove(key);
        }

        protected void PunchScale(string key, Transform t, float amount, float duration)
        {
            Animate(key, Tween.Run(duration, x =>
            {
                float s = 1f + amount * Tween.Punch(x);
                t.localScale = new Vector3(s, s, 1f);
            }));
        }

        protected void PopIn(string key, Transform t, float from = 0.85f, float duration = 0.35f, float delay = 0f)
        {
            t.localScale = new Vector3(from, from, 1f);
            Animate(key, Delayed(delay, Tween.Run(duration, x =>
            {
                float s = Mathf.LerpUnclamped(from, 1f, Tween.EaseOutBack(x));
                t.localScale = new Vector3(s, s, 1f);
            })));
        }

        /// <summary>Elemanı aşağıdan kaydırarak ve belirginleştirerek getirir.</summary>
        protected void SlideIn(string key, RectTransform rt, CanvasGroup cg, float delay, float distance = 40f)
        {
            if (!restPositions.TryGetValue(rt, out Vector2 end))
            {
                end = rt.anchoredPosition;
                restPositions[rt] = end;
            }
            Vector2 start = end - new Vector2(0f, distance);
            rt.anchoredPosition = start;
            cg.alpha = 0f;
            Animate(key, Delayed(delay, Tween.Run(0.45f, x =>
            {
                float e = Tween.EaseOutCubic(x);
                rt.anchoredPosition = Vector2.LerpUnclamped(start, end, e);
                cg.alpha = e;
            })));
        }

        protected static IEnumerator Delayed(float delay, IEnumerator then)
        {
            if (delay > 0f)
            {
                float t = 0f;
                while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }
            }
            yield return then;
        }

        protected static CanvasGroup EnsureGroup(Component c)
        {
            var g = c.GetComponent<CanvasGroup>();
            return g != null ? g : c.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
