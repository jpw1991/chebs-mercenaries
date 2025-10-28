using ChebsMercenaries.Minions;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ChebsMercenaries.Structure;

public class MercenaryChestOptionsGUI
{
    private const string SkinZdoKey = "ChebGonazMercSkin";
    private const string DefaultSkinColors = "#FEF5E7,#F5CBA7,#784212,#F5B041";
    private const string HairZdoKey = "ChebGonazMercHair";
    private const string DefaultHairColors = "#F7DC6F,#935116,#AFABAB,#FF5733,#1C2833";
    private const string GenderZdoKey = "ChebGonazMercSex";
    private const string DefaultGender = "50";
    
    private static GameObject _panel;
    private static Dropdown _skinColorsDropdown, _hairColorsDropdown;
    private static Text _skinColorDisplay, _hairColorDisplay;
    private static InputField _chanceOfFemaleInputField;

    private static Container _lastContainer;

    public static void Show(Container container)
    {
        // Create the panel if it does not exist
        if (!_panel)
        {
            if (GUIManager.Instance == null)
            {
                Logger.LogError("GUIManager instance is null");
                return;
            }

            if (!GUIManager.CustomGUIFront)
            {
                Logger.LogError("GUIManager CustomGUI is null");
                return;
            }

            _panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, -100),
                width: 500,
                height: 600,
                draggable: false);
            _panel.SetActive(false);

            _panel.AddComponent<DragWindowCntrl>();

