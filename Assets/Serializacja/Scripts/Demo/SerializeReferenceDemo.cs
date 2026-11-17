using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Serializacja
{
    /// <summary>Bazowa klasa efektu. Klasy pochodne nadpisują Apply.</summary>
    [Serializable]
    public class Effect
    {
        public string label = "efekt";
        public virtual string Apply(EffectTarget target) => "Effect (klasa bazowa) – brak działania";
    }

    [Serializable]
    public class DamageEffect : Effect
    {
        public int amount = 15;
        public override string Apply(EffectTarget t) { t.Change(-amount, new Color(1f, 0.3f, 0.3f)); return $"DamageEffect: −{amount} HP"; }
    }

    [Serializable]
    public class HealEffect : Effect
    {
        public int amount = 10;
        public override string Apply(EffectTarget t) { t.Change(amount, new Color(0.4f, 1f, 0.4f)); return $"HealEffect: +{amount} HP"; }
    }

    [Serializable]
    public class ColorEffect : Effect
    {
        public Color color = new Color(0.3f, 0.5f, 1f);
        public override string Apply(EffectTarget t) { t.SetBaseColor(color); return "ColorEffect: nowy kolor"; }
    }

    /// <summary>
    /// Scena 03: [SerializeField] (po wartości) vs [SerializeReference] (po referencji).
    /// Obie listy zostały wypełnione TYMI SAMYMI efektami w edytorze (generator scen) i zapisane w pliku sceny.
    /// Spacja – zastosuj listę "po wartości" do celu A, LPM – listę [SerializeReference] do celu B,
    /// PPM – Instantiate tego obiektu i sprawdzenie współdzielonej referencji w kopii.
    /// </summary>
    public class SerializeReferenceDemo : DemoActionBase
    {
        [Tooltip("Serializacja po wartości – zapisuje się tylko typ deklarowany (Effect)")]
        [SerializeField] List<Effect> effectsByValue = new List<Effect>();

        [Tooltip("Serializacja po referencji – zapisuje typ rzeczywisty i zachowuje współdzielenie oraz null")]
        [SerializeReference] List<Effect> effectsByReference = new List<Effect>();

        [Header("Dwa pola wskazujące ten sam obiekt")]
        [SerializeReference] Effect sharedA;
        [SerializeReference] Effect sharedB;

        [Header("Cele (ustawiane przez generator)")]
        public EffectTarget targetA;
        public EffectTarget targetB;

        bool m_IsClone;

        /// <summary>Wywoływane przez generator scen – dane trafiają do pliku .unity.</summary>
        public void FillDemoData()
        {
            effectsByValue = new List<Effect>
            {
                new DamageEffect { label = "obrażenia" },
                new HealEffect { label = "leczenie" },
                new ColorEffect { label = "kolor" },
            };
            effectsByReference = new List<Effect>
            {
                new DamageEffect { label = "obrażenia" },
                new HealEffect { label = "leczenie" },
                new ColorEffect { label = "kolor" },
                null,
            };
            var shared = new DamageEffect { label = "wspólny", amount = 5 };
            sharedA = shared;
            sharedB = shared;
        }

        void Start()
        {
            if (m_IsClone) return;
            ShowStatus("");
        }

        public override void OnMainAction()
        {
            var sb = new StringBuilder("<b>Cel A ← lista [SerializeField]</b>\n");
            foreach (var e in effectsByValue)
                sb.Append(e == null ? "null" : e.Apply(targetA)).Append('\n');
            DemoHud.Log("Zastosowano efekty po wartości (cel A)");
            ShowStatus(sb.ToString());
        }

        public override void OnSecondaryAction()
        {
            var sb = new StringBuilder("<b>Cel B ← lista [SerializeReference]</b>\n");
            foreach (var e in effectsByReference)
                sb.Append(e == null ? "null (pusty element jest dozwolony)" : e.Apply(targetB)).Append('\n');
            DemoHud.Log("Zastosowano efekty po referencji (cel B)");
            ShowStatus(sb.ToString());
        }

        public override void OnTertiaryAction()
        {
            // Instantiate = serializacja + deserializacja obiektu
            var clone = Instantiate(this, transform.position + Vector3.back * 3f, Quaternion.identity);
            clone.m_IsClone = true;
            clone.name = "Kopia (Instantiate)";
            bool same = ReferenceEquals(clone.sharedA, clone.sharedB);
            bool sameAsOriginal = ReferenceEquals(clone.sharedA, sharedA);
            clone.sharedA.label = "zmieniony w kopii";
            string msg = $"Kopia: sharedA == sharedB → <b>{same}</b> (ten sam obiekt po deserializacji)\n" +
                         $"kopia.sharedA == oryginał.sharedA → <b>{sameAsOriginal}</b> (kopia ma własne obiekty)\n" +
                         $"po zmianie kopia.sharedA.label: kopia.sharedB.label = \"{clone.sharedB.label}\", " +
                         $"oryginał.sharedB.label = \"{sharedB.label}\"\n";
            Destroy(clone.gameObject, 2f);
            DemoHud.Log("Instantiate – sprawdzenie współdzielonej referencji");
            ShowStatus(msg);
        }

        void ShowStatus(string extra)
        {
            var sb = new StringBuilder();
            sb.Append("<b>[SerializeField] List<Effect></b> – typy po wczytaniu sceny:\n");
            foreach (var e in effectsByValue)
                sb.Append("   ").Append(e == null ? "null" : e.GetType().Name + " \"" + e.label + "\"").Append('\n');
            sb.Append("<b>[SerializeReference] List<Effect></b>:\n");
            foreach (var e in effectsByReference)
                sb.Append("   ").Append(e == null ? "null" : e.GetType().Name + " \"" + e.label + "\"").Append('\n');
            sb.Append("sharedA == sharedB → ").Append(ReferenceEquals(sharedA, sharedB)).Append('\n');
            if (!string.IsNullOrEmpty(extra))
                sb.Append('\n').Append(extra);
            string json = JsonUtility.ToJson(this, true);
            int refs = json.IndexOf("\"references\"", StringComparison.Ordinal);
            if (refs >= 0)
            {
                string part = json.Substring(refs);
                var lines = part.Split('\n');
                sb.Append("\n<b>JsonUtility – blok \"references\"</b> (rid → typ + dane):\n");
                for (int i = 0; i < Mathf.Min(lines.Length, 22); i++)
                    sb.Append(lines[i]).Append('\n');
            }
            DemoHud.SetStatus("ref", sb.ToString().Replace("List<Effect>", "List‹Effect›"));
        }
    }
}
