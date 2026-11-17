using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Dane typu przeciwnika jako ScriptableObject – osobny plik .asset w projekcie.
    /// Wiele obiektów w scenie wskazuje ten sam zasób: dane istnieją w pamięci tylko raz.
    /// </summary>
    [CreateAssetMenu(fileName = "NowyTypPrzeciwnika", menuName = "Serializacja/Typ przeciwnika")]
    public class EnemyType : ScriptableObject
    {
        public string displayName = "Przeciwnik";
        [Min(1)] public int maxHp = 50;
        [Range(0.1f, 10f)] public float speed = 2f;
        public Color color = Color.red;
        [Range(0.3f, 3f)] public float size = 1f;

        public string Describe() => $"{displayName}: HP {maxHp}, speed {speed:0.00}, size {size:0.0}";
    }
}
