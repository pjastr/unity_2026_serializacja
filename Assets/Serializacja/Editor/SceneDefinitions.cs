using System.IO;
using UnityEditor;
using UnityEngine;

namespace Serializacja.EditorTools
{
    public static partial class SceneBuilder
    {
        /// <summary>Lista scen w kolejności numeracji.</summary>
        public static readonly SceneDef[] Scenes =
        {
            new SceneDef("01_Reguly_serializacji", S01_Rules),
            new SceneDef("02_Klasy_Serializable", S02_Classes),
            new SceneDef("03_SerializeReference", S03_SerializeReference),
            new SceneDef("04_Callbacki_Slownik", S04_Dictionary),
            new SceneDef("05_ScriptableObject_Dane", S05_ScriptableData),
            new SceneDef("06_ScriptableObject_Kanaly", S06_Channels),
            new SceneDef("07_Instantiate_Klonowanie", S07_Clone),
            new SceneDef("08_YAML_Format_plikow", S08_Yaml),
            new SceneDef("09_JsonUtility_ZapisGry", S09_SaveGame),
            new SceneDef("10_JsonUtility_ograniczenia", S10_JsonLimits),
            new SceneDef("11_Zapis_binarny_Wersje", S11_Binary),
            new SceneDef("12_Bezpieczny_zapis_Sloty", S12_Secure),
            new SceneDef("13_PlayerPrefs_Ustawienia", S13_PlayerPrefs),
            new SceneDef("14_PlayerPrefs_Rekordy_JSON", S14_HighScores),
        };

        // =====================================================================
        //  WSPÓLNE ZASOBY (ScriptableObject)
        // =====================================================================

        static string AssetPath(string name) => $"{DataDir}/{name}.asset";

        static void CreateSharedAssets()
        {
            MakeEnemyType("Goblin", "Goblin", 30, 2.0f, new Color(0.35f, 0.75f, 0.3f), 0.8f);
            MakeEnemyType("Ork", "Ork", 80, 1.4f, new Color(0.85f, 0.45f, 0.2f), 1.2f);
            MakeEnemyType("Troll", "Troll", 200, 0.8f, new Color(0.45f, 0.45f, 0.75f), 1.8f);

            if (AssetDatabase.LoadAssetAtPath<GameEventChannel>(AssetPath("Alarm")) == null)
            {
                var ev = ScriptableObject.CreateInstance<GameEventChannel>();
                ev.description = "Zdarzenie alarmu – nadawca: DemoManager, odbiorcy: lampy i drzwi.";
                AssetDatabase.CreateAsset(ev, AssetPath("Alarm"));
            }
            if (AssetDatabase.LoadAssetAtPath<FloatVariable>(AssetPath("ZdrowieGracza")) == null)
            {
                var v = ScriptableObject.CreateInstance<FloatVariable>();
                v.initialValue = 100f;
                v.maxValue = 100f;
                AssetDatabase.CreateAsset(v, AssetPath("ZdrowieGracza"));
            }
            AssetDatabase.SaveAssets();
        }

        static EnemyType MakeEnemyType(string file, string display, int hp, float speed, Color color, float size)
        {
            string path = AssetPath(file);
            var t = AssetDatabase.LoadAssetAtPath<EnemyType>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<EnemyType>();
                AssetDatabase.CreateAsset(t, path);
            }
            t.displayName = display;
            t.maxHp = hp;
            t.speed = speed;
            t.color = color;
            t.size = size;
            EditorUtility.SetDirty(t);
            return t;
        }

        static T Load<T>(string name) where T : Object => AssetDatabase.LoadAssetAtPath<T>(AssetPath(name));

        const string Keys3 = "<b>Spacja</b>, <b>LPM</b>, <b>PPM</b> – akcje sceny (opis w panelu)";

        // =====================================================================
        // 01 – REGUŁY SERIALIZACJI
        // =====================================================================
        static void S01_Rules()
        {
            Begin("01 · Co serializuje Unity?",
                "<b>Serializacja</b> – zamiana obiektu w pamięci na dane, które można zapisać (plik sceny, prefab, asset, JSON) " +
                "i później odtworzyć (<b>deserializacja</b>).\n\n" +
                "Serializator Unity zapisuje pola, które są:\n" +
                "• <b>public</b> albo oznaczone <b>[SerializeField]</b>,\n" +
                "• nie są static, const ani readonly,\n" +
                "• mają typ serializowalny: typy proste, enum, string, Vector3, Color, AnimationCurve…, " +
                "tablice T[] i List<T>, klasy/struktury z [Serializable], referencje do UnityEngine.Object.\n\n" +
                "Nie zapisuje: właściwości, Dictionary, tablic wielowymiarowych, list zagnieżdżonych, interfejsów " +
                "i pól [NonSerialized]. Auto-właściwość można zapisać przez <b>[field: SerializeField]</b> " +
                "(zapisywane jest ukryte pole <i>k__BackingField</i>).\n\n" +
                "Te same reguły obowiązują w Inspectorze, przy zapisie sceny, przy Instantiate i w JsonUtility. " +
                "Zaznacz obiekt <b>Komponent_testowy</b> i porównaj Inspector z tabelą.",
                "<b>Spacja</b> – tabela ↔ JSON   <b>LPM</b> – zmień wszystkie pola   <b>PPM</b> – przywróć z JSON",
                "", statusWidth: 640f);
            Floor();
            Cam(new Vector3(0, 3.2f, -7.5f), new Vector3(0, 1f, 0));
            var go = Box("Komponent_testowy", new Vector3(0, 1f, 0), new Vector3(2f, 2f, 2f), M_Teal);
            var demo = go.AddComponent<SerializationRulesDemo>();
            var target = Ball("Cel_referencji", new Vector3(2.5f, 0.5f, 1.5f), 1f, M_Orange);
            demo.reference = target.transform;
            Label(go, "SerializationRulesDemo", 1.4f);
            Label(target, "Transform – pole reference", 0.8f);
        }

