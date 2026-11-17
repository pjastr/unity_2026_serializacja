using System;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Zmienna współdzielona jako ScriptableObject (np. zdrowie gracza).
    /// initialValue jest zapisane w pliku .asset, runtimeValue – tylko w pamięci ([NonSerialized]).
    /// </summary>
    [CreateAssetMenu(fileName = "NowaZmienna", menuName = "Serializacja/Zmienna float")]
    public class FloatVariable : ScriptableObject
    {
        public float initialValue = 100f;
        public float maxValue = 100f;

        [NonSerialized] float m_RuntimeValue;
        [NonSerialized] bool m_Initialized;

        public event Action<float> Changed;

        public float Value
        {
            get { if (!m_Initialized) Reset(); return m_RuntimeValue; }
            set
            {
                m_Initialized = true;
                m_RuntimeValue = Mathf.Clamp(value, 0f, maxValue);
                if (Changed != null) Changed(m_RuntimeValue);
            }
        }

        public int ListenerCount => Changed == null ? 0 : Changed.GetInvocationList().Length;

        public void Reset()
        {
            m_Initialized = true;
            m_RuntimeValue = initialValue;
        }

        void OnEnable()
        {
            // OnEnable zasobu wywołuje się przy jego wczytaniu – także przy wejściu w tryb Play
            Reset();
        }
    }
}
