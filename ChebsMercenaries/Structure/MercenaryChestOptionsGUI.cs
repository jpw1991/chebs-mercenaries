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
                               $"<color=silver>Miner:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(HumanMinerMinion.ItemsCost.Value))}\n" +
                               $"<color=silver>Woodcutter:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(HumanWoodcutterMinion.ItemsCost.Value))}\n" +
                               $"<color=silver>Warrior I:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(MercenaryWarriorTier1Minion.ItemsCost.Value))}\n" +
                               $"<color=silver>Warrior II:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(MercenaryWarriorTier2Minion.ItemsCost.Value))}\n" +
                               $"<color=silver>Warrior III:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(MercenaryWarriorTier3Minion.ItemsCost.Value))}\n" +
                               $"<color=silver>Warrior IV:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(MercenaryWarriorTier4Minion.ItemsCost.Value))}\n" +
                               $"<color=silver>Archer I:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(MercenaryArcherTier1Minion.ItemsCost.Value))}\n" +
                               $"<color=silver>Archer II:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(MercenaryArcherTier2Minion.ItemsCost.Value))}\n" +
                               $"<color=silver>Archer III:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(MercenaryArcherTier3Minion.ItemsCost.Value))}\n" +
                               $"<color=silver>Catapult:</color> {string.Join("<color=white>, </color>", LocalizeItemsCost(CatapultMinion.ItemsCost.Value))}\n" +
                               $"\n<b>Armor Options (all except catapult):</b> {string.Join("<color=white>, </color>",
                                   "<color=cyan>" + MercenaryChest.ArmorLeatherScrapsRequiredConfig.Value + "</color> <color=red>" + LocalizeItemPrefabByPrefabName("LeatherScraps") + "</color>",
                                   "<color=cyan>" + MercenaryChest.ArmorBronzeRequiredConfig.Value + "</color> <color=red>" + LocalizeItemPrefabByPrefabName("Bronze") + "</color>",
                                   "<color=cyan>" + MercenaryChest.ArmorIronRequiredConfig.Value + "</color> <color=red>" + LocalizeItemPrefabByPrefabName("Iron") + "</color>",
                                   "<color=cyan>" + MercenaryChest.ArmorBlackIronRequiredConfig.Value + "</color> <color=red>" + LocalizeItemPrefabByPrefabName("BlackMetal") + "</color>",
                                   "<color=cyan>" + MercenaryChest.ArmorCarapaceRequiredConfig.Value + "</color> <color=red>" + LocalizeItemPrefabByPrefabName("Carapace") + "</color>",
                                   "<color=cyan>" + MercenaryChest.ArmorFlametalRequiredConfig.Value + "</color> <color=red>" + LocalizeItemPrefabByPrefabName("FlametalNew") + "</color>"
                               )}\n";

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

    private static string LocalizeItemsCost(List<string> itemsCost)
    {
        // Take the items cost eg. Wood:5 and localize it eg. Holz:5
        const string errorMsg = "Error. Check player.log";
        var finished = new List<string>();
        foreach (var fuel in itemsCost)
        {
            var splut = fuel.Split(':');
            if (splut.Length != 2)
            {
                Logger.LogError("Error in config for ItemsCost - please revise.");
                return errorMsg;
            }

            var itemRequired = splut[0];
            if (!int.TryParse(splut[1], out var itemAmountRequired))
            {
                Logger.LogError("Error in config for ItemsCost - please revise.");
                return errorMsg;
            }
            
            var acceptedItems = itemRequired.Split('|');
            var itemsLocalized = new List<string>();
            foreach (var acceptedItem in acceptedItems)
            {
                var requiredItemPrefab = ZNetScene.instance.GetPrefab(acceptedItem);
                if (requiredItemPrefab == null)
                {
                    Logger.LogError($"Error processing config for ItemsCost: {itemRequired} doesn't exist.");
                    return errorMsg;
                }
                itemsLocalized.Add($"<color=red>{LocalizationManager.Instance.TryTranslate(requiredItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name)}</color>");
            }

            finished.Add($"{string.Join("<color=white>|</color>", itemsLocalized)}<color=white>:</color><color=aqua>{itemAmountRequired}</color>");
        }
        return string.Join("<color=white>, </color>", finished);
    }
    
    private static string LocalizeItemPrefabByPrefabName(string prefabName)
    {
        // Localize a specific prefab eg. Wood and localize it eg. Holz
        const string errorMsg = "Error. Check player.log";
        var prefab = ZNetScene.instance.GetPrefab(prefabName);
        if (prefab == null)
        {
            Logger.LogError($"Error prefab {prefab} doesn't exist.");
            return errorMsg;
        }
        return LocalizationManager.Instance.TryTranslate(prefab.GetComponent<ItemDrop>().m_itemData.m_shared .m_name);
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