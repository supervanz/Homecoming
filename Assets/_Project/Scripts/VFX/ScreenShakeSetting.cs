using UnityEngine;

namespace Homecoming.Vfx
{
    /// <summary>화면 흔들림 옵션 (13-5, 멀미 고려). PlayerPrefs에 저장한다.</summary>
    public static class ScreenShakeSetting
    {
        private const string Key = "Homecoming.ScreenShake";

        public static bool IsEnabled(bool defaultValue) => PlayerPrefs.GetInt(Key, defaultValue ? 1 : 0) == 1;

        public static void Set(bool enabled)
        {
            PlayerPrefs.SetInt(Key, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
