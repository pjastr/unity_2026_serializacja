using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    /// <summary>"Koperta" zapisu: dane + wersja + suma kontrolna.</summary>
    [Serializable]
    public class SaveEnvelope
    {
        public int version = 1;
        public string savedAt;
        public string payload;     // JSON z danymi gry
        public string hash;        // SHA-256(payload + sól)
    }

    [Serializable]
    public class SlotData
    {
        public int gold;
        public int level = 1;
        public float playTime;
    }

    /// <summary>
    /// Scena 12: sloty zapisu, zapis atomowy (plik tymczasowy + podmiana), kopia zapasowa .bak i suma kontrolna.
    /// A / D – zmiana slotu, Spacja – zapisz, LPM – wczytaj, PPM – "oszukaj" (zmień złoto w pliku bez aktualizacji sumy).
    /// </summary>
    public class SecureSaveDemo : DemoActionBase
    {
        const string Salt = "pgwsu-2026";   // utrudnia ręczne przeliczenie sumy (to NIE jest szyfrowanie)

        public Transform[] slotMarkers;
        public Material activeMaterial;
        public Material idleMaterial;

        int m_Slot;
        SlotData m_Data = new SlotData();
        string m_Info = "";
        bool m_AxisHeld;

        static string SlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"slot{slot + 1}_scena12.json");

        void Update()
        {
            m_Data.playTime += Time.deltaTime;
            m_Data.gold = 100 + Mathf.FloorToInt(m_Data.playTime) * 5;
            m_Data.level = 1 + Mathf.FloorToInt(m_Data.playTime / 20f);

            float x = DemoInput.ReadVector2(DemoInput.Move).x;
            if (Mathf.Abs(x) > 0.5f && !m_AxisHeld)
            {
                m_Slot = (m_Slot + (x > 0 ? 1 : 2)) % 3;
                m_AxisHeld = true;
                DemoHud.Log("Wybrano slot " + (m_Slot + 1));
            }
            if (Mathf.Abs(x) < 0.2f) m_AxisHeld = false;

            if (Time.frameCount % 10 != 0) return;   // odczyt plików co 10 klatek
            for (int i = 0; slotMarkers != null && i < slotMarkers.Length; i++)
            {
                if (slotMarkers[i] == null) continue;
                slotMarkers[i].GetComponent<Renderer>().sharedMaterial = i == m_Slot ? activeMaterial : idleMaterial;
                var l = slotMarkers[i].GetComponent<WorldLabel>();
                if (l != null) l.extra = SlotSummary(i);
            }
            Show();
        }

        static string Hash(string payload)
        {
            using (var sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(payload + Salt));
                var sb = new StringBuilder();
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public override void OnMainAction()
        {
            string payload = JsonUtility.ToJson(m_Data);
            var env = new SaveEnvelope { savedAt = DateTime.Now.ToString("HH:mm:ss"), payload = payload, hash = Hash(payload) };
            string path = SlotPath(m_Slot);
            string tmp = path + ".tmp";
            string bak = path + ".bak";

            // 1) zapis do pliku tymczasowego – awaria w trakcie nie psuje poprzedniego zapisu
            File.WriteAllText(tmp, JsonUtility.ToJson(env, true), Encoding.UTF8);
            // 2) podmiana: stary plik → .bak, nowy plik na jego miejsce
            if (File.Exists(path))
                File.Replace(tmp, path, bak);
            else
                File.Move(tmp, path);
            m_Info = $"Zapisano slot {m_Slot + 1}: .tmp → podmiana, poprzednia wersja w .bak";
            DemoHud.Log(m_Info);
        }

        public override void OnSecondaryAction()
        {
            string path = SlotPath(m_Slot);
            SlotData d;
            string why;
            if (TryLoad(path, out d, out why))
            {
                m_Data = d;
                m_Info = $"Wczytano slot {m_Slot + 1} – suma kontrolna poprawna.";
            }
            else if (TryLoad(path + ".bak", out d, out string whyBak))
            {
                m_Data = d;
                m_Info = $"<color=#FFD966>Slot {m_Slot + 1}: {why}</color>\nWczytano kopię zapasową .bak.";
            }
            else
            {
                m_Info = $"<color=#FF7B7B>Slot {m_Slot + 1}: {why}; kopia .bak: {whyBak}</color>";
            }
            DemoHud.Log("Odczyt slotu " + (m_Slot + 1));
        }

        public override void OnTertiaryAction()
        {
            string path = SlotPath(m_Slot);
            if (!File.Exists(path)) { m_Info = "Brak pliku do zmiany – najpierw zapisz."; return; }
            var env = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
            var d = JsonUtility.FromJson<SlotData>(env.payload);
            d.gold = 999999;                                   // "edycja" pliku w notatniku
            env.payload = JsonUtility.ToJson(d);               // ...bez przeliczenia sumy kontrolnej
            File.WriteAllText(path, JsonUtility.ToJson(env, true));
            m_Info = $"Zmieniono złoto w pliku slotu {m_Slot + 1} na 999999 (hash bez zmian). Wczytaj – LPM.";
            DemoHud.Log("Ręczna modyfikacja pliku zapisu");
        }

        static bool TryLoad(string path, out SlotData data, out string reason)
        {
            data = null;
            if (!File.Exists(path)) { reason = "brak pliku"; return false; }
            try
            {
                var env = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
                if (env == null || string.IsNullOrEmpty(env.payload)) { reason = "uszkodzony plik"; return false; }
                if (Hash(env.payload) != env.hash) { reason = "niezgodna suma kontrolna (plik zmieniony)"; return false; }
                data = JsonUtility.FromJson<SlotData>(env.payload);
                reason = "";
                return true;
            }
            catch (Exception e)
            {
                reason = "błąd: " + e.Message;
                return false;
            }
        }

        static string SlotSummary(int slot)
        {
            string p = SlotPath(slot);
            if (!File.Exists(p)) return "pusty";
            var info = new FileInfo(p);
            return $"{info.Length} B, {info.LastWriteTime:HH:mm:ss}" + (File.Exists(p + ".bak") ? "\n+ .bak" : "");
        }

        void Show()
        {
            var sb = new StringBuilder();
            sb.Append("<b>Bieżący stan</b>: gold ").Append(m_Data.gold).Append(", level ").Append(m_Data.level)
              .Append(", czas ").Append(m_Data.playTime.ToString("0")).Append(" s\n");
            sb.Append("<b>Aktywny slot</b>: ").Append(m_Slot + 1).Append("  (A / D – zmiana)\n");
            string path = SlotPath(m_Slot);
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path);
                sb.Append("\n<b>").Append(Path.GetFileName(path)).Append("</b>\n").Append(text).Append('\n');
            }
            if (!string.IsNullOrEmpty(m_Info)) sb.Append('\n').Append(m_Info);
            DemoHud.SetStatus("secure", sb.ToString());
        }
    }
}
