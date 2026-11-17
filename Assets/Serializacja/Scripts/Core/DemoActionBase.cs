using System.Collections.Generic;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Bazowa klasa dla komponentów reagujących na akcje sceny:
    ///  • akcja główna – Spacja (Player/Jump) → OnMainAction(),
    ///  • akcja dodatkowa – LPM (Player/Attack) → OnSecondaryAction(),
    ///  • trzecia akcja – PPM (UI/RightClick) → OnTertiaryAction().
    /// Komponenty rejestrują się same; DemoHud wywołuje metody na wszystkich aktywnych.
    /// </summary>
    public abstract class DemoActionBase : MonoBehaviour
    {
        static readonly List<DemoActionBase> s_All = new List<DemoActionBase>();

        protected virtual void OnEnable()
        {
            if (!s_All.Contains(this))
                s_All.Add(this);
        }

        protected virtual void OnDisable()
        {
            s_All.Remove(this);
        }

        /// <summary>Spacja (akcja Player/Jump).</summary>
        public virtual void OnMainAction() { }

        /// <summary>LPM (akcja Player/Attack).</summary>
        public virtual void OnSecondaryAction() { }

        /// <summary>PPM (akcja UI/RightClick).</summary>
        public virtual void OnTertiaryAction() { }

        public static void InvokeTertiary()
        {
            foreach (var a in s_All.ToArray())
                if (a != null && a.isActiveAndEnabled)
                    a.OnTertiaryAction();
        }

        public static void InvokeMain()
        {
            foreach (var a in s_All.ToArray())
                if (a != null && a.isActiveAndEnabled)
                    a.OnMainAction();
        }

        public static void InvokeSecondary()
        {
            foreach (var a in s_All.ToArray())
                if (a != null && a.isActiveAndEnabled)
                    a.OnSecondaryAction();
        }
    }
}
