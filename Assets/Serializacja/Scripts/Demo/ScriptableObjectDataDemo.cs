using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Scena 05: ScriptableObject jako wspólne dane.
    /// Spacja – zmień zasób "Goblin" (wszyscy goblini przyspieszają), LPM – kopia w pamięci (Instantiate) dla jednego goblina,
    /// PPM – przywróć wartości. Przy wyjściu ze sceny wartości są przywracane, bo w edytorze zmiany w zasobie
    /// wykonane w trybie Play ZOSTAJĄ zapisane w pliku .asset (w buildzie – nie).
    /// </summary>
    public class ScriptableObjectDataDemo : DemoActionBase
    {
        public EnemyType[] types;
        public Enemy[] enemies;
        public Enemy specialEnemy;

        readonly Dictionary<EnemyType, string> m_Backup = new Dictionary<EnemyType, string>();
        EnemyType m_RuntimeCopy;
        EnemyType m_OriginalOfSpecial;

        void Start()
        {
            foreach (var t in types)
                if (t != null) m_Backup[t] = JsonUtility.ToJson(t); // kopia zapasowa wartości
            if (specialEnemy != null) m_OriginalOfSpecial = specialEnemy.type;
            UpdateStatus();
        }

        public override void OnMainAction()
        {
            var goblin = types[0];
            goblin.speed = Mathf.Min(goblin.speed * 1.5f, 10f);
            goblin.size = Mathf.Min(goblin.size + 0.15f, 3f);
            DemoHud.Log($"types[0].speed = {goblin.speed:0.00} – zmiana zasobu dotyczy wszystkich, którzy go używają");
            UpdateStatus();
        }

        public override void OnSecondaryAction()
        {
            if (specialEnemy == null) return;
            if (m_RuntimeCopy == null)
            {
                m_RuntimeCopy = Instantiate(m_OriginalOfSpecial); // kopia w pamięci – nie jest plikiem
                m_RuntimeCopy.name = m_OriginalOfSpecial.name + " (kopia)";
                specialEnemy.type = m_RuntimeCopy;
            }
            m_RuntimeCopy.speed = Mathf.Min(m_RuntimeCopy.speed * 2f, 10f);
            m_RuntimeCopy.color = new Color(1f, 0.85f, 0.2f);
            DemoHud.Log("Instantiate(EnemyType) – zmieniamy tylko kopię jednego przeciwnika");
            UpdateStatus();
        }

        public override void OnTertiaryAction()
        {
            Restore();
            if (specialEnemy != null) specialEnemy.type = m_OriginalOfSpecial;
            if (m_RuntimeCopy != null) Destroy(m_RuntimeCopy);
            m_RuntimeCopy = null;
            DemoHud.Log("Przywrócono wartości z kopii zapasowej (JsonUtility.FromJsonOverwrite)");
            UpdateStatus();
        }

        void Restore()
        {
            foreach (var kv in m_Backup)
                if (kv.Key != null) JsonUtility.FromJsonOverwrite(kv.Value, kv.Key);
        }

        void OnDestroy()
        {
            // bez tego zmiany wykonane w trybie Play zostałyby w plikach .asset (tylko w edytorze)
            Restore();
        }

        void UpdateStatus()
        {
            var sb = new StringBuilder("<b>Zasoby EnemyType (pliki .asset)</b>\n");
            foreach (var t in types)
            {
                int users = 0;
                foreach (var e in enemies) if (e != null && e.type == t) users++;
                sb.Append("   ").Append(t.name).Append(" – ").Append(t.Describe()).Append("  ← używa ").Append(users).Append(" obiektów\n");
            }
            if (m_RuntimeCopy != null)
                sb.Append("   ").Append(m_RuntimeCopy.name).Append(" – ").Append(m_RuntimeCopy.Describe()).Append("  ← używa 1 obiekt (tylko w pamięci)\n");
            sb.Append("\nKażdy obiekt Enemy przechowuje tylko <b>referencję</b> do zasobu\n(w pliku sceny: fileID + guid zasobu).");
            DemoHud.SetStatus("so", sb.ToString());
        }
    }
}
