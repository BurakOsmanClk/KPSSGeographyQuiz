using UnityEngine.EventSystems;

namespace KPSSGeography.Core
{
    public static class PointerUtil
    {
        /// <summary>Fare imleci tıklanabilir bir UI elemanının üzerinde mi?</summary>
        public static bool IsOverUI()
        {
            var es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }
    }
}