        // =====================================================================
        // 02 – KLASY [Serializable]
        // =====================================================================
        static void S02_Classes()
        {
            Begin("02 · Własne klasy i struktury [Serializable]",
                "Klasa lub struktura oznaczona <b>[System.Serializable]</b> jest zapisywana <b>po wartości</b> – " +
                "jej pola trafiają do pliku „w miejscu”, wewnątrz obiektu, który ją zawiera.\n\n" +
                "Konsekwencje:\n" +
                "• <b>brak polimorfizmu</b> – w List<Item> obiekt Sword : Item zapisze się jako Item (pole sharpness ginie),\n" +
                "• <b>brak null</b> – pole klasy po deserializacji zawiera pusty obiekt, a nie null,\n" +
                "• <b>brak współdzielenia</b> – dwa pola wskazujące ten sam obiekt po wczytaniu wskazują dwie osobne kopie,\n" +
                "• klasa <b>bez [Serializable]</b> jest pomijana,\n" +
                "• limit głębokości zagnieżdżenia: 10 poziomów (np. klasa zawierająca samą siebie).\n\n" +
                "Górny rząd – oryginał, dolny – kopia po ToJson → FromJson.",
                "<b>Spacja</b> – następny przypadek   <b>LPM</b> – ponów ToJson → FromJson", "", statusWidth: 640f);
            Floor();
            Cam(new Vector3(2.2f, 5.5f, -8.5f), new Vector3(2.2f, 0.5f, 0.5f));
            var demo = Manager<SerializableClassesDemo>();
            var orig = Group("Oryginal");
            orig.position = new Vector3(0, 0, 2f);
            var copy = Group("Kopia_po_deserializacji");
            copy.position = new Vector3(0, 0, -1.5f);
            demo.originalRow = orig;
            demo.copyRow = copy;
            demo.itemMaterial = M_Blue;
            demo.swordMaterial = M_Red;
            SectionLabel("ORYGINAŁ", new Vector3(-2.2f, 0.5f, 2f));
            SectionLabel("KOPIA", new Vector3(-2.2f, 0.5f, -1.5f));
        }

        // =====================================================================
        // 03 – [SerializeReference]
        // =====================================================================
        static void S03_SerializeReference()
        {
            Begin("03 · [SerializeReference] – polimorfizm i współdzielenie",
                "<b>[SerializeReference]</b> zapisuje obiekt <b>po referencji</b>: dane trafiają do osobnej sekcji " +
                "(<i>references</i>, każdy obiekt ma identyfikator <i>rid</i>) razem z <b>pełną nazwą typu</b>.\n\n" +
                "Dzięki temu:\n" +
                "• działa <b>polimorfizm</b> – pole typu bazowego/interfejsu może przechowywać obiekt klasy pochodnej,\n" +
                "• można zapisać <b>null</b>,\n" +
                "• dwa pola mogą wskazywać <b>ten sam obiekt</b> – po wczytaniu nadal jest to jeden obiekt,\n" +
                "• można zapisywać grafy obiektów (także cykliczne).\n\n" +
                "Obie listy zostały wypełnione w edytorze tymi samymi efektami (DamageEffect, HealEffect, ColorEffect). " +
                "Po zapisie sceny lista [SerializeField] zawiera już tylko obiekty klasy bazowej Effect.\n\n" +
                "Uwaga: zmiana nazwy klasy użytej w [SerializeReference] psuje dane – pomaga atrybut <b>[MovedFrom]</b>.",
                "<b>Spacja</b> – lista [SerializeField] → cel A   <b>LPM</b> – lista [SerializeReference] → cel B   <b>PPM</b> – Instantiate i test referencji",
                "", statusWidth: 640f);
            Floor();
            Cam(new Vector3(0, 4.2f, -8.5f), new Vector3(0, 1f, 0));
            var data = Ball("Efekty_dane", new Vector3(0, 1f, 1.5f), 1.2f, M_Purple);
            var demo = data.AddComponent<SerializeReferenceDemo>();
            demo.FillDemoData();
            Label(data, "SerializeReferenceDemo\n(dane efektów)", 0.9f);
            var a = Box("Cel_A", new Vector3(-2.6f, 1f, -0.5f), new Vector3(1.6f, 2f, 1.6f), M_White);
            var ta = a.AddComponent<EffectTarget>();
            ta.displayName = "A";
            Label(a, "Cel A – [SerializeField]", 1.4f);
            var b = Box("Cel_B", new Vector3(2.6f, 1f, -0.5f), new Vector3(1.6f, 2f, 1.6f), M_White);
            var tb = b.AddComponent<EffectTarget>();
            tb.displayName = "B";
            Label(b, "Cel B – [SerializeReference]", 1.4f);
            demo.targetA = ta;
            demo.targetB = tb;
        }

