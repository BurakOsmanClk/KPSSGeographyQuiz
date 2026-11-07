using System;
using KPSSGeography.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KPSSGeography.Map
{
    /// <summary>Fare ile il üzerine gelme (hover) ve sol tık ile seçme.</summary>
    public class MapPointer : MonoBehaviour
    {
        public event Action<ProvinceView> ProvinceClicked;

        /// <summary>Etkileşime izin verilip verilmediğini söyleyen koşul (ör. sadece soru aktifken).</summary>
        public Func<bool> CanInteract;

        TurkeyMap map;
        Camera cam;
        ProvinceView hovered;

        public ProvinceView Hovered => hovered;

        public void Initialize(TurkeyMap turkeyMap, Camera camera)
        {
            map = turkeyMap;
            cam = camera;
        }

        void Update()
        {
            if (map == null || cam == null) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            bool allowed = CanInteract == null || CanInteract();
            ProvinceView hit = null;

            if (allowed && !PointerUtil.IsOverUI())
            {
                Vector2 screen = mouse.position.ReadValue();
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                Vector2 local = map.transform.InverseTransformPoint(world);
                hit = map.FindAt(local);
            }

            if (hit != hovered)
            {
                if (hovered != null) hovered.SetHovered(false);
                hovered = hit;
                if (hovered != null) hovered.SetHovered(true);
            }

            if (hit != null && mouse.leftButton.wasPressedThisFrame)
                ProvinceClicked?.Invoke(hit);
        }

        public void ClearHover()
        {
            if (hovered != null) hovered.SetHovered(false);
            hovered = null;
        }
    }
}
