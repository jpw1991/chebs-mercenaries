using SimpleJson;
using UnityEngine;

namespace ChebsMercenaries.Options;

using Logger = Jotunn.Logger;

public class Options
{
    private const string ChanceOfFemaleKey = "ChanceOfFemale";
    private const string SkinColorsKey = "SkinColors";
    private const string HairColorsKey = "HairColors";
    
    public static List<Vector3> SkinColors
    {
        get
        {
            // parse the html colors into vector3 colors useable by the shader and return them
            var cols = OptionsDict[SkinColorsKey]
                .Select(str => str.Trim()).ToList().Select(colorCode =>
                ColorUtility.TryParseHtmlString(colorCode, out Color color)
                    ? Utils.ColorToVec3(color)
                    : Vector3.zero).ToList();
            return cols;
        }
        set
        {
            var cols = value
                .Select(vec3 => $"#{ColorUtility.ToHtmlStringRGB(new Color(vec3.x, vec3.y, vec3.z))}")
                .ToList();
            OptionsDict[SkinColorsKey] = cols;
        }
    }

    public static List<Vector3> HairColors
    {
        get
        {
            // parse the html colors into vector3 colors useable by the shader and return them
            var cols = OptionsDict[HairColorsKey]
                .Select(str => str.Trim()).ToList().Select(colorCode =>
                    ColorUtility.TryParseHtmlString(colorCode, out Color color)
                        ? Utils.ColorToVec3(color)
                        : Vector3.zero).ToList();
            return cols;
        }
        set
        {
            var cols = value
                .Select(vec3 => $"#{ColorUtility.ToHtmlStringRGB(new Color(vec3.x, vec3.y, vec3.z))}")
                .ToList();
            OptionsDict[HairColorsKey] = cols;
        }
    }
    
    public static float ChanceOfFemale
    {
        get
        {
            // convert from human-readable 50% to 0.5f
            var full = OptionsDict[ChanceOfFemaleKey][0];
            var fl = float.Parse(full.Replace("%", "").Trim()) / 100;
            if (fl > 1.0f) fl = 1.0f;
            else if (fl < 0.0f) fl = 0.0f;
            return fl;
        }
        set => OptionsDict[ChanceOfFemaleKey] = new List<string>() {$"{value * 100}%" };
    }

    private static Dictionary<string, List<string>> OptionsDict => _optionsDict ??= ReadOptionsFile();
    private static Dictionary<string, List<string>> _optionsDict;
    
    private static string OptionsFileName => $"ChebsMercenaries.{Player.m_localPlayer?.GetPlayerName()}.Options.json";

    public static void SaveOptions()
    {
        // results in annoying list stuff like:
        // [{"Key":"SkinColors","Value":["#FEF5E7","#F5CBA7","#784212","#F5B041"]},{"Key":"HairColors","Value":["#000000","#C71F1F"]},{"Key":"ChanceOfFemale","Value":["50%"]}]
        //var serializedDict = SimpleJson.SimpleJson.SerializeObject(_optionsDict);
        // custom to get nicely serialized stuff:
        // {"SkinColors":["#FEF5E7","#F5CBA7","#784212","#F5B041"],"HairColors":["#000000"],"ChanceOfFemale":["50%"]}
        var serializedDict = CustomJsonSerializer.Serialize(_optionsDict);
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
    
    private static Dictionary<string, List<string>> ReadOptionsFile()
    {
        var empty = new Dictionary<string, List<string>>()
        {
            { SkinColorsKey, new List<string>(){"#FEF5E7","#F5CBA7","#784212","#F5B041"} }, 
            { HairColorsKey, new List<string>(){"#F7DC6F,#935116,#AFABAB,#FF5733,#1C2833"} }, 
            { ChanceOfFemaleKey, new List<string>(){"50%"} }
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
        
        var parsed = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, List<string>>>(content);
        if (parsed == null)
        {
            Logger.LogError("Failed to parse options.");
            return empty;
        }

        return parsed;
    }
    
    public static class CustomJsonSerializer
    {
        public static string Serialize(Dictionary<string, List<string>> dictionary)
        {
            var jsonObject = new JsonObject();
            
            foreach (var kvp in dictionary)
            {
                string key = kvp.Key;
                List<string> values = kvp.Value;

                jsonObject[key] = values;
            }
            
            return jsonObject.ToString();
        }
    }
}