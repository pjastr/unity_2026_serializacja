using System;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Scena 07: obiekt kopiowany przez Instantiate. Instantiate serializuje oryginał i deserializuje kopię,
    /// więc kopia dostaje TYLKO pola serializowane; pozostałe mają wartości z inicjalizatorów.
    /// </summary>
    public class CloneProbe : MonoBehaviour
    {
        [SerializeField] int serializedCounter;
        public int publicCounter;
        int privateCounter;
        [NonSerialized] public int nonSerializedCounter;
        public static int staticCounter;

        [Tooltip("Referencja do własnego dziecka – w kopii wskaże dziecko KOPII")]
        public Transform internalChild;
        [Tooltip("Referencja do obiektu spoza kopiowanej hierarchii – w kopii pozostaje bez zmian")]
        public Transform externalTarget;

        [NonSerialized] public int awakeCalls;
        [NonSerialized] public bool isClone;

        void Awake()
        {
            awakeCalls++;
        }

        public void IncrementAll()
        {
            serializedCounter++;
            publicCounter++;
            privateCounter++;
            nonSerializedCounter++;
            staticCounter++;
        }

        public string Describe()
        {
            string child = internalChild == null ? "null"
                : internalChild.IsChildOf(transform) ? "<color=#7CFC7C>własne dziecko</color>" : "<color=#FF7B7B>dziecko oryginału</color>";
            return $"[SerializeField] private: {serializedCounter}\npublic: {publicCounter}\nprivate: {privateCounter}\n" +
                   $"[NonSerialized]: {nonSerializedCounter}\nstatic: {staticCounter}\ninternalChild → {child}\n" +
                   $"externalTarget → {(externalTarget != null ? externalTarget.name : "null")}";
        }
    }
}
