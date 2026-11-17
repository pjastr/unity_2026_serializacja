using UnityEngine;

namespace Serializacja
{
    /// <summary>Przeciwnik: zachowanie w komponencie, dane w zasobie EnemyType (referencja do ScriptableObject).</summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyType type;
        public Vector3 center;
        public float radius = 2f;
        public float phase;

        int m_Hp;
        Renderer m_Renderer;
        MaterialPropertyBlock m_Block;
        WorldLabel m_Label;
        float m_Angle;

        void Start()
        {
            m_Renderer = GetComponent<Renderer>();
            m_Block = new MaterialPropertyBlock();
            m_Label = GetComponent<WorldLabel>();
            m_Hp = type != null ? type.maxHp : 0;
            m_Angle = phase;
        }

        void Update()
        {
            if (type == null) return;
            m_Angle += type.speed / Mathf.Max(0.1f, radius) * Time.deltaTime;
            transform.position = center + new Vector3(Mathf.Cos(m_Angle), 0, Mathf.Sin(m_Angle)) * radius + Vector3.up * (type.size * 0.5f);
            transform.localScale = Vector3.one * type.size;
            if (m_Renderer != null)
            {
                m_Renderer.GetPropertyBlock(m_Block);
                m_Block.SetColor("_BaseColor", type.color);
                m_Block.SetColor("_Color", type.color);
                m_Renderer.SetPropertyBlock(m_Block);
            }
            if (m_Label != null)
            {
                m_Label.header = type.displayName;
                m_Label.extra = $"asset: {type.name} (id {type.GetInstanceID()})\nHP {m_Hp}, speed {type.speed:0.00}";
                m_Label.worldOffset = Vector3.up * (type.size * 0.6f + 0.2f);
            }
        }
    }
}
