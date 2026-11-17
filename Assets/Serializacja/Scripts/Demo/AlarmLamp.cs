using UnityEngine;

namespace Serializacja
{
    /// <summary>Lampa alarmowa – słucha kanału zdarzeń (scena 06).</summary>
    public class AlarmLamp : MonoBehaviour
    {
        public GameEventChannel alarm;
        public Light lampLight;
        float m_Time;

        void OnEnable() { if (alarm != null) alarm.Raised += OnAlarm; }
        void OnDisable() { if (alarm != null) alarm.Raised -= OnAlarm; }

        void OnAlarm() { m_Time = 3f; }

        void Update()
        {
            m_Time = Mathf.Max(0f, m_Time - Time.deltaTime);
            if (lampLight != null)
                lampLight.intensity = m_Time > 0f ? (Mathf.Sin(Time.time * 20f) > 0 ? 8f : 0f) : 0f;
        }
    }
}
