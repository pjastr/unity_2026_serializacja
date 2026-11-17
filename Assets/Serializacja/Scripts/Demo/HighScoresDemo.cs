using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    [Serializable]
    public class ScoreEntry
    {
        public string nick;
        public int score;
        public string date;
    }

    /// <summary>Tabela rekordów zapisywana w JEDNYM kluczu PlayerPrefs jako tekst JSON.</summary>
    [Serializable]
    public class HighScoreTable
    {
        public int version = 2;
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    /// <summary>
    /// Scena 14: obiekt złożony w PlayerPrefs (JSON w kluczu typu string), wersjonowanie, migracja ze starego formatu.
    /// Spacja – zagraj rundę (losowy wynik), LPM – zapisz tabelę w STARYM formacie v1 (osobne klucze), PPM – wyczyść tabelę.
    /// </summary>
    public class HighScoresDemo : DemoActionBase
    {
        const string KTable = "s14.highscores";
        const int MaxEntries = 5;
        static readonly string[] Nicks = { "Ania", "Bartek", "Ola", "Kuba", "Zosia", "Piotr", "Ewa", "Marek" };

        public Transform[] bars;
        public WorldLabel[] barLabels;
        public float maxScore = 10000f;

        HighScoreTable m_Table;
        string m_Info = "";
        int m_LastScore = -1;

        void Start()
        {
            m_Table = LoadTable(out m_Info);
            Refresh();
        }

        /// <summary>Odczyt z obsługą starego formatu (v1: osobne klucze s14.v1.nick0, s14.v1.score0 …).</summary>
        static HighScoreTable LoadTable(out string info)
        {
            if (PlayerPrefs.HasKey("s14.v1.count"))
            {
                var t = new HighScoreTable();
                int n = PlayerPrefs.GetInt("s14.v1.count");
                for (int i = 0; i < n; i++)
                    t.entries.Add(new ScoreEntry
                    {
                        nick = PlayerPrefs.GetString("s14.v1.nick" + i, "?"),
                        score = PlayerPrefs.GetInt("s14.v1.score" + i, 0),
                        date = "(z wersji 1)",
                    });
                // migracja: zapis w nowym formacie i usunięcie starych kluczy
                for (int i = 0; i < n; i++) { PlayerPrefs.DeleteKey("s14.v1.nick" + i); PlayerPrefs.DeleteKey("s14.v1.score" + i); }
                PlayerPrefs.DeleteKey("s14.v1.count");
                PrefsUtil.SetObject(KTable, t);
                PlayerPrefs.Save();
                info = $"<color=#FFD966>Migracja v1 → v2</color>: {n} wpisów przeniesiono z osobnych kluczy do jednego klucza JSON.";
                return t;
            }
            var table = PrefsUtil.GetObject<HighScoreTable>(KTable);
            info = PlayerPrefs.HasKey(KTable) ? $"Wczytano tabelę (wersja {table.version}) z klucza \"{KTable}\"." : "Brak zapisanej tabeli – pusta tabela.";
            return table;
        }

        public override void OnMainAction()
        {
            int score = Mathf.RoundToInt(UnityEngine.Random.Range(500f, maxScore) / 10f) * 10;
            string nick = Nicks[UnityEngine.Random.Range(0, Nicks.Length)];
            m_LastScore = score;
            var e = new ScoreEntry { nick = nick, score = score, date = DateTime.Now.ToString("HH:mm:ss") };
            m_Table.entries.Add(e);
            m_Table.entries.Sort((a, b) => b.score.CompareTo(a.score));
            if (m_Table.entries.Count > MaxEntries) m_Table.entries.RemoveRange(MaxEntries, m_Table.entries.Count - MaxEntries);
            bool inTop = m_Table.entries.Contains(e);
            PrefsUtil.SetObject(KTable, m_Table);   // SetString(klucz, JsonUtility.ToJson(tabela))
            PlayerPrefs.Save();
            m_Info = inTop ? $"{nick}: {score} pkt – nowy wpis w tabeli, zapisano." : $"{nick}: {score} pkt – za mało na top {MaxEntries}.";
            DemoHud.Log(m_Info);
            Refresh();
        }

        public override void OnSecondaryAction()
        {
            // symulacja starej wersji gry, która trzymała rekordy w osobnych kluczach
            PlayerPrefs.DeleteKey(KTable);
            string[] nicks = { "Stary1", "Stary2", "Stary3" };
            int[] scores = { 4200, 3100, 1500 };
            for (int i = 0; i < 3; i++)
            {
                PlayerPrefs.SetString("s14.v1.nick" + i, nicks[i]);
                PlayerPrefs.SetInt("s14.v1.score" + i, scores[i]);
            }
            PlayerPrefs.SetInt("s14.v1.count", 3);
            PlayerPrefs.Save();
            m_Table = LoadTable(out m_Info);
            DemoHud.Log("Zapisano dane w formacie v1 i wczytano z migracją");
            Refresh();
        }

        public override void OnTertiaryAction()
        {
            PlayerPrefs.DeleteKey(KTable);
            PlayerPrefs.Save();
            m_Table = new HighScoreTable();
            m_Info = "PlayerPrefs.DeleteKey(\"" + KTable + "\")";
            m_LastScore = -1;
            DemoHud.Log(m_Info);
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < bars.Length; i++)
            {
                bool has = i < m_Table.entries.Count;
                float h = has ? Mathf.Max(0.05f, m_Table.entries[i].score / maxScore * 4f) : 0.05f;
                bars[i].localScale = new Vector3(1f, h, 1f);
                bars[i].localPosition = new Vector3(bars[i].localPosition.x, h / 2f, 0f);
                if (barLabels[i] != null)
                {
                    barLabels[i].header = has ? $"{i + 1}. {m_Table.entries[i].nick}" : $"{i + 1}. –";
                    barLabels[i].extra = has ? m_Table.entries[i].score + " pkt" : "";
                    barLabels[i].worldOffset = new Vector3(0f, h / 2f + 0.3f, 0f);
                }
            }
            string raw = PlayerPrefs.GetString(KTable, "");
            var sb = new StringBuilder();
            sb.Append("<b>Tabela rekordów</b> (top ").Append(MaxEntries).Append(")\n");
            for (int i = 0; i < m_Table.entries.Count; i++)
            {
                var e = m_Table.entries[i];
                bool last = e.score == m_LastScore;
                sb.Append(last ? "<color=#7CFC7C>" : "").Append("   ").Append(i + 1).Append(". ").Append(e.nick).Append(" – ")
                  .Append(e.score).Append(" pkt  (").Append(e.date).Append(')').Append(last ? "</color>" : "").Append('\n');
            }
            if (m_Table.entries.Count == 0) sb.Append("   (pusta)\n");
            sb.Append('\n').Append(m_Info).Append("\n\n");
            sb.Append("<b>PlayerPrefs.GetString(\"").Append(KTable).Append("\")</b> – ")
              .Append(System.Text.Encoding.UTF8.GetByteCount(raw)).Append(" B\n");
            sb.Append(string.IsNullOrEmpty(raw) ? "(brak klucza)" : raw.Replace("},{", "},\n{"));
            sb.Append("\n\nJeden klucz = jeden obiekt JSON z polem version.\nTo nadal tylko lokalny tekst: gracz może go zmienić (np. w rejestrze).");
            DemoHud.SetStatus("hs", sb.ToString());
        }
    }
}
