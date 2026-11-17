# Serializacja w Unity – sceny demonstracyjne

Projekt Unity **6.3 LTS (6000.3.24f1)** do wykładu 6 „Serializacja i animacja w Unity”. Pokazuje, **co i jak zapisuje serializator Unity** (reguły pól, klasy `[Serializable]`, `[SerializeReference]`, `ISerializationCallbackReceiver`), **ScriptableObject** (dane i kanały zdarzeń), **Instantiate** jako kopię przez serializację, **format YAML** plików projektu oraz **zapis stanu gry** (JsonUtility i jego ograniczenia, format binarny, wersjonowanie, sloty, zapis atomowy, suma kontrolna) oraz **PlayerPrefs** (ustawienia, obiekt JSON w kluczu, migracja formatu).

Każde zagadnienie ma **osobną scenę** (`01_…` – `14_…`). W każdej scenie jest panel z opisem po polsku (lewy górny róg), panel stanu z wartościami na żywo i dziennikiem zdarzeń (prawy górny róg) oraz pasek ze sterowaniem (dół ekranu).

- Render pipeline: **URP** (ustawienia z szablonu „Universal 3D”)
- Pakiety: **Input System**, **uGUI** – bez dodatkowych pakietów
- Wejście: wyłącznie akcje z domyślnego pliku `Assets/InputSystem_Actions.inputactions` (Project-wide Actions)
- Generator ustawia **Asset Serialization = Force Text** – pliki scen i zasobów są czytelnym YAML-em (scena 08)

---

## 1. Szybki start

1. Otwórz projekt w Unity Hub (wersja **6000.3.24f1**) albo uruchom `Narzedzia/Otworz_projekt_w_Unity.bat`. Przy pierwszym otwarciu Unity pobierze pakiety.
2. Sceny są **generowane kodem**. Przy pierwszym otwarciu pojawi się pytanie „Wygenerować teraz 14 scen?” – wybierz **Generuj**. Można to zrobić także później: menu **Serializacja → Generuj wszystkie sceny**.
3. **Serializacja → Otwórz pierwszą scenę i uruchom Play**.
4. Klawiszami **1 / 2** przechodzisz do poprzedniej / następnej sceny.

> Jeżeli klawisze nie działają – **Serializacja → Napraw ustawienia projektu** i uruchom ponownie edytor.
> W oknie **Game** ustaw proporcje 16:9 (np. *Full HD 1920×1080*).

### Build Windows (opcjonalnie)
`Narzedzia/Generuj_sceny_i_zbuduj_Windows.bat` (edytor z tym projektem musi być zamknięty) – generuje sceny i buduje `Builds/Windows/SerializacjaDemo.exe`. Log: `Logs/batch_build.log`.

---

## 2. Menu „Serializacja” w edytorze

| Polecenie | Działanie |
|---|---|
| **Generuj wszystkie sceny** | Tworzy zasoby (EnemyType, Alarm, ZdrowieGracza, prefab, pliki YAML) i 14 scen, ustawia Build Settings |
| **Panel scen** | Okno z listą scen: *Otwórz*, ▶ (otwórz i Play), ↻ (wygeneruj tylko tę scenę) |
| **Otwórz pierwszą scenę i uruchom Play** | Skrót do pokazu |
| **Zbuduj wersję Windows (x64)** | Build do `Builds/Windows/SerializacjaDemo.exe` |
| **Napraw ustawienia projektu** | Active Input Handling = Input System, Project-wide Actions, Force Text, Build Settings |
| **Otwórz instrukcję (README.md)** | Otwiera ten plik |

**Uwaga:** ponowne generowanie nadpisuje ręczne zmiany w scenach.

---

## 3. Sterowanie

| Klawisz / mysz | Akcja z `InputSystem_Actions` | Działanie |
|---|---|---|
| **Spacja** | Player/Jump | akcja główna sceny |
| **LPM** | Player/Attack | akcja dodatkowa |
| **PPM** | UI/RightClick | trzecia akcja |
| **WASD / strzałki** | Player/Move | ruch gracza (scena 09), wybór slotu A/D (scena 12), wybór i zmiana ustawień (scena 13) |
| **C** | Player/Crouch | pokaż / ukryj panel opisu |
| **przytrzymaj E** | Player/Interact (Hold) | reset sceny |
| **1 / 2** | Player/Previous / Player/Next | poprzednia / następna scena |
| **Esc** | UI/Cancel | wyjście (w buildzie) |

---

## 4. Opis scen