        // =====================================================================
        // 04 – ISerializationCallbackReceiver
        // =====================================================================
        static void S04_Dictionary()
        {
            Begin("04 · ISerializationCallbackReceiver – zapis słownika",
                "Typów nieobsługiwanych przez serializator (np. <b>Dictionary</b>) nie da się zapisać wprost. " +
                "Interfejs <b>ISerializationCallbackReceiver</b> daje dwa „haki”:\n\n" +
                "• <b>OnBeforeSerialize()</b> – tuż przed zapisem: przepisujemy dane do pól, które Unity umie zapisać " +
                "(tu: dwie listy keys i values),\n" +
                "• <b>OnAfterDeserialize()</b> – tuż po wczytaniu: odbudowujemy słownik z list.\n\n" +
                "Uwagi:\n" +
                "• w edytorze OnBeforeSerialize jest wołane bardzo często (każde odświeżenie Inspectora) – metoda ma być szybka,\n" +
                "• OnAfterDeserialize może działać poza wątkiem głównym – nie wolno w nim wywoływać API Unity,\n" +
                "• dane z pliku mogą być błędne (np. powtórzony klucz) – trzeba je sprawdzić.\n\n" +
                "Wysokość pudełka na półce = cena towaru.",
                "<b>Spacja</b> – dodaj towar   <b>LPM</b> – ToJson → FromJson   <b>PPM</b> – JSON z błędem (duplikat klucza)",
                "", statusWidth: 640f);
            Floor();
            Cam(new Vector3(3.8f, 6f, -8f), new Vector3(3.8f, 0.5f, -1f));
            var demo = Manager<DictionaryDemo>();
            var shelf = Group("Polka");
            shelf.position = new Vector3(0, 0, 0);
            demo.shelf = shelf;
            demo.materials = new[] { M_Red, M_Orange, M_Yellow, M_Green, M_Teal, M_Blue, M_Purple };
            Box("Podstawa_polki", new Vector3(4f, 0.05f, -1.1f), new Vector3(10.5f, 0.1f, 5f), M_Wood);
        }

        // =====================================================================
        // 05 – ScriptableObject – dane
        // =====================================================================
        static void S05_ScriptableData()
        {
            Begin("05 · ScriptableObject – wspólne dane",
                "<b>ScriptableObject</b> to obiekt danych zapisany jako osobny plik <b>.asset</b> " +
                "(menu Assets > Create, dzięki atrybutowi [CreateAssetMenu]). Serializuje się według tych samych reguł co MonoBehaviour, " +
                "ale nie jest przypięty do obiektu w scenie.\n\n" +
                "Tu: trzy zasoby <b>EnemyType</b> (Goblin, Ork, Troll) w " + DataDir + ". Dziewięć obiektów <b>Enemy</b> " +
                "przechowuje tylko <b>referencję</b> do zasobu – dane istnieją w pamięci raz.\n\n" +
                "• Spacja zmienia zasób Goblin – przyspieszają <b>wszystkie</b> gobliny.\n" +
                "• LPM tworzy <b>Instantiate(zasób)</b> – kopię w pamięci tylko dla jednego goblina.\n\n" +
                "<b>Pułapka:</b> w edytorze zmiany zasobu w trybie Play zostają w pliku .asset po zatrzymaniu gry " +
                "(w buildzie – nie). Ten skrypt przywraca wartości w OnDestroy.",
                "<b>Spacja</b> – zmień zasób Goblin   <b>LPM</b> – kopia zasobu dla jednego goblina   <b>PPM</b> – przywróć",
                "", statusWidth: 660f);
            Floor();
            Cam(new Vector3(0, 9f, -11f), new Vector3(0, 0, 0.5f));
            var types = new[] { Load<EnemyType>("Goblin"), Load<EnemyType>("Ork"), Load<EnemyType>("Troll") };
            var demo = Manager<ScriptableObjectDataDemo>();
            demo.types = types;
            var list = new System.Collections.Generic.List<Enemy>();
            var g = Group("Przeciwnicy");
            Vector3[] centers = { new Vector3(-4.5f, 0, 0), new Vector3(0, 0, 0), new Vector3(4.5f, 0, 0) };
            for (int t = 0; t < 3; t++)
            {
                for (int i = 0; i < 3; i++)
                {
                    var go = Prim(PrimitiveType.Capsule, $"{types[t].name}_{i + 1}", centers[t], Vector3.one, M_White, g);
                    NoCollider(go);
                    var e = go.AddComponent<Enemy>();
                    e.type = types[t];
                    e.center = centers[t];
                    e.radius = 1.6f;
                    e.phase = i * 2.094f;
                    Label(go, types[t].name, 1f, 13);
                    list.Add(e);
                }
                SectionLabel("zasób " + types[t].name + ".asset", centers[t] + new Vector3(0, 0.1f, -2.6f), null, 16);
            }
            demo.enemies = list.ToArray();
            demo.specialEnemy = list[0];
        }

