using UnityEngine;

namespace Serializacja
{
    /// <summary>Pasek zdrowia – słucha zmiennej FloatVariable (scena 06).</summary>
    public class HealthBarView : MonoBehaviour
    {
        public FloatVariable health;
        public Transform fill;

        void OnEnable() { if (health != null) health.Changed += OnChanged; }
        void OnDisable() { if (health != null) health.Changed -= OnChanged; }
        void Start() { if (health != null) OnChanged(health.Value); }

        void OnChanged(float v)
        {
            if (fill == null) return;
            float k = health.maxValue > 0 ? v / health.maxValue : 0f;
            fill.localScale = new Vector3(Mathf.Max(0.001f, k), 1f, 1f);
            fill.localPosition = new Vector3((k - 1f) * 0.5f, 0f, fill.localPosition.z);
            var l = GetComponent<WorldLabel>();
            if (l != null) l.extra = $"{v:0} / {health.maxValue:0}";
        }
    }
}
