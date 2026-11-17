using System.Collections.Generic;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Scena 07: Instantiate = kopia przez serializację.
    /// Spacja – zwiększ wszystkie liczniki oryginału, LPM – Instantiate (nowa kopia), PPM – usuń kopie.
    /// </summary>
    public class CloneDemo : DemoActionBase
    {
        public CloneProbe original;
        readonly List<CloneProbe> m_Clones = new List<CloneProbe>();

        void Start()
        {
            CloneProbe.staticCounter = 0;
            Refresh();
        }

        public override void OnMainAction()
        {
            original.IncrementAll();
            DemoHud.Log("original.IncrementAll() – wszystkie liczniki +1");
            Refresh();
        }

        public override void OnSecondaryAction()
        {
            if (m_Clones.Count >= 5) { DemoHud.Log("Maksymalnie 5 kopii – PPM usuwa kopie"); return; }
            Vector3 pos = original.transform.position + Vector3.right * 3.2f * (m_Clones.Count + 1);
            var clone = Instantiate(original, pos, original.transform.rotation);
            clone.name = "Kopia_" + (m_Clones.Count + 1);
            clone.isClone = true;
            m_Clones.Add(clone);
            DemoHud.Log($"Instantiate(original) → {clone.name}, Awake w kopii: {clone.awakeCalls}×");
            Refresh();
        }

        public override void OnTertiaryAction()
        {
            foreach (var c in m_Clones) if (c != null) Destroy(c.gameObject);
            m_Clones.Clear();
            DemoHud.Log("Usunięto kopie");
            Refresh();
        }

        void Refresh()
        {
            SetLabel(original, "ORYGINAŁ");
            foreach (var c in m_Clones) SetLabel(c, c.name);
            DemoHud.SetStatus("clone",
                "<b>Oryginał</b>\n" + original.Describe() + "\n\n" +
                "Kopia dostaje wartości tylko pól <b>serializowanych</b>\n(serializedCounter, publicCounter). " +
                "Pola private i [NonSerialized]\nmają wartość początkową z kodu, static – jest wspólne dla klasy.\n" +
                "Referencje wewnątrz kopiowanej hierarchii są przemapowane na obiekty kopii.");
        }

        static void SetLabel(CloneProbe p, string title)
        {
            if (p == null) return;
            var l = p.GetComponent<WorldLabel>();
            if (l == null) return;
            l.header = title;
            l.extra = p.Describe();
        }

        void Update()
        {
            // static zmienia się dla wszystkich – odświeżamy etykiety kopii
            if (Time.frameCount % 20 == 0) Refresh();
        }
    }
}
