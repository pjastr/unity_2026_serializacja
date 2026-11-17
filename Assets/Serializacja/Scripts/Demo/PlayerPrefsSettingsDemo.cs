using System;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Scena 13: PlayerPrefs – ustawienia gracza.
    /// A / D – wybór ustawienia, W / S – zmiana wartości, Spacja – PlayerPrefs.Save(),
    /// LPM – wczytaj ponownie (odrzuca niezapisane zmiany w obiektach sceny), PPM – usuń klucze tej sceny (DeleteKey).
    /// Przytrzymaj E (reset sceny) lub uruchom grę ponownie – wartości zostają.
    /// </summary>
    public class PlayerPrefsSettingsDemo : DemoActionBase
    {
        // stałe nazwy kluczy – literówka w tekście klucza = cicha utrata danych
        const string KVolume = "s13.volume";
        const string KBrightness = "s13.brightness";
        const string KColor = "s13.color";
        const string KDifficulty = "s13.difficulty";
        const string KHints = "s13.showHints";
        const string KName = "s13.playerName";
        const string KLaunches = "s13.launchCount";
        const string KLastVisit = "s13.lastVisit";

        static readonly string[] Names = { "Głośność", "Jasność", "Kolor postaci", "Trudność", "Podpowiedzi", "Nazwa gracza" };
        static readonly Color[] Colors =
        {
            new Color(0.25f, 0.45f, 0.9f), new Color(0.86f, 0.27f, 0.23f), new Color(0.3f, 0.75f, 0.35f),
            new Color(0.95f, 0.82f, 0.25f), new Color(0.6f, 0.35f, 0.85f),
        };
        static readonly string[] Nicks = { "Gracz", "Ania", "Bartek", "Ola", "Kuba" };

        [Header("Obiekty sceny (ustawiane przez generator)")]
        public Renderer character;
        public Light sceneLight;
        public Transform speaker;
        public GameObject[] hints;
        public GameObject[] enemies;
        public WorldLabel nameLabel;

        // bieżące wartości w pamięci
        float m_Volume, m_Brightness;
        Color m_Color;
        Difficulty m_Difficulty;
        bool m_Hints;
        string m_Name;
        int m_Launches;
        string m_LastVisit;

        int m_Selected;
        bool m_AxisHeld;
        bool m_Dirty;
        MaterialPropertyBlock m_Block;

        void Start()
        {
            m_Block = new MaterialPropertyBlock();
            Load();
            // licznik wejść do sceny – zapisany od razu, widać, że PlayerPrefs przetrwa przeładowanie sceny
            m_Launches++;
            PlayerPrefs.SetInt(KLaunches, m_Launches);
            m_LastVisit = PlayerPrefs.GetString(KLastVisit, "–");
            PlayerPrefs.SetString(KLastVisit, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            PlayerPrefs.Save();
            Apply();
        }

        void Load()
        {
            m_Volume = PlayerPrefs.GetFloat(KVolume, 0.8f);                // drugi argument = wartość domyślna
            m_Brightness = PlayerPrefs.GetFloat(KBrightness, 1f);
            m_Color = PrefsUtil.GetColor(KColor, Colors[0]);
            m_Difficulty = PrefsUtil.GetEnum(KDifficulty, Difficulty.Normal);
            m_Hints = PrefsUtil.GetBool(KHints, true);
            m_Name = PlayerPrefs.GetString(KName, "Gracz");
            m_Launches = PlayerPrefs.GetInt(KLaunches, 0);
            m_Dirty = false;
        }

        void Store()
        {
            PlayerPrefs.SetFloat(KVolume, m_Volume);
            PlayerPrefs.SetFloat(KBrightness, m_Brightness);
            PrefsUtil.SetColor(KColor, m_Color);
            PrefsUtil.SetEnum(KDifficulty, m_Difficulty);
            PrefsUtil.SetBool(KHints, m_Hints);
            PlayerPrefs.SetString(KName, m_Name);
        }

        public override void OnMainAction()
        {
            Store();
            PlayerPrefs.Save();          // zapis na dysk teraz (domyślnie Unity zapisuje przy zamykaniu aplikacji)
            m_Dirty = false;
            DemoHud.Log("PlayerPrefs.Set… + PlayerPrefs.Save()");
        }

        public override void OnSecondaryAction()
        {
            Load();
            Apply();
            DemoHud.Log("Wczytano wartości z PlayerPrefs (Get… z wartościami domyślnymi)");
        }

        public override void OnTertiaryAction()
        {
            foreach (var k in new[] { KVolume, KBrightness, KColor, KDifficulty, KHints, KName, KLaunches, KLastVisit })
                PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
            Load();
            Apply();
            DemoHud.Log("PlayerPrefs.DeleteKey(…) dla kluczy sceny 13 – wartości domyślne");
        }

        void Update()
        {
            Vector2 mv = DemoInput.ReadVector2(DemoInput.Move);
            bool any = Mathf.Abs(mv.x) > 0.5f || Mathf.Abs(mv.y) > 0.5f;
            if (any && !m_AxisHeld)
            {
                m_AxisHeld = true;
                if (Mathf.Abs(mv.x) > 0.5f) m_Selected = (m_Selected + (mv.x > 0 ? 1 : Names.Length - 1)) % Names.Length;
                else Change(mv.y > 0 ? 1 : -1);
            }
            if (!any) m_AxisHeld = false;
            UpdateStatus();
        }

        void Change(int dir)
        {
            switch (m_Selected)
            {
                case 0: m_Volume = Mathf.Clamp01(Mathf.Round((m_Volume + 0.1f * dir) * 10f) / 10f); break;
                case 1: m_Brightness = Mathf.Clamp(Mathf.Round((m_Brightness + 0.2f * dir) * 10f) / 10f, 0.2f, 2f); break;
                case 2:
                {
                    int i = Array.FindIndex(Colors, c => ColorUtility.ToHtmlStringRGB(c) == ColorUtility.ToHtmlStringRGB(m_Color));
                    m_Color = Colors[((i < 0 ? 0 : i) + dir + Colors.Length) % Colors.Length];
                    break;
                }
                case 3: m_Difficulty = (Difficulty)(((int)m_Difficulty + dir + 3) % 3); break;
                case 4: m_Hints = !m_Hints; break;
                default:
                {
                    int i = Array.IndexOf(Nicks, m_Name);
                    m_Name = Nicks[((i < 0 ? 0 : i) + dir + Nicks.Length) % Nicks.Length];
                    break;
                }
            }
            m_Dirty = true;
            Apply();
        }

        void Apply()
        {
            if (character != null)
            {
                character.GetPropertyBlock(m_Block);
                m_Block.SetColor("_BaseColor", m_Color);
                m_Block.SetColor("_Color", m_Color);
                character.SetPropertyBlock(m_Block);
            }
            if (sceneLight != null) sceneLight.intensity = 1.2f * m_Brightness;
            if (speaker != null) speaker.localScale = new Vector3(0.6f + m_Volume, 0.3f + m_Volume, 0.6f + m_Volume);
            foreach (var h in hints) if (h != null) h.SetActive(m_Hints);
            int count = m_Difficulty == Difficulty.Easy ? 1 : m_Difficulty == Difficulty.Normal ? 3 : 5;
            for (int i = 0; i < enemies.Length; i++) if (enemies[i] != null) enemies[i].SetActive(i < count);
            if (nameLabel != null) nameLabel.header = m_Name;
        }

        void UpdateStatus()
        {
            var sb = new StringBuilder();
            sb.Append("<b>Ustawienia</b> (A / D – wybór, W / S – zmiana)").Append(m_Dirty ? "  <color=#FFD966>niezapisane – Spacja</color>" : "").Append('\n');
            string[] values =
            {
                $"{m_Volume:0.0}  → SetFloat(\"{KVolume}\")",
                $"{m_Brightness:0.0}  → SetFloat(\"{KBrightness}\")",
                $"#{ColorUtility.ToHtmlStringRGB(m_Color)}  → SetString(\"{KColor}\")",
                $"{m_Difficulty}  → SetInt(\"{KDifficulty}\", {(int)m_Difficulty})",
                $"{m_Hints}  → SetInt(\"{KHints}\", {(m_Hints ? 1 : 0)})",
                $"{m_Name}  → SetString(\"{KName}\")",
            };
            for (int i = 0; i < Names.Length; i++)
                sb.Append(i == m_Selected ? "<color=#7CFC7C>▶ " : "   ").Append(Names[i]).Append(": ").Append(values[i])
                  .Append(i == m_Selected ? "</color>" : "").Append('\n');

            sb.Append("\n<b>Zapisane w PlayerPrefs</b> (HasKey → Get…)\n");
            Row(sb, KVolume, PlayerPrefs.HasKey(KVolume) ? PlayerPrefs.GetFloat(KVolume).ToString("0.0") : null);
            Row(sb, KBrightness, PlayerPrefs.HasKey(KBrightness) ? PlayerPrefs.GetFloat(KBrightness).ToString("0.0") : null);
            Row(sb, KColor, PlayerPrefs.HasKey(KColor) ? PlayerPrefs.GetString(KColor) : null);
            Row(sb, KDifficulty, PlayerPrefs.HasKey(KDifficulty) ? PlayerPrefs.GetInt(KDifficulty).ToString() : null);
            Row(sb, KHints, PlayerPrefs.HasKey(KHints) ? PlayerPrefs.GetInt(KHints).ToString() : null);
            Row(sb, KName, PlayerPrefs.HasKey(KName) ? PlayerPrefs.GetString(KName) : null);
            Row(sb, KLaunches, PlayerPrefs.HasKey(KLaunches) ? PlayerPrefs.GetInt(KLaunches).ToString() : null);
            Row(sb, KLastVisit, PlayerPrefs.HasKey(KLastVisit) ? PlayerPrefs.GetString(KLastVisit) : null);
            sb.Append("\nWejść do tej sceny: <b>").Append(m_Launches).Append("</b>, poprzednia wizyta: ").Append(m_LastVisit).Append('\n');
            sb.Append("\n<b>Miejsce zapisu</b>\n").Append(PrefsUtil.StorageLocation().Replace("<", "‹").Replace(">", "›"));
            DemoHud.SetStatus("prefs", sb.ToString());
        }

        static void Row(StringBuilder sb, string key, string value)
        {
            sb.Append("   ").Append(key).Append(" = ")
              .Append(value == null ? "<color=#FF7B7B>brak klucza</color>" : value).Append('\n');
        }
    }
}
