using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>Haritada sorulabilen her şey (il ya da yer şekli). Oyun akışı bu arayüz üzerinden çalışır.</summary>
    public interface IMapTarget
    {
        string DisplayName { get; }
        /// <summary>İsim etiketinin ve uçan puanın gösterileceği dünya konumu.</summary>
        Vector3 LabelWorldPosition { get; }
        void SetState(ProvinceState state);
        void SetFocused(bool value);
        void PlayCorrect();
        void PlayWrongClick();
        void PlayReveal(float duration);
    }
}
