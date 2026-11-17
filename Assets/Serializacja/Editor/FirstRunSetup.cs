using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Serializacja.EditorTools
{
    /// <summary>
    /// Przy pierwszym otwarciu projektu (brak wygenerowanych scen) proponuje wygenerowanie wszystkich scen.
    /// Pytanie pojawia się raz na sesję edytora; zawsze można też użyć menu Serializacja > Generuj wszystkie sceny.
    /// </summary>
    [InitializeOnLoad]
    static class FirstRunSetup
    {
        const string SessionKey = "Serializacja.FirstRunAsked";

        static FirstRunSetup()
        {
            EditorApplication.delayCall += Check;
        }

        static void Check()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (SessionState.GetBool(SessionKey, false))
                return;
            SessionState.SetBool(SessionKey, true);
            if (File.Exists(SceneBuilder.ScenePath(SceneBuilder.Scenes[0].file)))
                return;
            if (!EditorUtility.DisplayDialog("Serializacja – pierwsze uruchomienie",
                    $"Sceny demonstracyjne nie są jeszcze wygenerowane.\n\nWygenerować teraz {SceneBuilder.Scenes.Length} scen ({SceneBuilder.ScenesDir})?\n" +
                    "Trwa to kilkanaście sekund. Można to zrobić później z menu Serializacja.",
                    "Generuj", "Później"))
                return;
            SceneBuilder.GenerateAll();
            EditorSceneManager.OpenScene(SceneBuilder.ScenePath(SceneBuilder.Scenes[0].file));
        }
    }
}
