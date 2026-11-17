using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Scena 06: ScriptableObject jako kanał zdarzeń i zmienna współdzielona.
    /// Nadawca (ten komponent) nie ma referencji do lamp, drzwi ani paska – zna tylko zasoby.
    /// Spacja – alarm (GameEventChannel.Raise), LPM – obrażenia (−15), PPM – leczenie (+10).
    /// </summary>
    public class ChannelsDemo : DemoActionBase
    {
        public GameEventChannel alarm;
        public FloatVariable playerHealth;

        void Awake()
        {
            // przy wyłączonym przeładowaniu domeny (Enter Play Mode Options) zasób pamięta wartość z poprzedniego uruchomienia
            playerHealth.Reset();
        }

        public override void OnMainAction()
        {
            alarm.Raise();
            DemoHud.Log($"alarm.Raise() → powiadomiono {alarm.ListenerCount} odbiorców");
        }

        public override void OnSecondaryAction()
        {
            playerHealth.Value -= 15f;
            DemoHud.Log($"playerHealth.Value = {playerHealth.Value:0}");
        }

        public override void OnTertiaryAction()
        {
            playerHealth.Value += 10f;
            DemoHud.Log($"playerHealth.Value = {playerHealth.Value:0}");
        }

        void Update()
        {
            DemoHud.SetStatus("ch",
                $"<b>Zasób {alarm.name}</b> (GameEventChannel)\n   odbiorców: {alarm.ListenerCount}\n" +
                $"<b>Zasób {playerHealth.name}</b> (FloatVariable)\n   initialValue (w pliku) = {playerHealth.initialValue:0}\n" +
                $"   Value (tylko w pamięci) = {playerHealth.Value:0}\n   odbiorców: {playerHealth.ListenerCount}\n\n" +
                "Subskrypcje (event) nie są serializowane – odbiorcy\nzapisują się w OnEnable i wypisują w OnDisable.");
        }
    }
}
