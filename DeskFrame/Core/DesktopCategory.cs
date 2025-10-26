using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DeskFrame.Core
{
    /// <summary>
    /// Repräsentiert eine Desktop-Kategorie (Dateiendungen + optional Regex) für automatische Instanz-Erstellung.
    /// </summary>
    public class DesktopCategory
    {
        public string Name { get; set; } = string.Empty;              // Anzeigename / Instanzsuffix
        public List<string> Extensions { get; set; } = new();          // Ohne Punkt, z.B. "exe"
        public bool Enabled { get; set; } = true;                      // Ob Instanz erzeugt werden soll
        public int Order { get; set; } = 0;                            // Reihenfolge für Positionierung
        public string? Regex { get; set; }                             // Optional eigener Regex statt Extensions-Liste
        public bool CatchAll { get; set; } = false;                    // Kategorie für verbleibende Dateien
    }

    /// <summary>
    /// Lädt / speichert Kategorien als JSON unter %AppData%\DeskFrame\categories.json
    /// </summary>
    public static class DesktopCategoryManager
    {
        private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskFrame");
        private static readonly string CategoryFile = Path.Combine(AppDataPath, "categories.json");
        private static List<DesktopCategory>? _cache;

        public static IReadOnlyList<DesktopCategory> LoadCategories()
        {
            if (_cache != null) return _cache;
            try
            {
                if (!Directory.Exists(AppDataPath)) Directory.CreateDirectory(AppDataPath);
                if (!File.Exists(CategoryFile))
                {
                    var defaults = CreateDefaultCategories();
                    SaveCategories(defaults);
                    _cache = defaults;
                    return _cache;
                }
                var json = File.ReadAllText(CategoryFile);
                var cats = JsonSerializer.Deserialize<List<DesktopCategory>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DesktopCategory>();
                // Fallback: falls leer -> Default
                if (cats.Count == 0)
                {
                    cats = CreateDefaultCategories();
                    SaveCategories(cats);
                }
                _cache = cats.OrderBy(c => c.Order).ToList();
                return _cache;
            }
            catch
            {
                var defaults = CreateDefaultCategories();
                _cache = defaults;
                return _cache;
            }
        }

        public static void SaveCategories(IEnumerable<DesktopCategory> categories)
        {
            try
            {
                if (!Directory.Exists(AppDataPath)) Directory.CreateDirectory(AppDataPath);
                var list = categories.ToList();
                // Normalisiere Endungen (lowercase, ohne führenden Punkt)
                foreach (var c in list)
                {
                    c.Extensions = c.Extensions
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Select(e => e.Trim().TrimStart('.').ToLowerInvariant())
                        .Distinct()
                        .ToList();
                }
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CategoryFile, json);
                _cache = list.OrderBy(c => c.Order).ToList();
            }
            catch
            {
                // Ignorieren – im Fehlerfall bleiben alte Kategorien im Speicher.
            }
        }

        private static List<DesktopCategory> CreateDefaultCategories() => new()
        {
            new DesktopCategory { Name = "Anwendungen", Order = 0, Extensions = new(){"exe","lnk","url","msi"} },
            new DesktopCategory { Name = "Dokumente", Order = 1, Extensions = new(){"pdf","docx","xlsx","pptx","txt","md"} },
            new DesktopCategory { Name = "Bilder", Order = 2, Extensions = new(){"png","jpg","jpeg","gif","svg","webp"} },
            new DesktopCategory { Name = "Archive", Order = 3, Extensions = new(){"zip","rar","7z","tar","gz"} },
            new DesktopCategory { Name = "Skripte", Order = 4, Extensions = new(){"ps1","bat","cmd","sh","py","js","ts"} },
            new DesktopCategory { Name = "Sonstiges", Order = 5, CatchAll = true, Enabled = true }
        };

        /// <summary>
        /// Baut aus einer Liste von Extensions eine Regex (Case-insensitive). Beispiel: (?i)^.*\.(exe|lnk)$
        /// </summary>
        public static string BuildExtensionRegex(IEnumerable<string> extensions)
        {
            var extList = extensions.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim().TrimStart('.').ToLowerInvariant()).Distinct().ToList();
            if (extList.Count == 0) return string.Empty;
            var joined = string.Join("|", extList.Select(Regex.Escape));
            return $"(?i)^.*\\.(?:{joined})$";
        }
    }
}
