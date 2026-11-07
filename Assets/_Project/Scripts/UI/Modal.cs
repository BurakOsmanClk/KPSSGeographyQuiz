using System;
using KPSSGeography.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>Karartılmış arka planlı, ortada kart olan basit açılır pencere.</summary>
    public class Modal : UIScreen
    {
        public RectTransform Content { get; private set; }
        RectTransform card;

        public static Modal Create(Transform parent, string name, string title, Vector2 size)
        {
            var go = new GameObject(name + "Controller");
            var m = go.AddComponent<Modal>();
            m.Build(parent, name, title, size);
            return m;
        }

        void Build(Transform parent, string name, string title, Vector2 size)
        {
            CreateRoot(parent, name);
            var dim = UIFactory.Solid("Dim", Root, Theme.Overlay, true);
            dim.rectTransform.Stretch();
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Hide);
            dim.GetComponent<Button>().transition = Selectable.Transition.None;

            var c = UIFactory.Card("Card", Root, Theme.NavyPanel, true);
            card = c.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

            UIFactory.Label("Title", card, title, 48, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(size.x - 80, 60));

            var bar = UIFactory.Panel("Accent", card, Theme.Amber);
            bar.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -104), new Vector2(90, 6));


            Content = UIFactory.Rect("Content", card).Stretch(48, 48, 130, 40);
        }

        public override void Show()
        {
            base.Show();
            PopIn("card", card, 0.88f, 0.3f);
        }
    }
}
