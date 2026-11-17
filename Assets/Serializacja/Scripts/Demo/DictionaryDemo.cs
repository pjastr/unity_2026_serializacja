using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Scena 04: ISerializationCallbackReceiver – zapis słownika (cennik sklepu).
    /// Spacja – dodaj towar, LPM – podróż ToJson → FromJson (słownik odbudowany z list),
    /// PPM – wczytaj JSON z powtórzonym kluczem (obsługa błędnych danych).
    /// </summary>
    public class DictionaryDemo : DemoActionBase
    {
        public StringIntDictionary prices = new StringIntDictionary();
        [Tooltip("Zwykły słownik – dla porównania (nie jest serializowany)")]
        public Dictionary<string, int> plainDictionary = new Dictionary<string, int>();

        [Header("Wizualizacja (ustawiane przez generator)")]
        public Transform shelf;
        public Material[] materials;

        [Serializable]
        class Shop
        {
            public StringIntDictionary prices;
        }

        static readonly string[] Goods = { "miecz", "tarcza", "mikstura", "łuk", "strzały", "hełm", "zwój", "pierścień", "kilof", "lina" };
        int m_Next;
        string m_LastJson = "";
        string m_Info = "";
        bool m_PlainInJson;

        void Start()
        {
            if (prices.Count == 0)
            {
                prices["miecz"] = 120;
                prices["tarcza"] = 80;
            }
            foreach (var p in prices) plainDictionary[p.Key] = p.Value;
            m_Next = 2;
            m_PlainInJson = JsonUtility.ToJson(this).Contains("plainDictionary");
            Rebuild();
        }

        public override void OnMainAction()
        {
            string key = Goods[m_Next % Goods.Length] + (m_Next >= Goods.Length ? " " + (m_Next / Goods.Length + 1) : "");
            m_Next++;
            int price = UnityEngine.Random.Range(5, 300);
            prices[key] = price;
            plainDictionary[key] = price;
            DemoHud.Log($"prices[\"{key}\"] = {price}");
            m_Info = "";
            Rebuild();
        }

        public override void OnSecondaryAction()
        {
            m_LastJson = JsonUtility.ToJson(new Shop { prices = prices }, true);
            var copy = JsonUtility.FromJson<Shop>(m_LastJson);
            m_Info = $"Kopia po FromJson: {copy.prices.Count} par, OnAfterDeserialize wywołane {copy.prices.afterDeserializeCount}×\n" +
                     "Słownik odbudowany z list keys / values.";
            DemoHud.Log("ToJson → FromJson (podróż w obie strony)");
            Rebuild();
        }

        public override void OnTertiaryAction()
        {
            const string broken = "{\"prices\":{\"keys\":[\"miecz\",\"miecz\",\"tarcza\"],\"values\":[1,2,3]}}";
            var copy = JsonUtility.FromJson<Shop>(broken);
            m_LastJson = broken;
            m_Info = $"Wczytano JSON z powtórzonym kluczem \"miecz\":\npar w słowniku = {copy.prices.Count}, pominięte duplikaty = {copy.prices.duplicatesSkipped}\n" +
                     "Bez tej kontroli Dictionary.Add rzuciłby wyjątek ArgumentException.";
            DemoHud.Log("FromJson z błędnymi danymi (duplikat klucza)");
            Rebuild();
        }

        void Rebuild()
        {
            var sb = new StringBuilder();
            sb.Append("<b>prices</b> (StringIntDictionary) – ").Append(prices.Count).Append(" par\n");
            foreach (var p in prices)
                sb.Append("   ").Append(p.Key).Append(" → ").Append(p.Value).Append(" zł\n");
            sb.Append("\nOnBeforeSerialize: <b>").Append(prices.beforeSerializeCount).Append("×</b>   ");
            sb.Append("OnAfterDeserialize: <b>").Append(prices.afterDeserializeCount).Append("×</b>\n");
            sb.Append("(zaznacz DemoManager w Hierarchy – Inspector serializuje obiekt przy każdym odświeżeniu)\n");
            sb.Append("\n<b>plainDictionary</b> (Dictionary): ").Append(plainDictionary.Count)
              .Append(" par – w JSON: ").Append(m_PlainInJson ? "TAK" : "NIE").Append('\n');
            if (!string.IsNullOrEmpty(m_Info))
                sb.Append('\n').Append(m_Info).Append('\n');
            if (!string.IsNullOrEmpty(m_LastJson))
            {
                var lines = m_LastJson.Split('\n');
                sb.Append("\n<b>JSON</b>\n");
                for (int i = 0; i < Mathf.Min(lines.Length, 26); i++)
                    sb.Append(lines[i]).Append('\n');
            }
            DemoHud.SetStatus("dict", sb.ToString());

            if (shelf == null) return;
            for (int i = shelf.childCount - 1; i >= 0; i--)
                Destroy(shelf.GetChild(i).gameObject);
            int idx = 0;
            foreach (var p in prices)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = p.Key;
                go.transform.SetParent(shelf, false);
                float h = 0.3f + p.Value / 100f;
                go.transform.localPosition = new Vector3((idx % 6) * 1.6f, h / 2f, (idx / 6) * -2.2f);
                go.transform.localScale = new Vector3(1f, h, 1f);
                if (materials != null && materials.Length > 0)
                    go.GetComponent<Renderer>().sharedMaterial = materials[idx % materials.Length];
                var l = go.AddComponent<WorldLabel>();
                l.header = p.Key;
                l.extra = p.Value + " zł";
                l.worldOffset = new Vector3(0, h / 2f + 0.3f, 0);
                idx++;
            }
        }

        void Update()
        {
            // liczniki rosną także wtedy, gdy edytor serializuje obiekt (Inspector)
            if (Time.frameCount % 15 == 0)
                Rebuild_StatusOnly();
        }

        void Rebuild_StatusOnly()
        {
            // odświeżamy tylko tekst (bez przebudowy półki)
            var t = shelf;
            shelf = null;
            Rebuild();
            shelf = t;
        }
    }
}