        // =====================================================================
        // 06 – ScriptableObject – kanały zdarzeń
        // =====================================================================
        static void S06_Channels()
        {
            Begin("06 · ScriptableObject – kanały zdarzeń i zmienne",
                "Zasoby ScriptableObject mogą łączyć systemy gry <b>bez bezpośrednich referencji</b> między obiektami sceny:\n\n" +
                "• <b>GameEventChannel</b> (Alarm.asset) – nadawca wywołuje Raise(), odbiorcy (lampy, drzwi) " +
                "zapisują się na zdarzenie w OnEnable i wypisują w OnDisable,\n" +
                "• <b>FloatVariable</b> (ZdrowieGracza.asset) – wspólna wartość; pasek zdrowia reaguje na zdarzenie Changed.\n\n" +
                "W pliku zasobu zapisane są tylko dane konfiguracyjne (initialValue, maxValue). Bieżąca wartość i lista " +
                "subskrybentów są oznaczone <b>[NonSerialized]</b> / są zdarzeniem – nie trafiają do pliku.\n\n" +
                "Zaleta: lampy i drzwi można przenieść do innej sceny – wystarczy przypisać ten sam zasób.",
                "<b>Spacja</b> – alarm   <b>LPM</b> – obrażenia   <b>PPM</b> – leczenie", "", statusWidth: 560f);
            Floor();
            Cam(new Vector3(0, 4.5f, -9f), new Vector3(0, 1.4f, 0));
            var alarm = Load<GameEventChannel>("Alarm");
            var health = Load<FloatVariable>("ZdrowieGracza");
            var demo = Manager<ChannelsDemo>();
            demo.alarm = alarm;
            demo.playerHealth = health;

            for (int i = 0; i < 3; i++)
            {
                float x = -4f + i * 4f;
                Prim(PrimitiveType.Cylinder, $"Slup_lampy_{i + 1}", new Vector3(x, 1.5f, 2.5f), new Vector3(0.2f, 1.5f, 0.2f), M_Dark);
                var bulb = Ball($"Lampa_{i + 1}", new Vector3(x, 3.2f, 2.5f), 0.6f, M_Red);
                var lightGo = new GameObject("Swiatlo");
                lightGo.transform.SetParent(bulb.transform, false);
                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.25f, 0.2f);
                l.range = 8f;
                l.intensity = 0f;
                var lamp = bulb.AddComponent<AlarmLamp>();
                lamp.alarm = alarm;
                lamp.lampLight = l;
                Label(bulb, "AlarmLamp", 0.6f, 13);
            }
            Box("Mur_L", new Vector3(-2.2f, 1.5f, 5f), new Vector3(3f, 3f, 0.4f), M_Wall);
            Box("Mur_P", new Vector3(2.2f, 1.5f, 5f), new Vector3(3f, 3f, 0.4f), M_Wall);
            var door = Box("Drzwi", new Vector3(0, 1.25f, 5f), new Vector3(1.4f, 2.5f, 0.3f), M_Wood);
            door.AddComponent<AlarmDoor>().alarm = alarm;
            Label(door, "AlarmDoor", 1.5f, 13);

