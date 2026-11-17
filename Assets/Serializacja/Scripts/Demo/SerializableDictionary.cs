using System;
using System.Collections.Generic;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Słownik, który Unity potrafi zapisać. Serializator nie obsługuje Dictionary,
    /// więc tuż przed zapisem kopiujemy pary do dwóch list (OnBeforeSerialize),
    /// a po wczytaniu odbudowujemy słownik z list (OnAfterDeserialize).
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] List<TKey> keys = new List<TKey>();
        [SerializeField] List<TValue> values = new List<TValue>();

        /// <summary>Liczniki wywołań – pokazują, jak często Unity serializuje obiekt (np. przy odświeżaniu Inspectora).</summary>
        [NonSerialized] public int beforeSerializeCount;
        [NonSerialized] public int afterDeserializeCount;
        [NonSerialized] public int duplicatesSkipped;

        public void OnBeforeSerialize()
        {
            beforeSerializeCount++;
            keys.Clear();
            values.Clear();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            // UWAGA: wywoływane poza wątkiem głównym (np. podczas ładowania) – nie wolno tu używać API Unity
            afterDeserializeCount++;
            Clear();
            duplicatesSkipped = 0;
            int n = Math.Min(keys.Count, values.Count);
            for (int i = 0; i < n; i++)
            {
                if (keys[i] == null || ContainsKey(keys[i]))
                {
                    duplicatesSkipped++;
                    continue;
                }
                Add(keys[i], values[i]);
            }
        }
    }

    /// <summary>Konkretny typ (nie generyczny) – czytelniejszy w Inspectorze i w kodzie.</summary>
    [Serializable]
    public class StringIntDictionary : SerializableDictionary<string, int> { }
}
