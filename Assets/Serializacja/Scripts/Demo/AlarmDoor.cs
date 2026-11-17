using UnityEngine;

namespace Serializacja
{
    /// <summary>Drzwi – otwierają się na zdarzenie alarmu (scena 06).</summary>
    public class AlarmDoor : MonoBehaviour
    {
        public GameEventChannel alarm;
        public float openHeight = 2.5f;
        Vector3 m_Closed;
        bool m_Open;

        void Awake() { m_Closed = transform.position; }
        void OnEnable() { if (alarm != null) alarm.Raised += OnAlarm; }
        void OnDisable() { if (alarm != null) alarm.Raised -= OnAlarm; }

        void OnAlarm() { m_Open = !m_Open; }

        void Update()
        {
            Vector3 target = m_Closed + (m_Open ? Vector3.up * openHeight : Vector3.zero);
            transform.position = Vector3.MoveTowards(transform.position, target, 3f * Time.deltaTime);
        }
    }
}
