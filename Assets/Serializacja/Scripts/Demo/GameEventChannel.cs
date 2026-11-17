using System;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Kanał zdarzeń jako ScriptableObject: nadawca i odbiorcy znają tylko zasób, a nie siebie nawzajem.
    /// </summary>
    [CreateAssetMenu(fileName = "NoweZdarzenie", menuName = "Serializacja/Kanał zdarzeń")]
    public class GameEventChannel : ScriptableObject
    {
        [TextArea] public string description;

        /// <summary>Subskrybenci nie są serializowani – lista istnieje tylko w czasie działania gry.</summary>
        public event Action Raised;

        public int ListenerCount => Raised == null ? 0 : Raised.GetInvocationList().Length;

        public void Raise()
        {
            if (Raised != null) Raised();
        }
    }
}
