using UnityEngine;

namespace Serializacja
{
    /// <summary>Moneta z trwałym identyfikatorem (id zapisuje się w pliku zapisu zamiast referencji do obiektu).</summary>
    public class Coin : MonoBehaviour
    {
        public string id;
        public int value = 10;

        void Update()
        {
            transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
        }
    }
}
