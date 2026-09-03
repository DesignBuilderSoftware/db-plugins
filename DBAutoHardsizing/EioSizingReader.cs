using System;
using System.Collections.Generic;
using System.IO;

namespace DBAutoHardsizeModel
{
    /// <summary>
    /// Reads "Component Sizing Information" rows from an EnergyPlus .eio file.
    ///
    /// Verified against the client's eplusout.eio:
    ///  - Data rows are indented one space; the header row starts with '!'.
    ///  - Component names are UPPERCASE in the .eio, mixed case in DesignBuilder,
    ///    so all lookups are case-insensitive.
    ///  - Keyed on (type, name): EnergyPlus permits the same name on different
    ///    object types.
    ///  - The "Design Size " prefix is NOT universal. Controller and AirLoopHVAC
    ///    rows have no prefix, so descriptions are matched verbatim rather than
    ///    by stripping a prefix.
    /// </summary>
    public class EioSizingReader
    {
        private const string RowPrefix = "Component Sizing Information";

        private readonly Dictionary<string, string> _values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>All (type, name) pairs present, for reporting what exists.</summary>
        private readonly HashSet<string> _objects =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int RowCount { get; private set; }
        public string SourcePath { get; private set; }

        public EioSizingReader(string eioPath)
        {
            SourcePath = eioPath;

            foreach (string raw in File.ReadLines(eioPath))
            {
                string line = raw.Trim();
                if (!line.StartsWith(RowPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                string[] parts = line.Split(new[] { ',' }, 5);
                if (parts.Length < 5) continue;

                string type = parts[1].Trim();
                string name = parts[2].Trim();
                string desc = parts[3].Trim();
                string value = parts[4].Trim();

                _values[Key(type, name, desc)] = value;
                _objects.Add(type + "\u0001" + name);
                RowCount++;
            }
        }

        private static string Key(string type, string name, string desc)
        {
            return type + "\u0001" + name + "\u0001" + desc;
        }

        /// <summary>
        /// Looks the value up across several candidate EnergyPlus types, returning
        /// the first hit. This is how heating coils resolve water vs electric and
        /// how fans resolve OnOff vs ConstantVolume - the .eio decides, we don't.
        /// </summary>
        public bool TryGetValue(IEnumerable<string> eioTypes, string componentName,
                                string description, out string value, out string matchedType)
        {
            value = null;
            matchedType = null;
            if (string.IsNullOrEmpty(componentName)) return false;

            foreach (string type in eioTypes)
            {
                if (_values.TryGetValue(Key(type, componentName, description), out value))
                {
                    matchedType = type;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Whether any sizing row exists for this object at all.</summary>
        public bool HasObject(IEnumerable<string> eioTypes, string componentName)
        {
            if (string.IsNullOrEmpty(componentName)) return false;
            foreach (string type in eioTypes)
                if (_objects.Contains(type + "\u0001" + componentName)) return true;
            return false;
        }

        /// <summary>
        /// Finds the .eio next to the generated IDF. DesignBuilder writes
        /// eplusout.eio into the EnergyPlus output folder.
        /// </summary>
        public static string ResolveEioPath(string energyPlusInputIdfPath)
        {
            if (string.IsNullOrEmpty(energyPlusInputIdfPath)) return null;

            string dir = Path.GetDirectoryName(energyPlusInputIdfPath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;

            string standard = Path.Combine(dir, "eplusout.eio");
            if (File.Exists(standard)) return standard;

            string[] candidates = Directory.GetFiles(dir, "*.eio");
            if (candidates.Length == 0) return null;

            string newest = candidates[0];
            DateTime newestTime = File.GetLastWriteTimeUtc(newest);
            for (int i = 1; i < candidates.Length; i++)
            {
                DateTime t = File.GetLastWriteTimeUtc(candidates[i]);
                if (t > newestTime) { newest = candidates[i]; newestTime = t; }
            }
            return newest;
        }
    }
}
