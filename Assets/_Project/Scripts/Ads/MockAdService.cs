using System;
using KPSSGeography.Core;
using KPSSGeography.UI;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.Ads
{
    /// <summary>
    /// Gerçek reklam SDK'sı kurulana kadar kullanılan test reklamı. Tam ekran bir kart gösterir:
    /// ödüllü reklamda geri sayım bitince "Ödülü al", geçiş reklamında birkaç saniye sonra "Kapat" çıkar.
    /// Akışın (devam teklifi, sıklık sınırı) uçtan uca denenmesini sağlar.
    /// </summary>
    public class MockAdService : UIScreen, IAdService
    {
        const float RewardedSeconds = 5f;
        const float InterstitialSeconds = 3f;

        SfxPlayer sfx;
        Text titleText;
        Text bodyText;
        Text timerText;
        RectTransform progressFill;
        Button claimButton;
        Button closeButton;
        RectTransform card;

        bool rewarded;
        float duration;
        float elapsed;
        bool running;
        Action<bool> onRewardDone;
        Action onInterstitialDone;

        public bool IsRewardedReady => true;
        public bool IsInterstitialReady => true;

        public void Initialize(Transform parent, SfxPlayer sfxPlayer)
        {
            sfx = sfxPlayer;
            CreateRoot(parent, "MockAd");
            UIFactory.Solid("Dim", Root, Theme.Hex(0x02080F, 0.92f), true).rectTransform.Stretch();

            var c = UIFactory.Card("Card", Root, Theme.NavyPanel, true);
            card = c.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 560));

            var tag = UIFactory.Panel("Tag", card, Theme.Amber);
            tag.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28, -28), new Vector2(150, 40));
            UIFactory.Label("TagText", tag.transform, "REKLAM", 22, Theme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Stretch();

            titleText = UIFactory.Label("Title", card, "", 52, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            titleText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(820, 64));
            bodyText = UIFactory.Label("Body", card, "", 26, Theme.TextMuted, TextAnchor.MiddleCenter);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(760, 90));

            var bar = UIFactory.Panel("Bar", card, new Color(1f, 1f, 1f, 0.12f));
            bar.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 180), new Vector2(700, 16));
            var fill = UIFactory.Panel("Fill", bar.transform, Theme.Amber);
            progressFill = fill.rectTransform;
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
            timerText = UIFactory.Label("Timer", card, "", 26, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            timerText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 204), new Vector2(700, 36));

            claimButton = UIFactory.Button("Claim", card, "ÖDÜLÜ AL", Theme.Success, new Vector2(340, 88), Claim, null, 32);
            claimButton.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(340, 88));
            closeButton = UIFactory.Button("Close", card, "KAPAT", Theme.NavyLight, new Vector2(240, 72), Close, null, 26);
            closeButton.GetComponent<RectTransform>().Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -24), new Vector2(200, 64));
        }

        public void ShowRewarded(string placement, Action<bool> onComplete)
        {
            Begin(true, RewardedSeconds, "+1 CAN İÇİN REKLAM",
                "Test reklamı. Ads Mediation (LevelPlay) kurulunca burada gerçek ödüllü reklam gösterilecek.\nKapatırsan ödül verilmez.");
            onRewardDone = onComplete;
            Debug.Log($"[KPSS] (Test) Ödüllü reklam: {placement}");
        }

        public void ShowInterstitial(string placement, Action onClosed)
        {
            Begin(false, InterstitialSeconds, "REKLAM ARASI",
                "Test reklamı. Ads Mediation (LevelPlay) kurulunca burada gerçek geçiş reklamı gösterilecek.");
            onInterstitialDone = onClosed;
            Debug.Log($"[KPSS] (Test) Geçiş reklamı: {placement}");
        }

        void Begin(bool isRewarded, float seconds, string title, string body)
        {
            rewarded = isRewarded;
            duration = seconds;
            elapsed = 0f;
            running = true;
            titleText.text = title;
            bodyText.text = body;
            claimButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(isRewarded); // ödüllüde erken kapatmak mümkün (ödülsüz)
            Show();
            PopIn("card", card, 0.9f, 0.3f);
        }

        void Update()
        {
            if (!running) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            progressFill.anchorMax = new Vector2(t, 1f);
            if (t < 1f)
            {
                timerText.text = $"{Mathf.CeilToInt(duration - elapsed)} sn";
                return;
            }
            running = false;
            timerText.text = rewarded ? "Ödül hazır!" : "";
            if (rewarded)
            {
                claimButton.gameObject.SetActive(true);
                PopIn("claim", claimButton.transform, 0.7f, 0.3f);
            }
            else
            {
                closeButton.gameObject.SetActive(true);
            }
        }

        void Claim()
        {
            sfx.Play(Sfx.Star);
            Finish(true);
        }

        void Close()
        {
            sfx.Play(Sfx.Click);
            Finish(false);
        }

        void Finish(bool granted)
        {
            running = false;
            Hide();
            if (rewarded)
            {
                var cb = onRewardDone;
                onRewardDone = null;
                cb?.Invoke(granted);
            }
            else
            {
                var cb = onInterstitialDone;
                onInterstitialDone = null;
                cb?.Invoke();
            }
        }
    }
}
