using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    public enum Difficulty { Easy, Normal, Hard }

    /// <summary>Klasa BEZ atrybutu [Serializable] – Unity jej nie zapisze.</summary>
    public class PlainClass
    {
        public int value = 5;
    }

    /// <summary>Klasa z atrybutem [Serializable] – zapisywana "w miejscu" (po wartości) wewnątrz komponentu.</summary>
    [Serializable]
    public class SerializableClass
    {
        public string name = "Miecz";
        public int damage = 12;
    }

    public interface IShape
    {
        float Area();
    }

    /// <summary>
    /// Scena 01: które pola zapisuje serializator Unity?
    /// Ten sam zestaw reguł obowiązuje w Inspectorze, przy zapisie sceny/prefabu, przy Instantiate i w JsonUtility.
    /// Spacja – tabela / JSON, LPM – zmień wartości wszystkich pól, PPM – odtwórz stan z JSON (FromJsonOverwrite).
    /// </summary>
    public class SerializationRulesDemo : DemoActionBase
    {
        // ---------- pola, które Unity serializuje ----------
        public int publicInt = 10;
        [SerializeField] private float privateSerialized = 2.5f;
        [HideInInspector] public int hiddenButSerialized = 4;
        [field: SerializeField] public int AutoProperty { get; private set; } = 11;
        public Difficulty difficulty = Difficulty.Normal;
        public long bigNumber = 1234567890123L;
        public Vector3 position = new Vector3(1, 2, 3);
        public Color color = Color.cyan;
        public int[] array = { 1, 2, 3 };
        public List<string> list = new List<string> { "a", "b" };
        public SerializableClass serializable = new SerializableClass();
        public Transform reference;

        // ---------- pola, których Unity NIE serializuje ----------
        private int privateNotSerialized = 7;
        [NonSerialized] public int publicNonSerialized = 3;
        public static int staticField = 99;
        public const int ConstValue = 42;
        public readonly int readonlyField = 8;
        public int Property { get; set; } = 6;
        public int[,] grid = new int[2, 2];
        public List<List<int>> nestedList = new List<List<int>> { new List<int> { 1 } };
        public Dictionary<string, int> dictionary = new Dictionary<string, int> { { "zloto", 100 } };
        public PlainClass plain = new PlainClass();
        public IShape shape;

        string m_Snapshot;
        bool m_ShowJson;
        int m_Changes;

        void Start()
        {
            m_Snapshot = JsonUtility.ToJson(this, true);
            DemoHud.Log("Zapisano stan początkowy: JsonUtility.ToJson(this)");
            Refresh();
        }

        public override void OnMainAction()
        {
            m_ShowJson = !m_ShowJson;
            Refresh();
        }

        public override void OnSecondaryAction()
        {
            m_Changes++;
            publicInt += 1;
            privateSerialized += 1f;
            hiddenButSerialized += 1;
            AutoProperty += 1;
            difficulty = (Difficulty)(((int)difficulty + 1) % 3);
            bigNumber += 1;
            position += Vector3.one;
            color = Color.HSVToRGB((m_Changes * 0.17f) % 1f, 0.7f, 0.9f);
            array = new[] { array[0] + 1, array[1] + 1, array[2] + 1 };
            list.Add("x" + m_Changes);
            serializable.damage += 1;
            privateNotSerialized += 1;
            publicNonSerialized += 1;
            staticField += 1;
            Property += 1;
            grid[0, 0] += 1;
            nestedList[0][0] += 1;
            dictionary["zloto"] += 1;
            plain.value += 1;
            DemoHud.Log("Zmieniono wartości wszystkich pól (+1)");
            Refresh();
        }

        public override void OnTertiaryAction()
        {
            // FromJsonOverwrite nadpisuje TYLKO pola obecne w JSON – czyli tylko pola serializowane
            JsonUtility.FromJsonOverwrite(m_Snapshot, this);
            DemoHud.Log("JsonUtility.FromJsonOverwrite(stan początkowy, this)");
            Refresh();
        }

        void Refresh()
        {
            string json = JsonUtility.ToJson(this, true);
            if (m_ShowJson)
            {
                DemoHud.SetStatus("rules", "<b>JsonUtility.ToJson(this, true)</b> – dokładnie to, co widzi serializator:\n" + json);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<b>Pole</b>  –  <b>wartość</b>  –  <b>czy jest w JSON?</b>\n");
            Row(sb, json, "publicInt", "public int", publicInt.ToString());
            Row(sb, json, "privateSerialized", "[SerializeField] private float", privateSerialized.ToString("0.0"));
            Row(sb, json, "hiddenButSerialized", "[HideInInspector] public", hiddenButSerialized.ToString());
            Row(sb, json, "<AutoProperty>k__BackingField", "[field: SerializeField] właściwość", AutoProperty.ToString());
            Row(sb, json, "difficulty", "enum", difficulty.ToString());
            Row(sb, json, "bigNumber", "long", bigNumber.ToString());
            Row(sb, json, "position", "Vector3", position.ToString());
            Row(sb, json, "color", "Color", "#" + ColorUtility.ToHtmlStringRGB(color));
            Row(sb, json, "array", "int[]", string.Join(",", array));
            Row(sb, json, "list", "List<string>", string.Join(",", list));
            Row(sb, json, "serializable", "klasa [Serializable]", serializable.name + " " + serializable.damage);
            Row(sb, json, "reference", "Transform (referencja)", reference != null ? reference.name : "null");
            sb.Append('\n');
            Row(sb, json, "privateNotSerialized", "private int", privateNotSerialized.ToString());
            Row(sb, json, "publicNonSerialized", "[NonSerialized] public", publicNonSerialized.ToString());
            Row(sb, json, "staticField", "static", staticField.ToString());
            Row(sb, json, "ConstValue", "const", ConstValue.ToString());
            Row(sb, json, "readonlyField", "readonly", readonlyField.ToString());
            Row(sb, json, "Property", "właściwość { get; set; }", Property.ToString());
            Row(sb, json, "grid", "int[,] (wielowymiarowa)", grid[0, 0].ToString());
            Row(sb, json, "nestedList", "List<List<int>>", nestedList[0][0].ToString());
            Row(sb, json, "dictionary", "Dictionary<string,int>", dictionary["zloto"].ToString());
            Row(sb, json, "plain", "klasa bez [Serializable]", plain.value.ToString());
            Row(sb, json, "shape", "interfejs", shape == null ? "null" : shape.ToString());
            sb.Append("\nLPM zmienia wszystkie wartości, PPM przywraca stan z JSON –\nwrócą tylko pola zapisane w JSON.");
            DemoHud.SetStatus("rules", sb.ToString());
        }

        static void Row(StringBuilder sb, string json, string key, string kind, string value)
        {
            bool inJson = json.Contains("\"" + key + "\"");
            string mark = inJson ? "<color=#7CFC7C>TAK</color>" : "<color=#FF7B7B>NIE</color>";
            string shownKey = key.Replace("<", "‹").Replace(">", "›");
            sb.Append(mark).Append("  <b>").Append(shownKey).Append("</b>  <color=#9fb4cc>(")
              .Append(kind.Replace("<", "‹").Replace(">", "›")).Append(")</color>  = ").Append(value).Append('\n');
        }
    }
}
