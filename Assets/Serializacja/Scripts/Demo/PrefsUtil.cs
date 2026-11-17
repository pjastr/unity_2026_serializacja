using System;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Pomocnicze metody dla PlayerPrefs (sceny 13–14). PlayerPrefs przechowuje tylko int, float i string –
    /// pozostałe typy zamieniamy na te trzy: bool → int, enum → int, Color → tekst "#RRGGBB", obiekt → JSON.
    /// </summary>
    public static class PrefsUtil
    {
        public static bool GetBool(string key, bool def) => PlayerPrefs.GetInt(key, def ? 1 : 0) != 0;
        public static void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);

        public static T GetEnum<T>(string key, T def) where T : struct
        {
            int v = PlayerPrefs.GetInt(key, Convert.ToInt32(def));
            return Enum.IsDefined(typeof(T), v) ? (T)Enum.ToObject(typeof(T), v) : def;
        }

        public static void SetEnum<T>(string key, T value) where T : struct
            => PlayerPrefs.SetInt(key, Convert.ToInt32(value));

        public static Color GetColor(string key, Color def)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(PlayerPrefs.GetString(key, ""), out c) ? c : def;
        }

        public static void SetColor(string key, Color value)
            => PlayerPrefs.SetString(key, "#" + ColorUtility.ToHtmlStringRGB(value));

        /// <summary>Obiekt złożony jako tekst JSON w jednym kluczu.</summary>
        public static T GetObject<T>(string key) where T : class, new()
        {
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return new T();
            try { return JsonUtility.FromJson<T>(json) ?? new T(); }
            catch (Exception) { return new T(); }      // uszkodzone dane – wartości domyślne
        }

        public static void SetObject<T>(string key, T value) => PlayerPrefs.SetString(key, JsonUtility.ToJson(value));

        /// <summary>Gdzie Unity trzyma PlayerPrefs na bieżącej platformie.</summary>
        public static string StorageLocation()
        {
            string c = Application.companyName, p = Application.productName;
#if UNITY_EDITOR_WIN
            return $"Rejestr: HKEY_CURRENT_USER\\Software\\Unity\\UnityEditor\\{c}\\{p}\n(w edytorze nazwy kluczy mają dopisany hash, np. s13.volume_h123456)";
#elif UNITY_EDITOR_OSX
            return $"~/Library/Preferences/com.{c}.{p}.plist";
#elif UNITY_STANDALONE_WIN
            return $"Rejestr: HKEY_CURRENT_USER\\Software\\{c}\\{p}";
#elif UNITY_STANDALONE_OSX
            return $"~/Library/Preferences/{Application.identifier}.plist";
#elif UNITY_STANDALONE_LINUX
            return $"~/.config/unity3d/{c}/{p}";
#elif UNITY_ANDROID
            return "SharedPreferences: /data/data/<pakiet>/shared_prefs/<pakiet>.v2.playerprefs.xml";
#elif UNITY_IOS
            return "NSUserDefaults";
#elif UNITY_WEBGL
            return "IndexedDB przeglądarki (limit 1 MB)";
#else
            return "zależnie od platformy (patrz dokumentacja PlayerPrefs)";
#endif
        }
    }
}
