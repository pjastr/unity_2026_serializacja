using System.Text;
using UnityEngine;

namespace Serializacja
{
    /// <summary>
    /// Scena 08: podgląd plików zapisanych przez edytor w formacie tekstowym (YAML).
    /// Generator scen skopiował treść plików .asset, .prefab, .unity i .meta do zasobów tekstowych (TextAsset).
    /// Spacja – następny plik, LPM – następna strona, PPM – poprzednia strona.
    /// </summary>
    public class YamlViewer : DemoActionBase
    {
        public TextAsset[] files;
        public string[] titles;
        [TextArea(2, 6)] public string[] notes;
        public int linesPerPage = 34;

        int m_File;
        int m_Page;

        void Start() => Show();

        public override void OnMainAction()
        {
            if (files == null || files.Length == 0) return;
            m_File = (m_File + 1) % files.Length;
            m_Page = 0;
            Show();
        }

        public override void OnSecondaryAction()
        {
            m_Page++;
            Show();
        }

        public override void OnTertiaryAction()
        {
            m_Page = Mathf.Max(0, m_Page - 1);
            Show();
        }

        void Show()
        {
            if (files == null || files.Length == 0 || files[m_File] == null)
            {
                DemoHud.SetStatus("yaml", "Brak plików – wygeneruj sceny ponownie (menu Serializacja).");
                return;
            }
            var lines = files[m_File].text.Replace("\r", "").Split('\n');
            int pages = Mathf.Max(1, (lines.Length + linesPerPage - 1) / linesPerPage);
            if (m_Page >= pages) m_Page = 0;
            var sb = new StringBuilder();
            sb.Append("<b>").Append(titles != null && m_File < titles.Length ? titles[m_File] : files[m_File].name).Append("</b>")
              .Append("   strona ").Append(m_Page + 1).Append('/').Append(pages).Append('\n');
            if (notes != null && m_File < notes.Length && !string.IsNullOrEmpty(notes[m_File]))
                sb.Append("<color=#9fb4cc>").Append(notes[m_File]).Append("</color>\n");
            sb.Append('\n');
            for (int i = m_Page * linesPerPage; i < Mathf.Min(lines.Length, (m_Page + 1) * linesPerPage); i++)
                sb.Append(Highlight(lines[i])).Append('\n');
            DemoHud.SetStatus("yaml", sb.ToString());
        }

        static string Highlight(string line)
        {
            string l = line.Replace("<", "‹").Replace(">", "›");
            if (l.Length > 110) l = l.Substring(0, 110) + "…";
            if (l.StartsWith("--- !u!")) return "<color=#FFD966><b>" + l + "</b></color>";
            if (l.StartsWith("%")) return "<color=#9fb4cc>" + l + "</color>";
            if (l.Contains("guid:")) return "<color=#9fd3ff>" + l + "</color>";
            if (l.Contains("m_Script") || l.Contains("m_Name")) return "<color=#7CFC7C>" + l + "</color>";
            return l;
        }
    }
}
