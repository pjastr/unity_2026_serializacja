using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    /// <summary>Dane profilu gracza w aktualnej wersji formatu (v2).</summary>
    [Serializable]
    public class ProfileData
    {
        public string name = "Gracz";
        public int level = 1;
        public int gold;
        public float mana = 50f;                // dodane w wersji 2
        public Vector3 position;                // dodane w wersji 2
    }

    /// <summary>
    /// Scena 11: własny format binarny (BinaryWriter / BinaryReader) i wersjonowanie zapisu.
    /// Spacja – zapisz w STAREJ wersji 1 (symulacja starej gry), LPM – zapisz w wersji 2, PPM – wczytaj (z migracją v1 → v2).
    /// </summary>
    public class BinarySaveDemo : DemoActionBase
    {
        const uint Magic = 0x45564153;   // "SAVE" (little-endian)
        const int CurrentVersion = 2;

        public string fileName = "profil_scena11.sav";
        public Transform player;

        ProfileData m_Data = new ProfileData { name = "Ania", level = 12, gold = 345, mana = 80f };
        string m_Info = "";

        string SavePath => Path.Combine(Application.persistentDataPath, fileName);

        void Start()
        {
            if (player != null) m_Data.position = player.position;
            Show();
        }

        void Update()
        {
            if (player != null)
            {
                player.Rotate(0f, 40f * Time.deltaTime, 0f);
                m_Data.position = player.position;
            }
        }

        public override void OnMainAction()
        {
            using (var w = new BinaryWriter(File.Open(SavePath, FileMode.Create)))
            {
                w.Write(Magic);
                w.Write(1);              // wersja 1: tylko name, level, gold
                w.Write(m_Data.name);
                w.Write(m_Data.level);
                w.Write(m_Data.gold);
            }
            m_Info = "Zapisano plik w wersji 1 (stara gra – bez mana i position).";
            DemoHud.Log("BinaryWriter: zapis v1");
            Show();
        }

        public override void OnSecondaryAction()
        {
            using (var w = new BinaryWriter(File.Open(SavePath, FileMode.Create)))
            {
                w.Write(Magic);
                w.Write(CurrentVersion);
                w.Write(m_Data.name);
                w.Write(m_Data.level);
                w.Write(m_Data.gold);
                w.Write(m_Data.mana);
                w.Write(m_Data.position.x);
                w.Write(m_Data.position.y);
                w.Write(m_Data.position.z);
            }
            m_Info = "Zapisano plik w wersji 2.";
            DemoHud.Log("BinaryWriter: zapis v2");
            Show();
        }

        public override void OnTertiaryAction()
        {
            if (!File.Exists(SavePath)) { m_Info = "Brak pliku."; Show(); return; }
            try
            {
                using (var r = new BinaryReader(File.OpenRead(SavePath)))
                {
                    if (r.ReadUInt32() != Magic) throw new InvalidDataException("to nie jest plik zapisu");
                    int version = r.ReadInt32();
                    if (version > CurrentVersion) throw new InvalidDataException("plik z nowszej wersji gry: " + version);
                    var d = new ProfileData();
                    d.name = r.ReadString();
                    d.level = r.ReadInt32();
                    d.gold = r.ReadInt32();
                    if (version >= 2)
                    {
                        d.mana = r.ReadSingle();
                        d.position = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        m_Info = "Wczytano wersję 2 – bez migracji.";
                    }
                    else
                    {
                        // migracja v1 → v2: brakujące pola dostają wartości domyślne
                        d.mana = 50f;
                        d.position = Vector3.zero;
                        m_Info = "Wczytano wersję 1 → <b>migracja</b>: mana = 50, position = (0,0,0).";
                    }
                    m_Info += $"\nWynik: {d.name}, level {d.level}, gold {d.gold}, mana {d.mana:0}, pos {d.position}";
                }
                DemoHud.Log("BinaryReader: odczyt");
            }
            catch (Exception e)
            {
                m_Info = "<color=#FF7B7B>Błąd: " + e.Message + "</color>";
            }
            Show();
        }

        void Show()
        {
            var sb = new StringBuilder();
            sb.Append("<b>Dane</b>: ").Append(m_Data.name).Append(", level ").Append(m_Data.level).Append(", gold ").Append(m_Data.gold)
              .Append(", mana ").Append(m_Data.mana.ToString("0")).Append('\n');
            string json = JsonUtility.ToJson(m_Data);
            sb.Append("rozmiar JSON (bez wcięć): <b>").Append(Encoding.UTF8.GetByteCount(json)).Append(" B</b>\n");
            sb.Append("rozmiar binarny v2: <b>").Append(4 + 4 + 1 + Encoding.UTF8.GetByteCount(m_Data.name) + 4 + 4 + 4 + 12).Append(" B</b>\n\n");
            sb.Append("<b>Plik</b>: ").Append(fileName);
            if (File.Exists(SavePath))
            {
                byte[] bytes = File.ReadAllBytes(SavePath);
                sb.Append(" (").Append(bytes.Length).Append(" B)\n");
                sb.Append(HexDump(bytes));
            }
            else sb.Append(" – nie istnieje\n");
            if (!string.IsNullOrEmpty(m_Info))
                sb.Append('\n').Append(m_Info).Append('\n');
            sb.Append("\nFormat: [magic 4 B][wersja 4 B][name: długość + UTF-8][level 4 B][gold 4 B]\n         v2: [mana 4 B][x y z 3×4 B]");
            DemoHud.SetStatus("bin", sb.ToString());
        }

        static string HexDump(byte[] bytes)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length && i < 64; i += 16)
            {
                sb.Append(i.ToString("X4")).Append(":  ");
                var ascii = new StringBuilder();
                for (int j = i; j < i + 16 && j < bytes.Length; j++)
                {
                    sb.Append(bytes[j].ToString("X2")).Append(' ');
                    char c = (char)bytes[j];
                    ascii.Append(c >= 32 && c < 127 ? c : '.');
                }
                sb.Append("  ").Append(ascii.ToString().Replace("<", "‹")).Append('\n');
            }
            return sb.ToString();
        }
    }
}
