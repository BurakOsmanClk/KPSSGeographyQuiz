using System;
using KPSSGeography.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KPSSGeography.Map
{
    /// <summary>Yer şekli bölümlerinde fare ile üzerine gelme ve sol tık ile seçme.</summary>
    public class FeaturePointer : MonoBehaviour
    {
        public event Action<FeatureView> FeatureClicked;
        public Func<bool> CanInteract;

        /// <summary>Üzerine gelince vurgulanmayacak hedefler (ör. bu turda zaten doğru bilinenler).</summary>
        public Func<FeatureView, bool> HoverBlocked;

        FeatureLayer layer;
        Camera cam;
        FeatureView hovered;

        public void Initialize(FeatureLayer featureLayer, Camera camera)
        {
            layer = featureLayer;
            cam = camera;
        }

        void Update()
        {
            if (layer == null || cam == null) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            FeatureView hit = null;
            if ((CanInteract == null || CanInteract()) && !PointerUtil.IsOverUI())
            {
                Vector2 screen = mouse.position.ReadValue();
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                Vector2 local = layer.transform.InverseTransformPoint(world);
                float unitsPerPixel = 2f * cam.orthographicSize / Mathf.Max(1, Screen.height);
                hit = layer.FindAt(local, unitsPerPixel);
            }

            FeatureView hoverTarget = hit != null && HoverBlocked != null && HoverBlocked(hit) ? null : hit;
            if (hoverTarget != hovered)
            {
                if (hovered != null) hovered.SetHovered(false);
                hovered = hoverTarget;
                if (hovered != null) hovered.SetHovered(true);
            }

            if (hit != null && mouse.leftButton.wasPressedThisFrame)
                FeatureClicked?.Invoke(hit);
        }
    }
}
