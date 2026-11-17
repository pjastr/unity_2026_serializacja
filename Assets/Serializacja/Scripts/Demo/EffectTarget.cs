using UnityEngine;

namespace Serializacja
{
    /// <summary>Cel efektów (scena 03): punkty życia, mignięcie kolorem, kolor bazowy.</summary>
    public class EffectTarget : MonoBehaviour
    {
        public int hp = 100;
        public string displayName = "Cel";

        Renderer m_Renderer;
        MaterialPropertyBlock m_Block;
        Color m_Base = Color.white;
        Color m_Flash;
        float m_FlashTime;
        WorldLabel m_Label;

        void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_Block = new MaterialPropertyBlock();
            if (m_Renderer != null && m_Renderer.sharedMaterial != null)
                m_Base = m_Renderer.sharedMaterial.color;
            m_Label = GetComponent<WorldLabel>();
        }

        public void Change(int delta, Color flash)
        {
            hp = Mathf.Clamp(hp + delta, 0, 200);
            m_Flash = flash;
            m_FlashTime = 0.5f;
        }

        public void SetBaseColor(Color c)
        {
            m_Base = c;
        }

        void Update()
        {
            m_FlashTime = Mathf.Max(0f, m_FlashTime - Time.deltaTime);
            Color c = Color.Lerp(m_Base, m_Flash, m_FlashTime / 0.5f);
            if (m_Renderer != null)
            {
                m_Renderer.GetPropertyBlock(m_Block);
                m_Block.SetColor("_BaseColor", c);
                m_Block.SetColor("_Color", c);
                m_Renderer.SetPropertyBlock(m_Block);
            }
            if (m_Label != null)
                m_Label.extra = "HP = " + hp;
        }
    }
}
