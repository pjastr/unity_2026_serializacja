using System;
using System.Collections.Generic;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Stan gry do zapisu (scena 09). Zwykła klasa C# z [Serializable] – bez referencji do obiektów sceny:
    /// zapisujemy identyfikatory i wartości, a nie GameObjecty.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string savedAt;
        public Vector3 playerPosition;
        public float playerYaw;
        public int score;
        public float playTime;
        public List<string> collectedCoins = new List<string>();
    }
}
