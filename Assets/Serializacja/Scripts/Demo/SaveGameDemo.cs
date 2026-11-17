using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Scena 09: zapis i odczyt stanu gry do pliku JSON (JsonUtility + System.IO).
    /// WASD – ruch gracza (zbieraj monety), Spacja – zapisz, LPM – wczytaj, PPM – usuń plik zapisu.
    /// </summary>
    public class SaveGameDemo : DemoActionBase
    {
        public Transform player;
        public Coin[] coins;
        public float speed = 5f;
        public string fileName = "zapis_scena09.json";

        int m_Score;
        float m_PlayTime;
        string m_LastJson = "";

        string SavePath => Path.Combine(Application.persistentDataPath, fileName);

        void Update()
        {
            m_PlayTime += Time.deltaTime;
            Vector2 mv = DemoInput.ReadVector2(DemoInput.Move);
            Vector3 dir = new Vector3(mv.x, 0f, mv.y);
            if (dir.sqrMagnitude > 0.01f)
            {
                player.position += dir * speed * Time.deltaTime;
                player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
            }
            foreach (var c in coins)
            {
                if (c == null || !c.gameObject.activeSelf) continue;
                Vector3 d = c.transform.position - player.position;
                d.y = 0f;
                if (d.magnitude < 1f)
                {
                    c.gameObject.SetActive(false);
                    m_Score += c.value;
                    DemoHud.Log($"Zebrano {c.id} (+{c.value})");
                }
            }
            UpdateStatus();
        }

        public override void OnMainAction()
        {
            var data = new SaveData
            {
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                playerPosition = player.position,
                playerYaw = player.eulerAngles.y,
                score = m_Score,
                playTime = m_PlayTime,
            };
            foreach (var c in coins)
                if (c != null && !c.gameObject.activeSelf) data.collectedCoins.Add(c.id);

            m_LastJson = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, m_LastJson, Encoding.UTF8);
            DemoHud.Log("Zapisano: File.WriteAllText(" + fileName + ")");
        }

        public override void OnSecondaryAction()
        {
            if (!File.Exists(SavePath))
            {
                DemoHud.Log("Brak pliku zapisu – najpierw Spacja");
                return;
            }
            try
            {
                m_LastJson = File.ReadAllText(SavePath, Encoding.UTF8);
                var data = JsonUtility.FromJson<SaveData>(m_LastJson);
                if (data == null) throw new Exception("pusty plik");
                player.position = data.playerPosition;
                player.rotation = Quaternion.Euler(0f, data.playerYaw, 0f);
                m_Score = data.score;
                m_PlayTime = data.playTime;
                foreach (var c in coins)
                    if (c != null) c.gameObject.SetActive(!data.collectedCoins.Contains(c.id));
                DemoHud.Log($"Wczytano zapis z {data.savedAt} (wersja {data.version})");
            }
            catch (Exception e)
            {
                DemoHud.Log("Błąd odczytu: " + e.Message);
            }
        }

        public override void OnTertiaryAction()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                DemoHud.Log("Usunięto plik zapisu");
            }
            m_LastJson = "";
        }

        void UpdateStatus()
        {
            var sb = new StringBuilder();
            sb.Append("<b>Stan gry</b>: wynik ").Append(m_Score).Append(", czas ").Append(m_PlayTime.ToString("0.0")).Append(" s\n");
            sb.Append("<b>Plik</b>: Application.persistentDataPath/").Append(fileName).Append('\n');
            sb.Append("<color=#9fb4cc>").Append(Application.persistentDataPath).Append("</color>\n");
            sb.Append("istnieje: ").Append(File.Exists(SavePath) ? "TAK" : "NIE").Append('\n');
            if (!string.IsNullOrEmpty(m_LastJson))
                sb.Append("\n<b>Ostatnio zapisany / wczytany JSON</b>\n").Append(m_LastJson);
            DemoHud.SetStatus("save", sb.ToString());
        }
    }
}
