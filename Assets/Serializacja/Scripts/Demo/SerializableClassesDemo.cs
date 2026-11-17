using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    [Serializable]
    public class Item
    {
        public string name = "Przedmiot";
        public int value = 1;
    }

    /// <summary>Klasa pochodna – przy serializacji "po wartości" jej dodatkowe pola giną.</summary>
    [Serializable]
    public class Sword : Item
    {
        public float sharpness = 0.9f;
    }

    [Serializable]
    public struct Stats
    {
        public int hp;
        public int armor;
    }

    [Serializable]
    public class Inventory
    {
        public List<Item> items = new List<Item>();
        public Item equipped = new Item();
        public Stats stats;
    }

    /// <summary>Bez [Serializable] – pole tego typu nie trafi do pliku.</summary>
    public class NotSerializableData
    {
        public int secret = 123;
    }

    /// <summary>
    /// Scena 02: własne klasy i struktury z [Serializable].
    /// Serializacja Unity zapisuje je PO WARTOŚCI (inline): bez polimorfizmu, bez null, bez współdzielenia.
    /// Spacja – kolejny przypadek, LPM – podróż w obie strony (ToJson → FromJson).
    /// </summary>
    public class SerializableClassesDemo : DemoActionBase
    {
        public Inventory inventory = new Inventory();
        public NotSerializableData ignored = new NotSerializableData();

        [Header("Wizualizacja (ustawiane przez generator)")]
        public Transform originalRow;
        public Transform copyRow;
        public Material itemMaterial;
        public Material swordMaterial;

        [Serializable]
        class Snapshot
        {
            public Inventory inventory;
            public NotSerializableData ignored;
        }

        int m_Case;
        Inventory m_Copy;
        string m_Json = "";

        static readonly string[] CaseNames =
        {
            "1. Lista obiektów klasy [Serializable]",
            "2. Polimorfizm – Sword : Item w List<Item>",
            "3. Wartość null w polu klasy",
            "4. Struktura (struct) i klasa bez [Serializable]",
        };

        void Start()
        {
            SetupCase();
        }

        public override void OnMainAction()
        {
            m_Case = (m_Case + 1) % CaseNames.Length;
            SetupCase();
        }

        public override void OnSecondaryAction()
        {
            RoundTrip();
        }

        void SetupCase()
        {
            inventory = new Inventory();
            inventory.items.Add(new Item { name = "Jabłko", value = 2 });
            inventory.items.Add(new Item { name = "Tarcza", value = 30 });
            inventory.equipped = inventory.items[1];
            inventory.stats = new Stats { hp = 100, armor = 5 };

            switch (m_Case)
            {
                case 1:
                    inventory.items.Add(new Sword { name = "Ostrze", value = 50, sharpness = 0.95f });
                    break;
                case 2:
                    inventory.equipped = null;
                    break;
                case 3:
                    ignored.secret = 999;
                    break;
            }
            m_Copy = null;
            DemoHud.Log("Przypadek: " + CaseNames[m_Case]);
            RoundTrip();
        }

        void RoundTrip()
        {
            // te same reguły co dla komponentu: pole typu bez [Serializable] jest pomijane
            m_Json = JsonUtility.ToJson(new Snapshot { inventory = inventory, ignored = ignored }, true);
            var copy = JsonUtility.FromJson<Snapshot>(m_Json);
            m_Copy = copy.inventory;
            int ignoredAfter = copy.ignored != null ? copy.ignored.secret : -1;

            DrawRow(originalRow, inventory);
            DrawRow(copyRow, m_Copy);

            var sb = new StringBuilder();
            sb.Append("<b>").Append(CaseNames[m_Case]).Append("</b>\n\n");
            sb.Append("<b>Oryginał</b>\n").Append(Describe(inventory));
            sb.Append("\n<b>Kopia po ToJson → FromJson</b>\n").Append(Describe(m_Copy));
            if (m_Case == 3)
                sb.Append("ignored.secret: oryginał = ").Append(ignored.secret).Append(", kopia = ").Append(ignoredAfter)
                  .Append("  (-1 = null: pole nie trafiło do JSON)\n");
            sb.Append("\n<b>JSON</b>\n").Append(Shorten(m_Json, 30));
            DemoHud.SetStatus("classes", sb.ToString());
        }

        static string Describe(Inventory inv)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < inv.items.Count; i++)
            {
                var it = inv.items[i];
                sb.Append("items[").Append(i).Append("]: <color=#9fd3ff>").Append(it.GetType().Name).Append("</color> ")
                  .Append(it.name).Append(", value = ").Append(it.value);
                var s = it as Sword;
                if (s != null) sb.Append(", sharpness = ").Append(s.sharpness.ToString("0.00"));
                sb.Append('\n');
            }
            sb.Append("equipped: ").Append(inv.equipped == null ? "<color=#FF7B7B>null</color>" : "\"" + inv.equipped.name + "\"");
            if (inv.equipped != null && inv.items.Count > 1)
                sb.Append(ReferenceEquals(inv.equipped, inv.items[1]) ? "  (to ten sam obiekt co items[1])" : "  (osobna kopia, nie items[1])");
            sb.Append('\n');
            sb.Append("stats: hp = ").Append(inv.stats.hp).Append(", armor = ").Append(inv.stats.armor).Append('\n');
            return sb.ToString();
        }

        static string Shorten(string text, int maxLines)
        {
            var lines = text.Split('\n');
            if (lines.Length <= maxLines) return text;
            return string.Join("\n", lines, 0, maxLines) + "\n…";
        }

        void DrawRow(Transform row, Inventory inv)
        {
            if (row == null) return;
            for (int i = row.childCount - 1; i >= 0; i--)
                Destroy(row.GetChild(i).gameObject);
            for (int i = 0; i < inv.items.Count; i++)
            {
                var it = inv.items[i];
                bool sword = it is Sword;
                var go = GameObject.CreatePrimitive(sword ? PrimitiveType.Capsule : PrimitiveType.Cube);
                go.name = it.name;
                go.transform.SetParent(row, false);
                go.transform.localPosition = new Vector3(i * 2.2f, 0.6f, 0);
                go.transform.localScale = sword ? new Vector3(0.5f, 0.6f, 0.5f) : Vector3.one * 1.1f;
                go.GetComponent<Renderer>().sharedMaterial = sword ? swordMaterial : itemMaterial;
                var label = go.AddComponent<WorldLabel>();
                label.header = it.GetType().Name;
                label.extra = it.name + (sword ? "\nsharpness " + ((Sword)it).sharpness.ToString("0.00") : "");
                label.worldOffset = new Vector3(0, 1.1f, 0);
            }
        }
    }
}
