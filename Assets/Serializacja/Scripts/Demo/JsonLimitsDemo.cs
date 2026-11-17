using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    // ---------- przypadek 1: słownik ----------
    [Serializable]
    public class PricesProblem
    {
        public Dictionary<string, int> items = new Dictionary<string, int>();
    }

    [Serializable]
    public class PriceEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class PricesSolution
    {
        public List<PriceEntry> items = new List<PriceEntry>();   // słownik zapisany jako lista par
    }

    // ---------- przypadek 2: tablica jako korzeń dokumentu ----------
    [Serializable]
    public class Wrapper<T>
    {
        public List<T> items = new List<T>();
    }

    // ---------- przypadek 3: właściwości ----------
    [Serializable]
    public class ProfileProblem
    {
        public string Nick { get; set; }
        public int Level { get; set; }
    }

    [Serializable]
    public class ProfileSolution
    {
        [field: SerializeField] public string Nick { get; set; }
        [field: SerializeField] public int Level { get; set; }
    }

    // ---------- przypadek 4: polimorfizm ----------
    [Serializable]
    public class Animal
    {
        public string name;
    }

    [Serializable]
    public class Dog : Animal
    {
        public bool goodBoy = true;
    }

    [Serializable]
    public class Cat : Animal
    {
        public int lives = 9;
    }

    [Serializable]
    public class ShelterProblem
    {
        public List<Animal> animals = new List<Animal>();
    }

    [Serializable]
    public class ShelterSolution
    {
        [SerializeReference] public List<Animal> animals = new List<Animal>();
    }

    // ---------- przypadek 5: null ----------
    [Serializable]
    public class Weapon
    {
        public string name = "miecz";
    }

    [Serializable]
    public class HeroProblem
    {
        public Weapon weapon;          // null przed zapisem
    }

    [Serializable]
    public class HeroSolution
    {
        public bool hasWeapon;         // jawna flaga zamiast null
        public Weapon weapon;
    }

    /// <summary>
    /// Scena 10: ograniczenia JsonUtility i sposoby ich obejścia (bez dodatkowych pakietów).
    /// Spacja – następny przypadek, LPM – deserializacja (odczyt) problemu i rozwiązania.
    /// </summary>
    public class JsonLimitsDemo : DemoActionBase
    {
        static readonly string[] Cases =
        {
            "1. Dictionary – nie jest serializowany",
            "2. Tablica / lista jako korzeń dokumentu",
            "3. Właściwości { get; set; }",
            "4. Polimorfizm – List‹Animal› z Dog i Cat",
            "5. Wartość null w polu klasy",
        };

        int m_Case;
        string m_Extra = "";

        void Start() => Show();

        public override void OnMainAction()
        {
            m_Case = (m_Case + 1) % Cases.Length;
            m_Extra = "";
            Show();
        }

        public override void OnSecondaryAction()
        {
            m_Extra = Deserialize();
            Show();
        }

        void Samples(out object problem, out object solution, out string hint)
        {
            switch (m_Case)
            {
                case 0:
                {
                    var p = new PricesProblem();
                    p.items["miecz"] = 120;
                    p.items["tarcza"] = 80;
                    var s = new PricesSolution();
                    foreach (var kv in p.items) s.items.Add(new PriceEntry { key = kv.Key, value = kv.Value });
                    problem = p; solution = s;
                    hint = "Rozwiązanie: lista par klucz–wartość (albo ISerializationCallbackReceiver – scena 04).";
                    return;
                }
                case 1:
                {
                    problem = new List<int> { 1, 2, 3 };
                    var w = new Wrapper<int>();
                    w.items.AddRange(new[] { 1, 2, 3 });
                    solution = w;
                    hint = "JsonUtility zapisuje tylko obiekt – lista jako korzeń daje \"{}\".\nRozwiązanie: klasa opakowująca Wrapper‹T› { List‹T› items }.";
                    return;
                }
                case 2:
                    problem = new ProfileProblem { Nick = "Gracz1", Level = 7 };
                    solution = new ProfileSolution { Nick = "Gracz1", Level = 7 };
                    hint = "Rozwiązanie: [field: SerializeField] – zapisuje ukryte pole właściwości automatycznej.";
                    return;
                case 3:
                {
                    var p = new ShelterProblem();
                    p.animals.Add(new Dog { name = "Burek" });
                    p.animals.Add(new Cat { name = "Mruczek" });
                    var s = new ShelterSolution();
                    s.animals.Add(new Dog { name = "Burek" });
                    s.animals.Add(new Cat { name = "Mruczek" });
                    problem = p; solution = s;
                    hint = "Rozwiązanie: [SerializeReference] – w JSON pojawia się blok references z typem (scena 03).";
                    return;
                }
                default:
                    problem = new HeroProblem { weapon = null };
                    solution = new HeroSolution { hasWeapon = false, weapon = null };
                    hint = "JsonUtility zapisuje null jako pusty obiekt – po odczycie pole NIE jest null.\nRozwiązanie: jawna flaga (hasWeapon) albo [SerializeReference].";
                    return;
            }
        }

        void Show()
        {
            object problem, solution;
            string hint;
            Samples(out problem, out solution, out hint);
            var sb = new StringBuilder();
            sb.Append("<b>").Append(Cases[m_Case]).Append("</b>\n\n");
            string pj = JsonUtility.ToJson(problem, true);
            sb.Append("<b><color=#FF9F7B>Problem</color> – JsonUtility.ToJson</b>\n").Append(string.IsNullOrEmpty(pj) ? "(pusty)" : pj).Append("\n\n");
            sb.Append("<b><color=#7CFC7C>Rozwiązanie</color> – JsonUtility.ToJson</b>\n").Append(Shorten(JsonUtility.ToJson(solution, true), 28)).Append("\n\n");
            sb.Append("<color=#9fb4cc>").Append(hint).Append("</color>\n");
            if (!string.IsNullOrEmpty(m_Extra))
                sb.Append("\n<b>Odczyt (FromJson)</b>\n").Append(m_Extra);
            DemoHud.SetStatus("json", sb.ToString());
        }

        static string Shorten(string s, int maxLines)
        {
            var lines = s.Split('\n');
            return lines.Length <= maxLines ? s : string.Join("\n", lines, 0, maxLines) + "\n…";
        }

        string Deserialize()
        {
            object problem, solution;
            string hint;
            Samples(out problem, out solution, out hint);
            string pj = JsonUtility.ToJson(problem);
            string sj = JsonUtility.ToJson(solution);
            switch (m_Case)
            {
                case 0:
                {
                    var a = JsonUtility.FromJson<PricesProblem>(pj);
                    var b = JsonUtility.FromJson<PricesSolution>(sj);
                    return $"problem: items = {a.items.Count} par (słownik pusty – dane utracone)\nrozwiązanie: items = {b.items.Count} par";
                }
                case 1:
                {
                    var b = JsonUtility.FromJson<Wrapper<int>>(sj);
                    return "problem: brak danych do odczytu\nrozwiązanie: " + string.Join(", ", b.items);
                }
                case 2:
                {
                    var a = JsonUtility.FromJson<ProfileProblem>(pj);
                    var b = JsonUtility.FromJson<ProfileSolution>(sj);
                    return $"problem: Nick = {a.Nick ?? "null"}, Level = {a.Level}\nrozwiązanie: Nick = {b.Nick}, Level = {b.Level}";
                }
                case 3:
                {
                    var a = JsonUtility.FromJson<ShelterProblem>(pj);
                    var b = JsonUtility.FromJson<ShelterSolution>(sj);
                    return "problem: " + Types(a.animals) + "\nrozwiązanie: " + Types(b.animals);
                }
                default:
                {
                    var a = JsonUtility.FromJson<HeroProblem>(pj);
                    var b = JsonUtility.FromJson<HeroSolution>(sj);
                    return $"problem: weapon == null → {a.weapon == null} (pole wypełnione obiektem \"{(a.weapon != null ? a.weapon.name : "")}\")\n" +
                           $"rozwiązanie: hasWeapon = {b.hasWeapon} → kod traktuje broń jako brak";
                }
            }
        }

        static string Types(List<Animal> list)
        {
            var parts = new List<string>();
            foreach (var a in list) parts.Add(a == null ? "null" : a.GetType().Name + " " + a.name);
            return string.Join(", ", parts.ToArray());
        }
    }
}
