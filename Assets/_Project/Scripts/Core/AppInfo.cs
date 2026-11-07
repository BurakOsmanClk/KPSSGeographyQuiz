using UnityEngine;

namespace KPSSGeography.Core
{
    /// <summary>
    /// Uygulama sürüm bilgisi. Sürüm numarası Unity'deki Player Settings > Version (bundleVersion) alanından okunur;
    /// yeni sürümde yalnızca orayı güncellemek ve docs/CHANGELOG.md'ye not eklemek yeterlidir.
    /// </summary>
    public static class AppInfo
    {
        /// <summary>Yayın aşaması: "Prototip" → "Beta" → "" (yayın).</summary>
        public const string Stage = "Prototip";

        public static string Version => Application.version;

        /// <summary>Kısa etiket: "v0.3.0 • Prototip".</summary>
        public static string ShortLabel => string.IsNullOrEmpty(Stage) ? $"v{Version}" : $"v{Version} • {Stage}";

        /// <summary>Ayrıntılı etiket: "Sürüm 0.3.0 (Prototip) • Unity 6000.6.0f1 • Android".</summary>
        public static string FullLabel =>
            $"Sürüm {Version}{(string.IsNullOrEmpty(Stage) ? "" : $" ({Stage})")}  •  Unity {Application.unityVersion}  •  {PlatformName}";

        static string PlatformName => Application.platform switch
        {
            RuntimePlatform.Android => "Android",
            RuntimePlatform.IPhonePlayer => "iOS",
            RuntimePlatform.WindowsEditor or RuntimePlatform.OSXEditor or RuntimePlatform.LinuxEditor => "Editör",
            RuntimePlatform.WebGLPlayer => "Web",
            _ => Application.platform.ToString()
        };
    }
}