| Scena | Co pokazuje | Spacja | LPM | PPM |
|---|---|---|---|---|
| **01_Reguly_serializacji** | które pola są serializowane: public, `[SerializeField]`, `[NonSerialized]`, static, const, readonly, właściwości, `[field: SerializeField]`, Dictionary, tablice wielowymiarowe, listy zagnieżdżone, interfejsy; tabela TAK/NIE liczona z `JsonUtility.ToJson` | tabela ↔ JSON | zmień wszystkie pola | przywróć z JSON |
| **02_Klasy_Serializable** | klasy i struktury `[Serializable]` zapisywane po wartości: brak polimorfizmu, brak null, brak współdzielenia, klasa bez atrybutu | następny przypadek | ToJson → FromJson | – |
| **03_SerializeReference** | `[SerializeReference]`: polimorfizm (Damage/Heal/ColorEffect), null w liście, współdzielona referencja, blok `references` w JSON | lista po wartości → cel A | lista po referencji → cel B | Instantiate i test referencji |
| **04_Callbacki_Slownik** | `ISerializationCallbackReceiver` – słownik zapisany jako dwie listy; liczniki wywołań; błędne dane (duplikat klucza) | dodaj towar | ToJson → FromJson | JSON z duplikatem |
| **05_ScriptableObject_Dane** | zasoby `EnemyType` współdzielone przez 9 przeciwników; zmiana zasobu; `Instantiate(zasób)`; pułapka zmian w trybie Play | zmień zasób Goblin | kopia dla jednego goblina | przywróć |
| **06_ScriptableObject_Kanaly** | kanał zdarzeń `GameEventChannel` i zmienna `FloatVariable` – systemy bez wzajemnych referencji | alarm | obrażenia | leczenie |
| **07_Instantiate_Klonowanie** | Instantiate = serializacja + deserializacja: które liczniki trafiają do kopii, przemapowanie referencji wewnątrz hierarchii | liczniki +1 | Instantiate | usuń kopie |
| **08_YAML_Format_plikow** | podgląd plików `.asset`, `.prefab`, `.meta`, `.unity` w formacie tekstowym: `--- !u!114 &…`, fileID, guid, m_Script | następny plik | następna strona | poprzednia strona |
| **09_JsonUtility_ZapisGry** | zapis stanu gry (pozycja, wynik, zebrane monety po id) do `Application.persistentDataPath` | zapisz | wczytaj | usuń zapis |
| **10_JsonUtility_ograniczenia** | ograniczenia JsonUtility i obejścia: słownik → lista par, tablica jako korzeń → `Wrapper<T>`, właściwości → `[field: SerializeField]`, polimorfizm → `[SerializeReference]`, null → flaga | następny przypadek | odczyt (FromJson) | – |
| **11_Zapis_binarny_Wersje** | `BinaryWriter`/`BinaryReader`, magiczna liczba, numer wersji, migracja v1 → v2, zrzut szesnastkowy | zapisz v1 | zapisz v2 | wczytaj z migracją |
| **12_Bezpieczny_zapis_Sloty** | 3 sloty, zapis atomowy (.tmp + File.Replace), kopia .bak, suma SHA-256, wykrywanie edycji pliku | zapisz | wczytaj | „oszukaj” (zmień złoto) |
| **13_PlayerPrefs_Ustawienia** | ustawienia w PlayerPrefs: SetInt/SetFloat/SetString, bool/enum/kolor zamienione na int i tekst, wartości domyślne, HasKey, DeleteKey, Save, licznik wejść do sceny (trwałość po resecie), miejsce zapisu na platformie; A/D – wybór, W/S – zmiana | Save() | wczytaj | usuń klucze |
| **14_PlayerPrefs_Rekordy_JSON** | tabela rekordów jako JSON w jednym kluczu, pole version, migracja ze starego formatu (osobne klucze) | zagraj rundę | stary format v1 + migracja | wyczyść tabelę |

Klucze PlayerPrefs scen 13–14 (prefiksy `s13.`, `s14.`) w Windows: rejestr `HKEY_CURRENT_USER\Software\PGWSU\Serializacja_Demo` (w edytorze: `…\Software\Unity\UnityEditor\PGWSU\Serializacja_Demo`).
Pliki zapisu scen 09, 11 i 12 trafiają do `Application.persistentDataPath` (w Windows: `%USERPROFILE%\AppData\LocalLow\PGWSU\Serializacja_Demo`).

---

## 5. Struktura projektu

```
Assets/
  InputSystem_Actions.inputactions   domyślne akcje (Project-wide Actions)
  Settings/                          ustawienia URP
  Serializacja/
    Scripts/Core/   DemoHud, DemoInput, DemoActionBase, WorldLabel, FreeCamera
    Scripts/Demo/   skrypty scen 01–14 (SerializationRulesDemo, SerializableClassesDemo, SerializeReferenceDemo,
                    SerializableDictionary, DictionaryDemo, EnemyType, Enemy, GameEventChannel, FloatVariable,
                    CloneProbe, YamlViewer, SaveData, SaveGameDemo, JsonLimitsDemo, BinarySaveDemo, SecureSaveDemo,
                    PrefsUtil, PlayerPrefsSettingsDemo, HighScoresDemo …)
    Editor/         generator scen (SceneBuilder, SceneDefinitions), panel scen, pierwsze uruchomienie
    Scenes/ Data/ Materials/ Textures/   (tworzone przez generator)
Narzedzia/          skrypty .bat: otwarcie projektu, generowanie scen, build Windows
```
