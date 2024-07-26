using UnityEngine;

namespace ChebsMercenaries.Options;

using Logger = Jotunn.Logger;

public class Options
{
    private const string ChanceOfFemaleKey = "ChanceOfFemale";
    private const string SkinColorsKey = "EyeColors";
    private const string HairColorsKey = "HairColors";
    
    public static List<Vector3> SkinColors
    {
        get
        {
            // parse the html colors into vector3 colors useable by the shader and return them
            var cols = OptionsDict[SkinColorsKey]
                .Split(',').Select(str => str.Trim()).ToList().Select(colorCode =>
                ColorUtility.TryParseHtmlString(colorCode, out Color color)
                    ? Utils.ColorToVec3(color)
                    : Vector3.zero).ToList();
            return cols;
        }
        set
        {
            var cols = string.Join(",", value.Select(vec => 
                ColorUtility.ToHtmlStringRGB(new Color(vec.x, vec.y, vec.z)).Select(o => $"#{o}").ToList()));
            OptionsDict[SkinColorsKey] = cols;
        }
    }

    public static List<Vector3> HairColors
    {
        get
        {
            // parse the html colors into vector3 colors useable by the shader and return them
            var cols = OptionsDict[SkinColorsKey]
                .Split(',').Select(str => str.Trim()).ToList().Select(colorCode =>
                    ColorUtility.TryParseHtmlString(colorCode, out Color color)
                        ? Utils.ColorToVec3(color)
                        : Vector3.zero).ToList();
            return cols;
        }
        set
        {
            var cols = string.Join(",", value.Select(vec => 
                ColorUtility.ToHtmlStringRGB(new Color(vec.x, vec.y, vec.z)).Select(o => $"#{o}").ToList()));
            OptionsDict[SkinColorsKey] = cols;
        }
    }
    
    public static float ChanceOfFemale
    {
        get
        {
            // convert from human-readable 50% to 0.5f
            var full = OptionsDict[ChanceOfFemaleKey];
            var fl = float.Parse(full.Replace("%", "").Trim()) / 100;
            if (fl > 1.0f) fl = 1.0f;
            else if (fl < 0.0f) fl = 0.0f;
            return fl;
        }
        set => OptionsDict[ChanceOfFemaleKey] = $"{value * 100}%";
    }

    private static Dictionary<string, string> OptionsDict => _optionsDict ??= ReadOptionsFile();
    private static Dictionary<string, string> _optionsDict;
    
    private static string OptionsFileName => $"ChebsMercenaries.{Player.m_localPlayer?.GetPlayerName()}.Options.json";

    public static void SaveOptions()
    {
        var serializedDict = DictionaryToJson(OptionsDict); 
        Logger.LogInfo($"serializedDict={serializedDict}");
        UpdateOptionsFile(serializedDict);
    }
    
    private static void UpdateOptionsFile(string content)
    {
        var filePath = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), OptionsFileName);

        if (!File.Exists(filePath))
        {
            try
            {
                using var fs = File.Create(filePath);
                fs.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating {filePath}: {ex.Message}");
            }
        }

        try
        {
            using var writer = new StreamWriter(filePath, false);
            writer.Write(content);
            writer.Close();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error writing to {filePath}: {ex.Message}");
        }
    }
    
    private static Dictionary<string, string> ReadOptionsFile()
    {
        var empty = new Dictionary<string, string>()
        {
            { SkinColorsKey, "#FEF5E7,#F5CBA7,#784212,#F5B041" }, 
            { HairColorsKey, "#F7DC6F,#935116,#AFABAB,#FF5733,#1C2833" }, 
            { ChanceOfFemaleKey, "50%" }
        };
        
        var filePath = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), OptionsFileName);

        if (!File.Exists(filePath))
        {
            try
            {
                using var fs = File.Create(filePath);
                fs.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating {filePath}: {ex.Message}");
            }
        }

        string content = null;
        try
        {
            using var reader = new StreamReader(filePath);
            content = reader.ReadToEnd();
            reader.Close();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error reading from {filePath}: {ex.Message}");
        }

        if (string.IsNullOrEmpty(content))
        {
            Logger.LogInfo($"Content is empty or null; create fresh options.");
            return empty;
        }

        // Logger.LogInfo($"Attempting to parse {content}");
        var parsed = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, string>>(content);
        // Logger.LogInfo($"parsed={parsed}");
        if (parsed == null)
        {
            Logger.LogError("Failed to parse options.");
            return empty;
        }

        return parsed;
    }
    
    private static string DictionaryToJson(Dictionary<string, string> dictionary)
    {
        // because simplejson is being a PITA and saving a list of keyvalue pairs that it can't parse back in, let's
        // do it ourselves
        var entries = new List<string>();
        foreach (var kvp in dictionary)
        {
            var entry = $"\"{kvp.Key}\": {kvp.Value}";
            entries.Add(entry);
        }
        return "{" + string.Join(", ", entries) + "}";
    }
}