            GUIManager.Instance.CreateText("Mercenary Options", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -50f),
                font: GUIManager.Instance.AveriaSerifBold, fontSize: 30, color: GUIManager.Instance.ValheimOrange,
                outline: true, outlineColor: Color.black,
                width: 350f, height: 40f, addContentSizeFitter: false);

            {
                // Merc skin color
                GUIManager.Instance.CreateText("Skin Colors:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-150f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 120f, height: 30f, addContentSizeFitter: false);
                
                var dropdownObject = GUIManager.Instance.CreateDropDown(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(-40f, 210f),
                    fontSize: 16,
                    width: 120f, height: 30f);
                _skinColorsDropdown = dropdownObject.GetComponent<Dropdown>();
                _skinColorsDropdown.onValueChanged.AddListener(UpdateSkinColorDisplay);
                
                _skinColorDisplay = GUIManager.Instance.CreateText("Skin", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(60f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 60f, height: 30f, addContentSizeFitter: false).GetComponent<Text>();

                GUIManager.Instance.CreateButton("+", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(140f, 210f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    var itemText = _skinColorsDropdown.options[_skinColorsDropdown.value].text;
                    var parseSuccessful = ColorUtility.TryParseHtmlString(itemText, out Color col);
                    if (!parseSuccessful) Logger.LogError($"Failed to parse color {itemText}, defaulting to {col}");
                    GUIManager.Instance.CreateColorPicker(
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        col,
                        "Choose a Skin Color",
                        delegate {},
                        delegate (Color color)
                        {
                            var html = $"#{ColorUtility.ToHtmlStringRGB(color)}";
                            var skins = GetSkins(container);
                            skins.Add(html);
                            SetSkins(container, skins);
                            RefreshSkinDropdown(container, skins.Count - 1);
                            UpdateSkinColorDisplay(_skinColorsDropdown.value);
                        }
                    );
                });
                
                GUIManager.Instance.CreateButton("-", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(170f, 210f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_skinColorsDropdown.options.Count > 1)
                    {
                        var itemText = _skinColorsDropdown.options[_skinColorsDropdown.value].text;
                        var skins = GetSkins(container);
                        skins.Remove(itemText);
                        SetSkins(container, skins);
                        RefreshSkinDropdown(container);
                        UpdateSkinColorDisplay(_skinColorsDropdown.value);
                    }
                });
            }
            
            {
                // Merc hair color
                GUIManager.Instance.CreateText("Hair Colors:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-150f, -140f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 120f, height: 30f, addContentSizeFitter: false);
                
                var dropdownObject = GUIManager.Instance.CreateDropDown(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(-40f, 170f),
                    fontSize: 16,
                    width: 120f, height: 30f);
                _hairColorsDropdown = dropdownObject.GetComponent<Dropdown>();
                _hairColorsDropdown.onValueChanged.AddListener(UpdateHairColorDisplay);
                
                _hairColorDisplay = GUIManager.Instance.CreateText("Hair", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(60f, -140f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 60f, height: 30f, addContentSizeFitter: false).GetComponent<Text>();

                GUIManager.Instance.CreateButton("+", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(140f, 170f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    var itemText = _hairColorsDropdown.options[_hairColorsDropdown.value].text;
                    var parseSuccessful = ColorUtility.TryParseHtmlString(itemText, out Color col);
                    if (!parseSuccessful) Logger.LogError($"Failed to parse color {itemText}, defaulting to {col}");
                    GUIManager.Instance.CreateColorPicker(
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        col,
                        "Choose a Hair Color",
                        delegate {},
                        delegate (Color color)
                        {
                            var html = $"#{ColorUtility.ToHtmlStringRGB(color)}";
                            var hairs = GetHairs(container);
                            hairs.Add(html);
                            SetHairs(container, hairs);
                            RefreshHairDropdown(container, hairs.Count - 1);
                            UpdateHairColorDisplay(_hairColorsDropdown.value);
                        }
                    );
                });
                
                GUIManager.Instance.CreateButton("-", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(170f, 170f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_hairColorsDropdown.options.Count > 1)
                    {
                        var itemText = _hairColorsDropdown.options[_hairColorsDropdown.value].text;
                        var hairs = GetHairs(container);
                        hairs.Remove(itemText);
                        SetHairs(container, hairs);
                        RefreshHairDropdown(container);
                        UpdateHairColorDisplay(_hairColorsDropdown.value);
                    }
                });
            }

            {
                // Merc gender
                GUIManager.Instance.CreateText("Female %:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-150f, -180f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 120f, height: 30f, addContentSizeFitter: false);
                
                _chanceOfFemaleInputField = GUIManager.Instance.CreateInputField(parent:  _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(-40f, 130f),
                    contentType: InputField.ContentType.Alphanumeric,
                    placeholderText: "50%",
                    fontSize: 16,
                    width: 120f, height: 30f).GetComponent<InputField>();
                _chanceOfFemaleInputField.characterLimit = 3;
                _chanceOfFemaleInputField.onValueChanged.AddListener(ChanceOfFemaleInputHandler) ;
            }
        }

        {
            // info area
            var infoAreaText = "Here you can configure the aesthetics of the mercenaries spawned by this chest. These" +
                               " settings are unique to this chest.\n\n" +
                               "Mercenary creation costs:\n\n" +
                               $"Miner: {string.Join(", ", HumanMinerMinion.ItemsCost.Value)}\n" +
                               $"Woodcutter: {string.Join(", ", HumanWoodcutterMinion.ItemsCost.Value)}\n" +
                               $"Warrior I: {string.Join(", ", MercenaryWarriorTier1Minion.ItemsCost.Value)}\n" +
                               $"Warrior II: {string.Join(", ", MercenaryWarriorTier2Minion.ItemsCost.Value)}\n" +
                               $"Warrior III: {string.Join(", ", MercenaryWarriorTier3Minion.ItemsCost.Value)}\n" +
                               $"Warrior IV: {string.Join(", ", MercenaryWarriorTier4Minion.ItemsCost.Value)}\n" +
                               $"Archer I: {string.Join(", ", MercenaryArcherTier1Minion.ItemsCost.Value)}\n" +
                               $"Archer II: {string.Join(", ", MercenaryArcherTier2Minion.ItemsCost.Value)}\n" +
                               $"Archer III: {string.Join(", ", MercenaryArcherTier3Minion.ItemsCost.Value)}\n" +
                               $"Armor Options (all except catapult): {string.Join(", ", 
                                   MercenaryChest.ArmorLeatherScrapsRequiredConfig.Value + " LeatherScraps",
                                   MercenaryChest.ArmorBronzeRequiredConfig.Value + " Bronze",
                                   MercenaryChest.ArmorIronRequiredConfig.Value + " Iron",
                                   MercenaryChest.ArmorBlackIronRequiredConfig.Value + " BlackMetal",
                                   MercenaryChest.ArmorCarapaceRequiredConfig.Value + " Carapace",
                                   MercenaryChest.ArmorFlametalRequiredConfig.Value + " Flametal"
                               ) }\n" +
                               $"Catapult: {string.Join(", ", CatapultMinion.ItemsCost.Value)}";
            
            GUIManager.Instance.CreateText(infoAreaText, parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0, -470f),
                font: GUIManager.Instance.AveriaSerifBold, fontSize: 14, color: GUIManager.Instance.ValheimOrange,
                outline: true, outlineColor: Color.black,
                width: 350f, height: 500f, addContentSizeFitter: false);
        }

        _lastContainer = container;
        
        RefreshSkinDropdown(container);
        RefreshHairDropdown(container);
        
        _skinColorsDropdown.value = 0;
        UpdateSkinColorDisplay(0);
        _hairColorsDropdown.value = 0;
        UpdateHairColorDisplay(0);

        _chanceOfFemaleInputField.text = GetGender(container);
        
        _panel.SetActive(true);
    }

    private static void ChanceOfFemaleInputHandler(string str)
    {
        if (_lastContainer == null) return;
        if (int.TryParse(str, out var result))
        {
            SetGender(_lastContainer, result.ToString());
        }
    }

    public static void Hide()
    {
        if (_panel != null) _panel.SetActive(false);
        _lastContainer = null;
    }

    public static List<string> GetSkins(Container container)
    {
        var skins = container.m_nview.GetZDO().GetString(SkinZdoKey);
        if (skins == string.Empty)
        {
            container.m_nview.GetZDO().Set(SkinZdoKey, DefaultSkinColors);
            skins = DefaultSkinColors;
        }
        return skins.Split(',').ToList();
    }
    
    private static void SetSkins(Container container, List<string> skins)
    {
        container.m_nview.GetZDO().Set(SkinZdoKey, string.Join(",", skins));
    }
    
    public static List<string> GetHairs(Container container)
    {
        var hairs = container.m_nview.GetZDO().GetString(HairZdoKey);
        if (hairs == string.Empty)
        {
            container.m_nview.GetZDO().Set(HairZdoKey, DefaultHairColors);
            hairs = DefaultHairColors;
        }
        return hairs.Split(',').ToList();
    }
    
    private static void SetHairs(Container container, List<string> hairs)
    {
        container.m_nview.GetZDO().Set(HairZdoKey, string.Join(",", hairs));
    }
    
    public static string GetGender(Container container)
    {
        var gender = container.m_nview.GetZDO().GetString(GenderZdoKey);
        if (gender == string.Empty)
        {
            container.m_nview.GetZDO().Set(GenderZdoKey, DefaultGender);
            gender = DefaultGender;
        }
        return gender;
    }
    
    private static void SetGender(Container container, string gender)
    {
        container.m_nview.GetZDO().Set(GenderZdoKey, gender);
    }
    
    private static void UpdateSkinColorDisplay(int unused)
    {
        var itemText = _skinColorsDropdown.options[_skinColorsDropdown.value].text;
        var parseSuccessful = ColorUtility.TryParseHtmlString(itemText, out Color col);
        if (!parseSuccessful) Logger.LogError($"Failed to parse color {itemText}, defaulting to {col}");
        _skinColorDisplay.color = col;
    }

    private static void UpdateHairColorDisplay(int unused)
    {
        var itemText = _hairColorsDropdown.options[_hairColorsDropdown.value].text;
        var parseSuccessful = ColorUtility.TryParseHtmlString(itemText, out Color col);
        if (!parseSuccessful) Logger.LogError($"Failed to parse color {itemText}, defaulting to {col}");
        _hairColorDisplay.color = col;
    }

    private static void RefreshSkinDropdown(Container container, int newIndex = 0)
    {
        _skinColorsDropdown.ClearOptions();
        _skinColorsDropdown.AddOptions(GetSkins(container));
        _skinColorsDropdown.value = newIndex;
        _skinColorsDropdown.RefreshShownValue();
    }
    
    private static void RefreshHairDropdown(Container container, int newIndex = 0)
    {
        _hairColorsDropdown.ClearOptions();
        _hairColorsDropdown.AddOptions(GetHairs(container));
        _hairColorsDropdown.value = newIndex;
        _hairColorsDropdown.RefreshShownValue();
    }
}