            var bar = new GameObject("Pasek_zdrowia");
            bar.transform.position = new Vector3(0, 0.6f, -1.5f);
            bar.transform.localScale = new Vector3(4f, 0.4f, 0.4f);
            var bg = Box("Tlo", bar.transform.position, new Vector3(4.1f, 0.42f, 0.38f), M_Dark, bar.transform);
            NoCollider(bg);
            var fill = Box("Wypelnienie", bar.transform.position + Vector3.back * 0.05f, new Vector3(4f, 0.4f, 0.4f), M_Green, bar.transform);
            NoCollider(fill);
            var view = bar.AddComponent<HealthBarView>();
            view.health = health;
            view.fill = fill.transform;
            Label(bar, "HealthBarView ← ZdrowieGracza.asset", 0.5f, 14);
        }

        // =====================================================================
        // 07 – INSTANTIATE
        // =====================================================================
        static void S07_Clone()
        {
            Begin("07 · Instantiate – kopiowanie przez serializację",
                "<b>Instantiate(obiekt)</b> działa jak zapis i natychmiastowy odczyt: Unity serializuje oryginał " +
                "(wraz z całą hierarchią dzieci i komponentami) i deserializuje dane do nowego obiektu.\n\n" +
                "Skutki:\n" +
                "• kopia dostaje wartości <b>tylko pól serializowanych</b>,\n" +
                "• pola private i [NonSerialized] mają w kopii wartość z inicjalizatora w kodzie,\n" +
                "• pole static jest jedno dla całej klasy,\n" +
                "• referencje do obiektów <b>wewnątrz</b> kopiowanej hierarchii są przemapowane na odpowiednie obiekty kopii, " +
                "a referencje do obiektów <b>spoza</b> hierarchii zostają bez zmian,\n" +
                "• w kopii wywołuje się Awake i OnEnable (Start – w następnej klatce).\n\n" +
                "Tak samo działa tworzenie obiektu z <b>prefabu</b>.",
                "<b>Spacja</b> – liczniki oryginału +1   <b>LPM</b> – Instantiate   <b>PPM</b> – usuń kopie", "", statusWidth: 560f);
            Floor();
            Cam(new Vector3(6.5f, 5.5f, -10.5f), new Vector3(6.5f, 1f, 0));
            var orig = Box("Oryginal", new Vector3(0, 0.75f, 0), new Vector3(1.5f, 1.5f, 1.5f), M_Blue);
            var probe = orig.AddComponent<CloneProbe>();
            var child = Ball("Dziecko", new Vector3(0, 1.9f, 0), 0.6f, M_Yellow, orig.transform);
            probe.internalChild = child.transform;
            var ext = Prim(PrimitiveType.Cylinder, "Obiekt_zewnetrzny", new Vector3(-3f, 1f, 2f), new Vector3(1f, 1f, 1f), M_Orange);
            probe.externalTarget = ext.transform;
            Label(ext, "Obiekt_zewnetrzny\n(externalTarget)", 1.2f, 13);
            var l = Label(orig, "ORYGINAŁ", 1.6f, 13);
            l.color = Color.white;
            var demo = Manager<CloneDemo>();
            demo.original = probe;
        }

        // =====================================================================
        // 08 – YAML
        // =====================================================================
        static void S08_Yaml()
        {
            // Zasoby tekstowe z treścią plików zapisanych przez edytor (Asset Serialization = Force Text)
            AssetDatabase.SaveAssets();
            string goblin = AssetPath("Goblin");
            string prefabPath = $"{DataDir}/Skrzynia.prefab";

            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tmp.name = "Skrzynia";
            var probe = tmp.AddComponent<CloneProbe>();
            probe.publicCounter = 7;
            var lid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lid.name = "Uchwyt";
            lid.transform.SetParent(tmp.transform, false);
            lid.transform.localPosition = new Vector3(0, 0.6f, 0);
            probe.internalChild = lid.transform;
            PrefabUtility.SaveAsPrefabAsset(tmp, prefabPath);
            Object.DestroyImmediate(tmp);
            AssetDatabase.SaveAssets();

            var files = new System.Collections.Generic.List<TextAsset>();
            var titles = new System.Collections.Generic.List<string>();
            var notes = new System.Collections.Generic.List<string>();
            AddYaml(files, titles, notes, goblin, "YAML_ScriptableObject", "Goblin.asset – zasób ScriptableObject (EnemyType)",
                "--- !u!114 &11400000 → obiekt klasy 114 (MonoBehaviour/ScriptableObject) o fileID 11400000. " +
                "m_Script wskazuje plik skryptu przez guid. Dalej – pola klasy pod swoimi nazwami.", 0);
            AddYaml(files, titles, notes, prefabPath, "YAML_Prefab", "Skrzynia.prefab – prefab",
                "Każdy obiekt i komponent to osobny dokument YAML: !u!1 GameObject, !u!4 Transform, !u!33 MeshFilter, " +
                "!u!23 MeshRenderer, !u!65 BoxCollider, !u!114 skrypt. Referencje = {fileID: …} w obrębie pliku.", 0);
            AddYaml(files, titles, notes, goblin + ".meta", "YAML_Meta", "Goblin.asset.meta – plik metadanych",
                "guid jednoznacznie identyfikuje zasób w projekcie. Referencje z innych plików używają pary guid + fileID – " +
                "dlatego pliki .meta MUSZĄ trafić do repozytorium.", 0);
            string sceneFile = ScenePath(Scenes[6].file);
            AddYaml(files, titles, notes, sceneFile, "YAML_Scena", Path.GetFileName(sceneFile) + " – plik sceny (fragment)",
                "Scena ma ten sam format co prefab. Zmienione pola komponentu CloneProbe zapisane są pod nazwami pól " +
                "(serializedCounter, publicCounter, internalChild: {fileID: …}).", 400);

            Begin("08 · Format plików Unity – YAML",
                "Edytor zapisuje sceny, prefaby, materiały i zasoby przez ten sam serializator. Przy ustawieniu " +
                "<b>Project Settings > Editor > Asset Serialization = Force Text</b> pliki są tekstowe (YAML) – " +
                "można je porównywać i scalać w systemie kontroli wersji.\n\n" +
                "Najważniejsze elementy:\n" +
                "• <b>--- !u!114 &11400000</b> – nagłówek obiektu: identyfikator klasy (114 = MonoBehaviour) i <b>fileID</b>,\n" +
                "• <b>m_Script: {fileID: 11500000, guid: …, type: 3}</b> – skrypt wskazany przez guid z pliku .meta,\n" +
                "• pola zapisane pod <b>nazwami pól w kodzie</b> – zmiana nazwy pola gubi dane, chyba że użyjesz " +
                "<b>[FormerlySerializedAs(\"staraNazwa\")]</b>,\n" +
                "• referencje do obiektów: {fileID: …} (ten sam plik) lub {fileID: …, guid: …} (inny zasób).\n\n" +
                "Teksty po prawej skopiował generator scen z plików w " + DataDir + ".",
                "<b>Spacja</b> – następny plik   <b>LPM</b> – następna strona   <b>PPM</b> – poprzednia strona",
                "", statusWidth: 900f, descriptionWidth: 520f);
            Floor();
            Cam(new Vector3(-2f, 2.5f, -6f), new Vector3(-2f, 1f, 0));
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            inst.transform.position = new Vector3(-2f, 0.5f, 0f);
            Label(inst, "instancja prefabu Skrzynia", 1.4f);
            var viewer = Manager<YamlViewer>();
            viewer.files = files.ToArray();
            viewer.titles = titles.ToArray();
            viewer.notes = notes.ToArray();
        }

        static void AddYaml(System.Collections.Generic.List<TextAsset> files, System.Collections.Generic.List<string> titles,
            System.Collections.Generic.List<string> notes, string source, string target, string title, string note, int maxLines)
        {
            string full = Path.GetFullPath(source);
            if (!File.Exists(full))
            {
                Debug.LogWarning("[Serializacja] Brak pliku do podglądu YAML: " + source + " (wygeneruj wszystkie sceny)");
                return;
            }
            string text = File.ReadAllText(full);
            if (maxLines > 0)
            {
                var lines = text.Split('\n');
                if (lines.Length > maxLines)
                    text = string.Join("\n", lines, 0, maxLines) + "\n… (dalsza część pliku pominięta)";
            }
            string path = $"{DataDir}/{target}.txt";
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path);
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (ta == null) return;
            files.Add(ta);
            titles.Add(title);
            notes.Add(note);
        }

        // =====================================================================
        // 09 – ZAPIS GRY JsonUtility
        // =====================================================================
        static void S09_SaveGame()
        {
            Begin("09 · Zapis stanu gry – JsonUtility + plik",
                "Zapis gry = serializacja <b>stanu</b>, a nie obiektów sceny:\n\n" +
                "1. Zbierz dane do zwykłej klasy [Serializable] (<b>SaveData</b>): pozycja, wynik, czas, " +
                "<b>identyfikatory</b> zebranych monet – nie referencje do GameObjectów.\n" +
                "2. <b>JsonUtility.ToJson(data, true)</b> → tekst.\n" +
                "3. <b>File.WriteAllText</b> w katalogu <b>Application.persistentDataPath</b> " +
                "(jedyne pewne miejsce zapisu na każdej platformie).\n\n" +
                "Odczyt: File.ReadAllText → <b>JsonUtility.FromJson<SaveData></b> → przywrócenie stanu sceny.\n\n" +
                "Pole <b>version</b> pozwala później rozpoznać stare zapisy (scena 11). " +
                "Gdzie trzymać ustawienia (głośność, język)? W PlayerPrefs – patrz wykład 5.",
                "<b>WASD</b> – ruch, zbieraj monety   <b>Spacja</b> – zapisz   <b>LPM</b> – wczytaj   <b>PPM</b> – usuń zapis",
                "", statusWidth: 620f);
            Floor();
            Cam(new Vector3(0, 13f, -10f), new Vector3(0, 0, 1f));
            var player = Prim(PrimitiveType.Capsule, "Gracz", new Vector3(0, 1f, -3f), Vector3.one, M_Blue);
            var nose = Box("Nos", new Vector3(0, 1.45f, -2.55f), new Vector3(0.5f, 0.18f, 0.3f), M_Dark, player.transform);
            NoCollider(nose);
            Label(player, "Gracz", 1.3f);
            var coins = new System.Collections.Generic.List<Coin>();
            var g = Group("Monety");
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                var c = Prim(PrimitiveType.Cylinder, $"Moneta_{i + 1:00}", new Vector3(Mathf.Cos(a) * 5f, 0.6f, Mathf.Sin(a) * 4f + 1f),
                    new Vector3(0.7f, 0.05f, 0.7f), M_Yellow, g, Quaternion.Euler(90f, 0, 0));
                NoCollider(c);
                var coin = c.AddComponent<Coin>();
                coin.id = $"moneta_{i + 1:00}";
                coin.value = 10;
                coins.Add(coin);
            }
            var demo = Manager<SaveGameDemo>();
            demo.player = player.transform;
            demo.coins = coins.ToArray();
        }

        // =====================================================================
        // 10 – OGRANICZENIA JsonUtility
        // =====================================================================
        static void S10_JsonLimits()
        {
            Begin("10 · Ograniczenia JsonUtility i ich obejścia",
                "<b>JsonUtility</b> (wbudowany w Unity) jest bardzo szybki i nie wymaga dodatkowych pakietów, " +
                "ale działa według reguł serializatora Unity – zapisze tylko to, co widzi Inspector.\n\n" +
                "Typowe problemy i rozwiązania:\n" +
                "• <b>Dictionary</b> – nie jest zapisywany → lista par lub ISerializationCallbackReceiver,\n" +
                "• <b>tablica jako korzeń</b> dokumentu → klasa opakowująca Wrapper<T>,\n" +
                "• <b>właściwości</b> – pomijane → [field: SerializeField] albo pola,\n" +
                "• <b>polimorfizm</b> – klasy pochodne tracą pola → [SerializeReference],\n" +
                "• <b>null</b> – zapisuje się jako pusty obiekt → jawna flaga.\n\n" +
                "Obsługuje za to typy Unity (Vector3, Color, Quaternion) i obiekty MonoBehaviour / ScriptableObject " +
                "(ToJson, FromJsonOverwrite). Pełny serializer .NET (np. Newtonsoft Json.NET) obsługuje słowniki i właściwości, " +
                "ale jest wolniejszy i nie zna typów Unity.",
                "<b>Spacja</b> – następny przypadek   <b>LPM</b> – odczyt (FromJson)", "", statusWidth: 700f);
            Floor();
            Cam(new Vector3(-1.5f, 2.5f, -6f), new Vector3(-1.5f, 1f, 0));
            var a = Box("Problem", new Vector3(-2.6f, 1f, 0), new Vector3(1.4f, 2f, 1.4f), M_Orange);
            Label(a, "problem", 1.3f);
            var b = Box("Rozwiazanie", new Vector3(-0.4f, 1f, 0), new Vector3(1.4f, 2f, 1.4f), M_Green);
            Label(b, "rozwiązanie", 1.3f);
            Manager<JsonLimitsDemo>();
        }

        // =====================================================================
        // 11 – BINARNIE I WERSJE
        // =====================================================================
        static void S11_Binary()
        {
            Begin("11 · Format binarny i wersjonowanie zapisu",
                "<b>BinaryWriter / BinaryReader</b> zapisują dane jako bajty w ustalonej kolejności: plik jest mniejszy " +
                "i szybszy do odczytu niż JSON, ale nieczytelny dla człowieka i wrażliwy na każdą zmianę formatu.\n\n" +
                "Dobra praktyka: nagłówek pliku – <b>magiczna liczba</b> (rozpoznanie typu pliku) i <b>numer wersji</b>. " +
                "Gra w wersji 2 musi umieć wczytać zapis z wersji 1 – brakujące pola dostają wartości domyślne (<b>migracja</b>).\n\n" +
                "Nie używaj <b>BinaryFormatter</b>: jest przestarzały i niebezpieczny (deserializacja danych z pliku może " +
                "uruchomić dowolny kod) – Microsoft wycofał go z .NET.\n\n" +
                "Ten sam problem wersji dotyczy JSON: dodaj pole version i obsłuż stare pliki.",
                "<b>Spacja</b> – zapisz v1 (stara gra)   <b>LPM</b> – zapisz v2   <b>PPM</b> – wczytaj z migracją", "", statusWidth: 660f);
            Floor();
            Cam(new Vector3(0, 3f, -7f), new Vector3(0, 1f, 0));
            var p = Prim(PrimitiveType.Capsule, "Gracz", new Vector3(0, 1f, 0), Vector3.one, M_Green);
            var nose = Box("Nos", new Vector3(0, 1.45f, 0.45f), new Vector3(0.5f, 0.18f, 0.3f), M_Dark, p.transform);
            NoCollider(nose);
            Label(p, "Ania – profil gracza", 1.3f);
            Manager<BinarySaveDemo>().player = p.transform;
        }

        // =====================================================================
        // 12 – BEZPIECZNY ZAPIS
        // =====================================================================
        static void S12_Secure()
        {
            Begin("12 · Sloty, zapis atomowy, kopia zapasowa, suma kontrolna",
                "Zapis może się nie udać (zamknięcie gry, brak prądu, pełny dysk) – uszkodzony plik oznacza utratę postępu.\n\n" +
                "• <b>Zapis atomowy</b>: zapisz do pliku .tmp, potem podmień (File.Replace) – stary plik trafia do <b>.bak</b>.\n" +
                "• <b>Kopia zapasowa</b>: gdy główny plik jest uszkodzony, wczytaj .bak.\n" +
                "• <b>Suma kontrolna</b> (SHA-256 z danych + „sól”): wykrywa uszkodzenie i ręczną edycję pliku. " +
                "Nie jest to zabezpieczenie przed zdeterminowanym graczem – kod gry można podejrzeć.\n" +
                "• <b>Sloty</b>: osobne pliki dla każdego zapisu; metadane (data, rozmiar) do wyświetlenia w menu.\n\n" +
                "Ważne dane (waluta premium, rankingi) przechowuj na <b>serwerze</b>, nie u gracza.",
                "<b>A / D</b> – slot   <b>Spacja</b> – zapisz   <b>LPM</b> – wczytaj   <b>PPM</b> – zmień złoto w pliku („oszustwo”)",
                "", statusWidth: 640f);
            Floor();
            Cam(new Vector3(0, 4f, -8f), new Vector3(0, 1f, 0));
            var markers = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var m = Box($"Slot_{i + 1}", new Vector3(-3.2f + i * 3.2f, 0.9f, 0), new Vector3(2.2f, 1.8f, 0.6f), M_Gray);
                Label(m, $"Slot {i + 1}", 1.2f);
                markers[i] = m.transform;
            }
            var demo = Manager<SecureSaveDemo>();
            demo.slotMarkers = markers;
            demo.activeMaterial = M_Green;
            demo.idleMaterial = M_Gray;
        }

        // =====================================================================
        // 13 – PlayerPrefs – USTAWIENIA
        // =====================================================================
        static void S13_PlayerPrefs()
        {
            Begin("13 · PlayerPrefs – ustawienia gracza",
                "<b>PlayerPrefs</b> – prosty magazyn par <b>klucz → wartość</b> trzymany przez Unity poza plikami gry " +
                "(Windows: rejestr, macOS: plik .plist, Android: SharedPreferences, WebGL: IndexedDB).\n\n" +
                "• tylko trzy typy: <b>SetInt / SetFloat / SetString</b> – bool, enum i kolor trzeba zamienić (1/0, numer, \"#RRGGBB\"),\n" +
                "• <b>Get…(klucz, wartośćDomyślna)</b> – brak klucza nie jest błędem,\n" +
                "• <b>HasKey</b>, <b>DeleteKey</b>, <b>DeleteAll</b>,\n" +
                "• <b>Save()</b> – zapis na dysk teraz; bez niego Unity zapisuje przy zamykaniu aplikacji " +
                "(awaria gry = utrata zmian).\n\n" +
                "Licznik wejść do sceny rośnie przy każdym jej wczytaniu – przytrzymaj E albo uruchom grę ponownie: " +
                "wartości zostają. Klucze mają prefiks sceny (s13.) i są stałymi w kodzie.\n\n" +
                "Nie trzymaj tu zapisu gry ani danych ważnych: PlayerPrefs nie są szyfrowane.",
                "<b>A / D</b> – ustawienie   <b>W / S</b> – wartość   <b>Spacja</b> – Save()   <b>LPM</b> – wczytaj   <b>PPM</b> – usuń klucze",
                "", statusWidth: 660f);
            Floor();
            Cam(new Vector3(0f, 3.4f, -8f), new Vector3(0f, 1f, 0.5f));
            var light = Object.FindFirstObjectByType<Light>();
            var hero = Prim(PrimitiveType.Capsule, "Postac", new Vector3(0f, 1f, 0f), Vector3.one, M_White);
            var nameLabel = Label(hero, "Gracz", 1.3f, 18);
            var speaker = Prim(PrimitiveType.Cylinder, "Glosnik", new Vector3(-2.5f, 0.4f, 0.5f), new Vector3(1.4f, 0.4f, 1.4f), M_Dark);
            Label(speaker, "głośność", 0.9f, 13);
            var hints = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < 2; i++)
            {
                var h = Box("Podpowiedz_" + (i + 1), new Vector3(i == 0 ? -1.6f : 1.6f, 2.4f, 1f), new Vector3(1.1f, 0.5f, 0.05f), M_Yellow);
                Label(h, i == 0 ? "podpowiedź: W/S" : "podpowiedź: A/D", 0.4f, 12);
                hints.Add(h);
            }
            var enemies = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                var e = Ball("Przeciwnik_" + (i + 1), new Vector3(2.2f + (i % 3) * 0.9f, 0.35f, 1.2f + (i / 3) * 1f), 0.7f, M_Red);
                enemies.Add(e);
            }
            SectionLabel("przeciwnicy = trudność", new Vector3(3.1f, 0.05f, 0.4f), null, 13);
            var demo = Manager<PlayerPrefsSettingsDemo>();
            demo.character = hero.GetComponent<Renderer>();
            demo.sceneLight = light;
            demo.speaker = speaker.transform;
            demo.hints = hints.ToArray();
            demo.enemies = enemies.ToArray();
            demo.nameLabel = nameLabel;
        }

        // =====================================================================
        // 14 – PlayerPrefs – OBIEKT JSON I MIGRACJA
        // =====================================================================
        static void S14_HighScores()
        {
            Begin("14 · PlayerPrefs – tabela rekordów jako JSON",
                "Obiekt złożony (lista rekordów) można trzymać w <b>jednym kluczu</b> PlayerPrefs jako tekst JSON:\n" +
                "<b>SetString(klucz, JsonUtility.ToJson(tabela))</b> i <b>JsonUtility.FromJson<T>(GetString(klucz))</b>.\n\n" +
                "• pole <b>version</b> w obiekcie pozwala rozpoznać stary format,\n" +
                "• <b>migracja</b>: LPM zapisuje tabelę tak, jak robiła to „stara wersja gry” – w osobnych kluczach " +
                "(s14.v1.nick0, s14.v1.score0…). Przy odczycie dane są przenoszone do nowego formatu, a stare klucze usuwane,\n" +
                "• uszkodzony JSON → wartości domyślne zamiast wyjątku.\n\n" +
                "Kiedy PlayerPrefs, a kiedy plik (sceny 09–12)? PlayerPrefs – małe dane: ustawienia, rekordy lokalne, " +
                "flagi („samouczek ukończony”). Plik – zapis stanu gry, duże dane, sloty, kopie zapasowe. " +
                "W WebGL PlayerPrefs mają limit 1 MB.",
                "<b>Spacja</b> – zagraj rundę   <b>LPM</b> – dane w starym formacie v1 + migracja   <b>PPM</b> – wyczyść tabelę",
                "", statusWidth: 660f);
            Floor();
            Cam(new Vector3(0f, 3.6f, -9f), new Vector3(0f, 1.5f, 0f));
            var podium = Group("Tabela");
            var bars = new Transform[5];
            var labels = new WorldLabel[5];
            for (int i = 0; i < 5; i++)
            {
                var b = Box("Wynik_" + (i + 1), new Vector3(-3.2f + i * 1.6f, 0.05f, 0f), new Vector3(1.1f, 0.1f, 1.1f), Palette[i], podium);
                bars[i] = b.transform;
                labels[i] = Label(b, (i + 1) + ".", 0.3f, 15);
            }
            var demo = Manager<HighScoresDemo>();
            demo.bars = bars;
            demo.barLabels = labels;
        }
    }
}
