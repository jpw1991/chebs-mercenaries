using BepInEx;
using BepInEx.Configuration;
using ChebsValheimLibrary.PvP;
using Jotunn.Configs;
using Jotunn.GUI;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ChebsMercenaries.Options;

public class OptionsGUI
{
    private static GameObject _panel;
    private static Dropdown _skinColorsDropdown, _hairColorsDropdown;
    private static Text _skinColorDisplay, _hairColorDisplay;
    private static Text _alliesText;
    private static InputField _allyInput;

    private static List<string> _unsavedFriends, _unsavedSkins, _unsavedHairs;

    public static ConfigEntry<KeyboardShortcut> OptionsKeyConfigEntry;
    public static ButtonConfig OptionsButton;

    public static void CreateConfigs(BaseUnityPlugin plugin, string pluginGuid)
    {
        const string client = "Options (Client)";

        OptionsKeyConfigEntry = plugin.Config.Bind(client, "OpenOptions",
            new KeyboardShortcut(KeyCode.F7), new ConfigDescription("Open the mod options window."));
        
        OptionsButton = new ButtonConfig
        {
            Name = "OptionsButton",
            ShortcutConfig = OptionsKeyConfigEntry,
            HintToken = "OptionsButton"
        };
        InputManager.Instance.AddButton(pluginGuid, OptionsButton);
    }

    public static void TogglePanel()
    {
        // abort if player's not in game
        if (Player.m_localPlayer == null) return;

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
                position: new Vector2(0, 0),
                width: 850,
                height: 600,
                draggable: false);
            _panel.SetActive(false);

            _panel.AddComponent<DragWindowCntrl>();

