using System.Text.RegularExpressions;

namespace LocalizationMerger;

public static class Validator
{
    public static void Run()
    {
        string current = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(current, ".git")) && Directory.GetParent(current) != null)
        {
            current = Directory.GetParent(current)!.FullName;
        }
        string baseDir = current;
        string webAppDir = Path.Combine(baseDir, "MyFathersWorkWebApp", "MyFathersWorkWebApp");
        string locDir = Path.Combine(webAppDir, "wwwroot", "localization");
        string setupImgDir = Path.Combine(webAppDir, "wwwroot", "images", "setup");
        string gameImgDir = Path.Combine(webAppDir, "wwwroot", "images", "gameplay");

        Console.WriteLine("=== VALIDATOR STARTING ===");

        // 1. Check PopUpIcon constants against images
        Console.WriteLine("\n--- Checking PopUpIcon constants ---");
        string popupIconFile = Path.Combine(webAppDir, "Shared", "Consts", "PopUpIcon.cs");
        if (File.Exists(popupIconFile))
        {
            var lines = File.ReadAllLines(popupIconFile);
            var iconRegex = new Regex(@"public const string (\w+)\s*=\s*""([^""]+)""");
            foreach (var line in lines)
            {
                var match = iconRegex.Match(line);
                if (match.Success)
                {
                    string iconName = match.Groups[1].Value;
                    string iconValue = match.Groups[2].Value;
                    string setupPath = Path.Combine(setupImgDir, iconValue + ".png");
                    string gamePath = Path.Combine(gameImgDir, iconValue + ".png");
                    if (!File.Exists(setupPath) && !File.Exists(gamePath))
                    {
                        Console.WriteLine($"[MISSING IMAGE] PopUpIcon {iconName} = \"{iconValue}\" has no png in setup or gameplay images!");
                    }
                }
            }
        }

        // 2. Load all CSV tags
        var scenarios = new[]
        {
            ("TheCostOfDisease", "TheCostOfDisease", "TheCostOfDisease_Localization.csv", "TheCostOfDisease_Gameplay_Localization.csv"),
            ("FearOfTheUnknown", "FearOfTheUnknown", "FearOfTheUnknown_Localization.csv", "FearOfTheUnknown_Gameplay_Localization.csv"),
            ("ATimeOfWar", "ATimeOfWar", "ATimeOfWar_Localization.csv", "ATimeOfWar_Gameplay_Localization.csv"),
        };

        var uiTags = LoadCsvTags(Path.Combine(locDir, "UI_Localization.csv"));
        Console.WriteLine($"Loaded {uiTags.Count} UI tags.");

        foreach (var (scenFolder, scenName, locCsv, gameCsv) in scenarios)
        {
            Console.WriteLine($"\n--- Validating {scenName} ---");
            string locCsvPath = Path.Combine(locDir, locCsv);
            string gameCsvPath = Path.Combine(locDir, gameCsv);

            var storyTags = LoadCsvTags(locCsvPath);
            var gameplayTags = LoadCsvTags(gameCsvPath);
            Console.WriteLine($"Loaded {storyTags.Count} story tags, {gameplayTags.Count} gameplay tags.");

            var allScenTags = new Dictionary<string, string>(storyTags);
            foreach (var kvp in gameplayTags) allScenTags[kvp.Key] = kvp.Value;

            // Check icon/sprite references in CSV
            var tagRegex = new Regex(@"<(?:icon|sprite)=([A-Za-z0-9_]+)");
            foreach (var kvp in allScenTags)
            {
                var matches = tagRegex.Matches(kvp.Value);
                foreach (Match m in matches)
                {
                    string iconName = m.Groups[1].Value;
                    string setupPath = Path.Combine(setupImgDir, iconName + ".png");
                    string gamePath = Path.Combine(gameImgDir, iconName + ".png");
                    if (!File.Exists(setupPath) && !File.Exists(gamePath))
                    {
                        // Some sprites like "Storybook" or "Bank1" might have different casing or be special
                        Console.WriteLine($"[CSV UNKNOWN SPRITE] In tag {kvp.Key}: <{m.Value}> (iconName={iconName}) not found in images");
                    }
                }
            }

            // Check all C# files in scenario folder
            string scenDir = Path.Combine(webAppDir, "Shared", "Scenarios", scenFolder);
            var csFiles = Directory.GetFiles(scenDir, "*.cs");

            foreach (var csFile in csFiles)
            {
                string fileName = Path.GetFileName(csFile);
                string text = File.ReadAllText(csFile);

                // Find methods
                var methodRegex = new Regex(@"(?:public|private|internal)\s+static\s+void\s+(\w+)\s*\(([^)]*)\)\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}", RegexOptions.Multiline);
                var matches = methodRegex.Matches(text);

                foreach (Match m in matches)
                {
                    string methodName = m.Groups[1].Value;
                    string methodBody = m.Groups[3].Value;

                    // Check for AddDefaultTitle
                    if (methodBody.Contains("AddDefaultTitle()"))
                    {
                        string expectedTag = $"{methodName}_Title";
                        if (!allScenTags.ContainsKey(expectedTag) && !uiTags.ContainsKey(expectedTag))
                        {
                            Console.WriteLine($"[{fileName}::{methodName}] MISSING TAG: {expectedTag}");
                        }
                    }

                    // Check for AddDefaultBaseTitle
                    if (methodBody.Contains("AddDefaultBaseTitle()"))
                    {
                        string baseName = methodName.Split('_')[0];
                        string expectedTag = $"{baseName}_Title";
                        if (!allScenTags.ContainsKey(expectedTag) && !uiTags.ContainsKey(expectedTag))
                        {
                            Console.WriteLine($"[{fileName}::{methodName}] MISSING BASE TITLE TAG: {expectedTag}");
                        }
                    }

                    // Check for AddDefaultSubtitle
                    if (methodBody.Contains("AddDefaultSubtitle()"))
                    {
                        string expectedTag = $"{methodName}_SubTitle";
                        if (!allScenTags.ContainsKey(expectedTag) && !uiTags.ContainsKey(expectedTag))
                        {
                            Console.WriteLine($"[{fileName}::{methodName}] MISSING SUBTITLE TAG: {expectedTag}");
                        }
                    }

                    // Skip hub methods
                    if (methodBody.Contains("ActiveHub")) continue;

                    // Skip helper methods with explicit caller names
                    if (methodName.StartsWith("Show") || methodName.StartsWith("AddWitchwolf")) continue;

                    // Check for AddDefaultContent
                    if (Regex.IsMatch(methodBody, @"ActiveWindow\.AddDefaultContent\s*\("))
                    {
                        string expectedTag = $"{methodName}_Content";
                        if (!allScenTags.ContainsKey(expectedTag) && !uiTags.ContainsKey(expectedTag))
                        {
                            Console.WriteLine($"[{fileName}::{methodName}] MISSING CONTENT TAG: {expectedTag}");
                        }
                    }

                    // Check for AddNextContent(N)
                    var nextMatches = Regex.Matches(methodBody, @"ActiveWindow\.AddNextContent(?:WithLinks)?\s*\(\s*(\d+)");
                    foreach (Match nm in nextMatches)
                    {
                        int idx = int.Parse(nm.Groups[1].Value);
                        string expectedTag = $"{methodName}_Content{idx}";
                        if (!allScenTags.ContainsKey(expectedTag) && !uiTags.ContainsKey(expectedTag))
                        {
                            Console.WriteLine($"[{fileName}::{methodName}] MISSING NEXT CONTENT TAG: {expectedTag}");
                        }
                    }
                }
            }
        }

        Console.WriteLine("\n=== VALIDATOR FINISHED ===");
    }

    private static Dictionary<string, string> LoadCsvTags(string path)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return dict;

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
            var parts = line.Split(';');
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                dict[parts[0].Trim()] = parts[1];
            }
        }
        return dict;
    }
}
