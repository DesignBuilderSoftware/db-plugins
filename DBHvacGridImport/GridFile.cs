using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DBHvacGridImport
{
    /// <summary>
    /// A CSV file exported from the DesignBuilder model data grid (HVAC tables).
    ///
    /// File layout:
    ///   Row 1   culture of the export (e.g. en-US) - sets the decimal separator
    ///   Row 2   "DesignBuilder - Model data grid view", format version, date
    ///   Row 3   model identifiers, model name, unit system of the export (e.g. usIP)
    ///   Row 4   attribute names; column 2 holds "HVAC" + separator + table name
    ///   Row 5   column captions, with the display unit in brackets - this is what
    ///           decides each column's units, so a file in either unit system works
    ///   Row 6+  one row per HVAC component
    ///
    /// The file may have been re-saved from Excel: a UTF-8 byte order mark and a
    /// semicolon or tab separator are both accepted.
    /// </summary>
    public class GridFile
    {
        // Columns that locate a component in the HVAC network.
        public const string ColSubLoopSubComponent = "HVACSubLoopSubComponentTitle";
        public const string ColSubLoopComponent = "HVACSubLoopComponentTitle";
        public const string ColLoop = "HVACLoopTitle";
        public const string ColZoneSubComponent = "HVACZoneSubComponentTitle";
        public const string ColZoneComponent = "HVACZoneComponentTitle";
        public const string ColZone = "HVACZoneTitle";

        private const int HeaderRows = 5;

        private readonly Dictionary<string, int> mColumnIndex =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public string FilePath { get; private set; }
        public string TableName { get; private set; }
        public CultureInfo Culture { get; private set; }
        public List<string> Captions { get; private set; }
        public List<string[]> Rows { get; private set; }

        private GridFile() { }

        /// <summary>Reads and validates an export. Throws FormatException with a
        /// user-readable message if the file is not an HVAC model data grid export.</summary>
        public static GridFile Load(string path)
        {
            List<string> lines = ReadLines(path);
            if (lines.Count < HeaderRows)
            {
                throw new FormatException("The file has fewer than " +
                    HeaderRows.ToString(CultureInfo.InvariantCulture) + " rows.");
            }

            char separator = DetectSeparator(lines[3]);
            string[] intro = SplitLine(lines[1], separator);
            if (intro.Length == 0 || intro[0].IndexOf("data grid", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new FormatException("This is not a DesignBuilder model data grid export " +
                    "(row 2 should start with 'DesignBuilder - Model data grid view').");
            }

            string[] attributes = SplitLine(lines[3], separator);
            const string hvacPrefix = "HVAC";
            string tableCell = attributes.Length > 1 ? attributes[1].Trim() : "";
            if (!tableCell.StartsWith(hvacPrefix, StringComparison.OrdinalIgnoreCase) ||
                tableCell.Length <= hvacPrefix.Length + 1)
            {
                throw new FormatException("This export is not an HVAC component table " +
                    "(row 4, column 2 reads '" + tableCell + "').");
            }

            var file = new GridFile
            {
                FilePath = path,
                // Skip "HVAC" and the one-character separator that follows it.
                TableName = tableCell.Substring(hvacPrefix.Length + 1),
                Culture = ReadCulture(SplitLine(lines[0], separator)),
                Captions = new List<string>(SplitLine(lines[4], separator)),
                Rows = new List<string[]>()
            };

            for (int i = 0; i < attributes.Length; i++)
            {
                string name = attributes[i].Trim();
                if (name.Length > 0 && !file.mColumnIndex.ContainsKey(name))
                {
                    file.mColumnIndex.Add(name, i);
                }
            }

            for (int i = HeaderRows; i < lines.Count; i++)
            {
                string[] cells = SplitLine(lines[i], separator);
                if (!IsBlank(cells))
                {
                    file.Rows.Add(cells);
                }
            }
            return file;
        }

        public bool HasColumn(string attribute)
        {
            return mColumnIndex.ContainsKey(attribute);
        }

        /// <summary>Cell text, trimmed; empty if the column or cell is missing.</summary>
        public string Cell(string[] row, string attribute)
        {
            int index;
            if (!mColumnIndex.TryGetValue(attribute, out index) || index >= row.Length)
            {
                return "";
            }
            return row[index].Trim();
        }

        /// <summary>Display unit of a column, from the brackets at the end of its
        /// caption, e.g. "Rated total cooling capacity (Btu/h)" gives "Btu/h".</summary>
        public string CaptionUnit(string attribute)
        {
            int index;
            if (!mColumnIndex.TryGetValue(attribute, out index) || index >= Captions.Count)
            {
                return "";
            }
            string caption = Captions[index];
            int open = caption.LastIndexOf('(');
            int close = caption.LastIndexOf(')');
            return (open >= 0 && close > open) ? caption.Substring(open + 1, close - open - 1).Trim() : "";
        }

        /// <summary>
        /// Parses a number written in the export's culture, falling back to the
        /// invariant culture (a file edited on a machine with different regional
        /// settings). decimals is the number of decimal places written, or -1 for
        /// exponent notation.
        /// </summary>
        public bool TryParseNumber(string text, out double value, out int decimals)
        {
            decimals = -1;
            if (!double.TryParse(text, NumberStyles.Float, Culture, out value) &&
                !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }
            if (text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0)
            {
                return true;
            }
            int point = text.LastIndexOf(Culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
            if (point < 0)
            {
                point = text.LastIndexOf('.');
            }
            decimals = point < 0 ? 0 : text.Length - point - 1;
            return true;
        }

        // ------------------------------------------------------------------
        private static List<string> ReadLines(string path)
        {
            // FileShare.ReadWrite lets the file be read while it is still open in Excel.
            // DesignBuilder writes the export in the Windows ANSI code page; a byte
            // order mark (Excel "CSV UTF-8") overrides that.
            var lines = new List<string>();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.Default, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }

        private static CultureInfo ReadCulture(string[] firstRow)
        {
            try
            {
                string name = firstRow.Length > 0 ? firstRow[0].Trim() : "";
                return name.Length > 0 ? CultureInfo.GetCultureInfo(name) : CultureInfo.InvariantCulture;
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        /// <summary>The attribute-name row contains no separators of its own, so the
        /// most frequent candidate character is the separator.</summary>
        private static char DetectSeparator(string attributeRow)
        {
            char best = ',';
            int bestCount = -1;
            foreach (char candidate in new[] { ',', ';', '\t' })
            {
                int count = 0;
                foreach (char c in attributeRow)
                {
                    if (c == candidate) count++;
                }
                if (count > bestCount)
                {
                    best = candidate;
                    bestCount = count;
                }
            }
            return best;
        }

        /// <summary>Splits one CSV line, honouring double quotes ("" inside quotes is a quote).</summary>
        private static string[] SplitLine(string line, char separator)
        {
            var cells = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == separator)
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                }
                else
                {
                    cell.Append(c);
                }
            }
            cells.Add(cell.ToString());
            return cells.ToArray();
        }

        private static bool IsBlank(string[] cells)
        {
            foreach (string cell in cells)
            {
                if (cell.Trim().Length > 0) return false;
            }
            return true;
        }
    }
}