            GUIManager.Instance.CreateText("Cheb's Mercenaries Options", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -50f),
                font: GUIManager.Instance.AveriaSerifBold, fontSize: 30, color: GUIManager.Instance.ValheimOrange,
                outline: true, outlineColor: Color.black,
                width: 350f, height: 40f, addContentSizeFitter: false);

            {
                // Merc skin color
                GUIManager.Instance.CreateText("Mercenary Skin Colors:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);
                
                var dropdownObject = GUIManager.Instance.CreateDropDown(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 210f),
                    fontSize: 16,
                    width: 200f, height: 30f);
                _skinColorsDropdown = dropdownObject.GetComponent<Dropdown>();
                _skinColorsDropdown.onValueChanged.AddListener(UpdateSkinColorDisplay);
                
                _skinColorDisplay = GUIManager.Instance.CreateText("Skin", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(150f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 60f, height: 30f, addContentSizeFitter: false).GetComponent<Text>();

                GUIManager.Instance.CreateButton("+", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(200f, 210f),
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
                            _unsavedSkins.Add(html);
                            RefreshSkinDropdown(_unsavedSkins.Count - 1);
                        }
                    );
                });
                
                GUIManager.Instance.CreateButton("-", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(260f, 210f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_skinColorsDropdown.options.Count > 1)
                    {
                        var itemText = _skinColorsDropdown.options[_skinColorsDropdown.value].text;
                        _unsavedSkins.Remove(itemText);
                        RefreshSkinDropdown();
                    }
                });
            }
            
            {
                // Merc hair color
                GUIManager.Instance.CreateText("Mercenary Hair Colors:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -140f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);
                
                var dropdownObject = GUIManager.Instance.CreateDropDown(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 170f),
                    fontSize: 16,
                    width: 200f, height: 30f);
                _hairColorsDropdown = dropdownObject.GetComponent<Dropdown>();
                _hairColorsDropdown.onValueChanged.AddListener(UpdateHairColorDisplay);
                
                _hairColorDisplay = GUIManager.Instance.CreateText("Hair", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(150f, -140f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 60f, height: 30f, addContentSizeFitter: false).GetComponent<Text>();

                GUIManager.Instance.CreateButton("+", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(200f, 170f),
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
                            _unsavedHairs.Add(html);
                            RefreshHairDropdown(_unsavedHairs.Count - 1);
                        }
                    );
                });
                
                GUIManager.Instance.CreateButton("-", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(260f, 170f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_hairColorsDropdown.options.Count > 1)
                    {
                        var itemText = _hairColorsDropdown.options[_hairColorsDropdown.value].text;
                        _unsavedHairs.Remove(itemText);
                        RefreshHairDropdown();
                    }
                });
            }
            
            {
                // PvP stuff
                var allies = PvPManager.GetPlayerFriends();
                GUIManager.Instance.CreateText("PvP Allies:", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -210f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);

                var textObject = GUIManager.Instance.CreateText(string.Join(", ", allies), parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -210f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 400f, height: 30f, addContentSizeFitter: false);
                _alliesText = textObject.GetComponentInChildren<Text>();

                // add/remove ally
                GUIManager.Instance.CreateText("Ally (case sensitive):", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(-250f, -240f),
                    font: GUIManager.Instance.AveriaSerifBold, fontSize: 16, color: GUIManager.Instance.ValheimOrange,
                    outline: true, outlineColor: Color.black,
                    width: 200f, height: 30f, addContentSizeFitter: false);

                _allyInput = GUIManager.Instance.CreateInputField(parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0f, 60f),
                    contentType: InputField.ContentType.Standard,
                    placeholderText: "player",
                    fontSize: 16,
                    width: 200f,
                    height: 30f).GetComponentInChildren<InputField>();
                _allyInput.characterValidation = InputField.CharacterValidation.Alphanumeric;

                GUIManager.Instance.CreateButton("+", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(200f, 60f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_allyInput.text != string.Empty)
                    {
                        var newAlly = _allyInput.text;
                        if (!_unsavedFriends.Contains(newAlly))
                        {
                            _unsavedFriends.Add(newAlly);
                        }

                        _alliesText.text = string.Join(", ", _unsavedFriends);

                        _allyInput.text = string.Empty;
                    }
                });

                GUIManager.Instance.CreateButton("-", parent: _panel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(260f, 60f),
                    width: 30f, height: 30f).GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_allyInput.text != string.Empty)
                    {
                        var newAlly = _allyInput.text;
                        if (_unsavedFriends.Contains(newAlly))
                        {
                            _unsavedFriends.Remove(newAlly);
                        }

                        _alliesText.text = string.Join(", ", _unsavedFriends);

                        _allyInput.text = string.Empty;
                    }
                });
            }

            // close button
            GUIManager.Instance.CreateButton("Cancel", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, -250f),
                width: 250f, height: 60f).GetComponent<Button>().onClick.AddListener(TogglePanel);

            GUIManager.Instance.CreateButton("Save", parent: _panel.transform,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(250f, -250f),
                width: 250f, height: 60f).GetComponent<Button>().onClick.AddListener(() =>
            {
                // save PvP
                PvPManager.UpdatePlayerFriendsDict(_unsavedFriends);
                
                // save aesthetics
                var vec3Skins = new List<Vector3>();
                foreach (var unsavedSkin in _unsavedSkins)
                {
                    if (!ColorUtility.TryParseHtmlString(unsavedSkin, out Color color))
                    {
                        Logger.LogError($"Failed to save {unsavedSkin} as skin color - couldn't parse html color");
                    }
                    vec3Skins.Add(Utils.ColorToVec3(color));
                }
                Options.SkinColors = vec3Skins;
                
                var vec3Hairs = new List<Vector3>();
                foreach (var unsavedHair in _unsavedHairs)
                {
                    if (!ColorUtility.TryParseHtmlString(unsavedHair, out Color color))
                    {
                        Logger.LogError($"Failed to save {unsavedHair} as hair color - couldn't parse html color");
                    }
                    vec3Hairs.Add(Utils.ColorToVec3(color));
                }
                Options.HairColors = vec3Hairs;
                
                Options.SaveOptions();

                TogglePanel();
            });
        }

        var state = !_panel.activeSelf;
        _panel.SetActive(state);

        _unsavedFriends = PvPManager.GetPlayerFriends()
            .ToList(); // ensure new copy, not byref. Fixed in CVL 2.6.3
        _alliesText.text = string.Join(", ", _unsavedFriends);

        _unsavedSkins = Options.SkinColors.Select(v => $"#{ColorUtility.ToHtmlStringRGB(Utils.Vec3ToColor(v))}").ToList();
        RefreshSkinDropdown();
        _unsavedHairs = Options.HairColors.Select(v => $"#{ColorUtility.ToHtmlStringRGB(Utils.Vec3ToColor(v))}").ToList();
        RefreshHairDropdown();
        
        _skinColorsDropdown.value = 0;
        UpdateSkinColorDisplay(0);
        _hairColorsDropdown.value = 0;
        UpdateHairColorDisplay(0);

        // Toggle input for the player and camera while displaying the GUI
        GUIManager.BlockInput(state);
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

    private static void RefreshSkinDropdown(int newIndex = 0)
    {
        _skinColorsDropdown.ClearOptions();
        _skinColorsDropdown.AddOptions(_unsavedSkins);
        _skinColorsDropdown.value = newIndex;
        _skinColorsDropdown.RefreshShownValue();
    }
    
    private static void RefreshHairDropdown(int newIndex = 0)
    {
        _hairColorsDropdown.ClearOptions();
        _hairColorsDropdown.AddOptions(_unsavedHairs);
        _hairColorsDropdown.value = newIndex;
        _hairColorsDropdown.RefreshShownValue();
    }
}