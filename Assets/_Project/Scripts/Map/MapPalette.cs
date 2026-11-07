using System;
using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>Harita renkleri. GameBootstrap üzerinden Inspector'da değiştirilebilir.</summary>
    [Serializable]
    public class MapPalette
    {
        public Color sea = new Color32(0x8E, 0xC1, 0xDD, 0xFF);
        public Color land = new Color32(0xF3, 0xEE, 0xDF, 0xFF);
        public Color border = new Color32(0x8C, 0x84, 0x70, 0xFF);
        public Color hover = new Color32(0xFF, 0xE3, 0x94, 0xFF);
        public Color correct = new Color32(0x86, 0xCC, 0x86, 0xFF);
        public Color missed = new Color32(0xEC, 0x8F, 0x82, 0xFF);
        public Color correctFlash = new Color32(0x3F, 0xB9, 0x52, 0xFF);
        public Color wrongFlash = new Color32(0xD9, 0x3B, 0x2F, 0xFF);
        public Color reveal = new Color32(0xFF, 0xB8, 0x1F, 0xFF);
        public Color shadow = new Color(0f, 0f, 0f, 0.22f);
    }
}
