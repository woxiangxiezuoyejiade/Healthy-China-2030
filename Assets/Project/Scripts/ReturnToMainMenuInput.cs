using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
#endif

public class ReturnToMainMenuInput : MonoBehaviour
{
    [Header("Return Target")]
    [Tooltip("For Func1/Func2, return to the current scene's intro button page instead of loading a separate scene.")]
    public bool returnToCurrentSceneIntro = true;
    public string mainMenuSceneName = "MainMenu";
    public string mainMenuScenePath = "Assets/Project/Scenes/MainMenu.unity";
    public System.Action OnReturnRequested;

    [Header("Input")]
    public bool enableKeyboardEscape = true;
    public bool enableLeftController = true;
    public bool enableRightController = true;

    [Tooltip("Standard secondaryButton (Pico B/Y).")]
    public bool useSecondaryButton = true;
    [Tooltip("Standard menuButton (Pico flat menu key).")]
    public bool useMenuButton = true;
    [Tooltip("Joystick click.")]
    public bool useJoystickClick = true;
    [Tooltip("Legacy Unity joystick buttons. Helps Pico devices that do not report B/Y as XR secondaryButton.")]
    public bool useLegacyJoystickButtons = true;
    public KeyCode[] legacyReturnButtons =
    {
        KeyCode.JoystickButton1,
        KeyCode.JoystickButton3,
        KeyCode.JoystickButton7,
        KeyCode.JoystickButton8,
        KeyCode.JoystickButton9
    };
    [Tooltip("Brute-force: scan ALL bool usages on device. Uncheck if it causes false triggers.")]
    public bool useBruteForceScan = true;
    [Tooltip("Usage NAMES to exclude from brute-force scan (training uses these). Case-insensitive substring match.")]
    public List<string> excludedUsagePatterns = new List<string> { "Trigger", "Grip", "PrimaryButton", "Battery", "IsTracked", "TrackingState" };
    public bool usePrimaryButtonFallback = true;
    public float primaryFallbackHoldSeconds = 0.6f;

    [Header("Visible Return Button")]
    public bool createVisibleReturnButton = true;
    public string returnButtonText = "\u8fd4\u56de\u9009\u62e9\u9875";
    public Vector3 returnButtonCameraOffset = new Vector3(-0.43f, 0.28f, 1.12f);
    public Vector2 returnButtonSize = new Vector2(210f, 62f);
    public float returnButtonScale = 0.00125f;
    public Color returnButtonColor = new Color(0.10f, 0.38f, 0.46f, 0.82f);
    public Color returnButtonTextColor = Color.white;

    XRInputDevice leftDevice;
    XRInputDevice rightDevice;
    bool wasPressed;
    bool isReturning;
    float primaryFallbackHeldTime;
    GameObject returnButtonRoot;
    TextMeshProUGUI returnButtonLabel;
    bool returnButtonFontApplied;

    // Cache all bool usages per device (refreshed every 120 frames or on device reconnect).
    List<InputFeatureUsage<bool>> leftBoolUsages = new List<InputFeatureUsage<bool>>();
    List<InputFeatureUsage<bool>> rightBoolUsages = new List<InputFeatureUsage<bool>>();
    int lastCacheFrame = -1;

    void Start()
    {
        EnsureVisibleReturnButton();
    }

    void Update()
    {
        if (isReturning) return;
        EnsureVisibleReturnButton();

        // Refresh usage cache periodically.
        if (Time.frameCount - lastCacheFrame > 120 || lastCacheFrame < 0)
        {
            lastCacheFrame = Time.frameCount;
            CacheBoolUsages(XRNode.LeftHand, ref leftDevice, leftBoolUsages);
            CacheBoolUsages(XRNode.RightHand, ref rightDevice, rightBoolUsages);
        }

        bool pressed = IsReturnPressed();
        if (pressed && !wasPressed)
        {
            ReturnToMainMenu();
        }

        wasPressed = pressed;
    }

    public void Configure(string sceneName, bool returnToCurrentScene = true, System.Action onReturnRequested = null)
    {
        returnToCurrentSceneIntro = returnToCurrentScene;
        OnReturnRequested = onReturnRequested;
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            mainMenuSceneName = sceneName;
        }
    }

    public void SetReturnButtonText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        returnButtonText = text;
        if (returnButtonLabel != null)
        {
            returnButtonLabel.text = returnButtonText;
            returnButtonFontApplied = false;
            ApplyReturnButtonFont();
        }
    }

    void CacheBoolUsages(XRNode node, ref XRInputDevice device, List<InputFeatureUsage<bool>> cache)
    {
        cache.Clear();

        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(node);
        }
        if (!device.isValid) return;

        // Enumerate ALL usages on the device, filter to bool type.
        var allUsages = new List<InputFeatureUsage>();
        if (device.TryGetFeatureUsages(allUsages))
        {
            foreach (var usage in allUsages)
            {
                if (usage.type == typeof(bool))
                {
                    cache.Add(new InputFeatureUsage<bool>(usage.name));
                }
            }
        }
    }

    void ReturnToMainMenu()
    {
        if (OnReturnRequested != null)
        {
            OnReturnRequested.Invoke();
            return;
        }

        isReturning = true;
        Time.timeScale = 1f;

        if (returnToCurrentSceneIntro)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && !string.IsNullOrWhiteSpace(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name);
                return;
            }
        }

        if (TryLoadScene(mainMenuSceneName))
        {
            return;
        }

        if (TryLoadScene(mainMenuScenePath))
        {
            return;
        }

        isReturning = false;
        Debug.LogError($"Cannot load return scene. Add '{mainMenuSceneName}' / '{mainMenuScenePath}' to File > Build Settings > Scenes In Build.");
    }

    bool TryLoadScene(string sceneNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(sceneNameOrPath)) return false;
        if (!Application.CanStreamedLevelBeLoaded(sceneNameOrPath)) return false;

        SceneManager.LoadScene(sceneNameOrPath);
        return true;
    }

    void EnsureVisibleReturnButton()
    {
        if (!createVisibleReturnButton) return;
        if (returnButtonRoot != null)
        {
            ApplyReturnButtonFont();
            return;
        }

        Camera camera = Camera.main;
        if (camera == null) return;

        Transform existing = camera.transform.Find("Runtime_ReturnToMainMenu_Button");
        if (existing != null)
        {
            returnButtonRoot = existing.gameObject;
            returnButtonLabel = returnButtonRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            Button existingButton = returnButtonRoot.GetComponentInChildren<Button>(true);
            if (existingButton != null)
            {
                existingButton.onClick.RemoveListener(ReturnToMainMenu);
                existingButton.onClick.AddListener(ReturnToMainMenu);
            }
            ApplyReturnButtonFont();
            return;
        }

        returnButtonRoot = new GameObject("Runtime_ReturnToMainMenu_Button");
        returnButtonRoot.transform.SetParent(camera.transform, false);
        returnButtonRoot.transform.localPosition = returnButtonCameraOffset;
        returnButtonRoot.transform.localRotation = Quaternion.identity;
        returnButtonRoot.transform.localScale = Vector3.one * returnButtonScale;

        Canvas canvas = returnButtonRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = returnButtonRoot.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        returnButtonRoot.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = returnButtonRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = returnButtonSize;

        GameObject buttonObject = new GameObject("Return_Button");
        buttonObject.transform.SetParent(returnButtonRoot.transform, false);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = returnButtonSize;

        Image image = buttonObject.AddComponent<Image>();
        image.color = returnButtonColor;
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(ReturnToMainMenu);

        ColorBlock colors = button.colors;
        colors.normalColor = returnButtonColor;
        colors.highlightedColor = new Color(0.18f, 0.62f, 0.72f, 0.92f);
        colors.pressedColor = new Color(0.06f, 0.25f, 0.32f, 0.95f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        returnButtonLabel = labelObject.AddComponent<TextMeshProUGUI>();
        returnButtonLabel.text = returnButtonText;
        returnButtonLabel.fontSize = 25f;
        returnButtonLabel.fontStyle = FontStyles.Bold;
        returnButtonLabel.alignment = TextAlignmentOptions.Center;
        returnButtonLabel.color = returnButtonTextColor;
        returnButtonLabel.raycastTarget = false;
        ApplyReturnButtonFont();
    }

    void ApplyReturnButtonFont()
    {
        if (returnButtonLabel == null || returnButtonFontApplied) return;

        TMP_FontAsset selectedFont = null;
        TextMeshProUGUI[] sceneTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in sceneTexts)
        {
            if (text == null || text == returnButtonLabel || text.font == null) continue;

            string fontName = text.font.name;
            if (fontName.Contains("HanyiHuaMulanW"))
            {
                selectedFont = text.font;
                break;
            }

            if (selectedFont == null &&
                (fontName.Contains("msyh") || fontName.Contains("YaHei")))
            {
                selectedFont = text.font;
            }

            if (selectedFont == null &&
                !fontName.Contains("Liberation") &&
                !fontName.Contains("Default") &&
                !fontName.Contains("TMP"))
            {
                selectedFont = text.font;
            }
        }

        if (selectedFont == null) return;

        returnButtonLabel.font = selectedFont;
        selectedFont.TryAddCharacters(returnButtonText, out string missingCharacters);
        returnButtonFontApplied =
            string.IsNullOrEmpty(missingCharacters) ||
            selectedFont.name.Contains("HanyiHuaMulanW") ||
            selectedFont.name.Contains("msyh") ||
            selectedFont.name.Contains("YaHei");
    }

    bool IsReturnPressed()
    {
        if (enableKeyboardEscape && KeyboardInputCompat.GetKeyDown(KeyCode.Escape))
        {
            return true;
        }

        bool immediatePressed =
            (enableLeftController && IsDeviceReturnPressed(XRNode.LeftHand, ref leftDevice, leftBoolUsages)) ||
            (enableRightController && IsDeviceReturnPressed(XRNode.RightHand, ref rightDevice, rightBoolUsages)) ||
            IsLegacyJoystickReturnPressed() ||
            IsAnyInputSystemReturnPressed();

        if (immediatePressed)
        {
            primaryFallbackHeldTime = 0f;
            return true;
        }

        if (usePrimaryButtonFallback && IsPrimaryFallbackPressed())
        {
            primaryFallbackHeldTime += Time.unscaledDeltaTime;
            return primaryFallbackHeldTime >= Mathf.Max(0.1f, primaryFallbackHoldSeconds);
        }

        primaryFallbackHeldTime = 0f;
        return false;
    }

    bool IsDeviceReturnPressed(XRNode node, ref XRInputDevice device, List<InputFeatureUsage<bool>> cachedBoolUsages)
    {
        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(node);
            // Force cache refresh on device reconnect.
            lastCacheFrame = -1;
        }

        if (!device.isValid) return false;

        // --- Route 1: Brute-force scan ALL bool usages the device actually supports ---
        if (useBruteForceScan && cachedBoolUsages.Count > 0)
        {
            foreach (var usage in cachedBoolUsages)
            {
                // Skip usages that training depends on (trigger, grip, primary button).
                if (IsExcluded(usage.name)) continue;

                if (device.TryGetFeatureValue(usage, out bool value) && value)
                {
                    return true;
                }
            }
        }

        // --- Route 2: Explicit fast-path checks (always checked, even if brute force is off) ---
        if (useSecondaryButton &&
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool secondaryPressed) &&
            secondaryPressed)
        {
            return true;
        }

        if (useMenuButton &&
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool menuPressed) &&
            menuPressed)
        {
            return true;
        }

        if (useJoystickClick &&
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out bool stickClicked) &&
            stickClicked)
        {
            return true;
        }

        // Also check these regardless of toggles (they won't be in training mappings).
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondary2DAxisClick, out bool s2Click) && s2Click) return true;
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryTouch, out bool pTouch) && pTouch) return true;
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryTouch, out bool sTouch) && sTouch) return true;

        return false;
    }

    bool IsExcluded(string usageName)
    {
        if (excludedUsagePatterns == null || excludedUsagePatterns.Count == 0) return false;

        foreach (string pattern in excludedUsagePatterns)
        {
            if (!string.IsNullOrEmpty(pattern) &&
                usageName.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    bool IsLegacyJoystickReturnPressed()
    {
        if (!useLegacyJoystickButtons || legacyReturnButtons == null) return false;

#if ENABLE_LEGACY_INPUT_MANAGER
        for (int i = 0; i < legacyReturnButtons.Length; i++)
        {
            if (Input.GetKeyDown(legacyReturnButtons[i]))
            {
                return true;
            }
        }
#endif

        return false;
    }

    bool IsPrimaryFallbackPressed()
    {
        bool leftPressed = enableLeftController && IsDevicePrimaryPressed(XRNode.LeftHand, ref leftDevice);
        bool rightPressed = enableRightController && IsDevicePrimaryPressed(XRNode.RightHand, ref rightDevice);
        return leftPressed || rightPressed || IsAnyInputSystemPrimaryPressed();
    }

    bool IsDevicePrimaryPressed(XRNode node, ref XRInputDevice device)
    {
        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(node);
        }

        return device.isValid &&
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primaryPressed) &&
            primaryPressed;
    }

    bool IsAnyInputSystemReturnPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (IsInputSystemControllerReturnPressed(XRController.leftHand)) return true;
        if (IsInputSystemControllerReturnPressed(XRController.rightHand)) return true;
        if (IsInputSystemGamepadReturnPressed()) return true;
#endif
        return false;
    }

#if ENABLE_INPUT_SYSTEM
    bool IsInputSystemControllerReturnPressed(XRController controller)
    {
        if (controller == null) return false;

        string[] names =
        {
            "secondaryButton", "menuButton", "primary2DAxisClick", "secondary2DAxisClick",
            "buttonEast", "buttonNorth", "buttonWest", "buttonSouth",
            "primaryTouch", "secondaryTouch", "start", "select",
        };

        foreach (string name in names)
        {
            var btn = controller.TryGetChildControl<ButtonControl>(name);
            if (btn != null && btn.wasPressedThisFrame) return true;
        }

        return false;
    }

    bool IsAnyInputSystemPrimaryPressed()
    {
        return IsInputSystemPrimaryPressed(XRController.leftHand) ||
               IsInputSystemPrimaryPressed(XRController.rightHand) ||
               IsInputSystemGamepadPrimaryPressed();
    }

    bool IsInputSystemPrimaryPressed(XRController controller)
    {
        return controller != null && IsPressed(controller.TryGetChildControl<ButtonControl>("primaryButton"));
    }

    bool IsPressed(ButtonControl button)
    {
        return button != null && button.isPressed;
    }

    bool IsInputSystemGamepadReturnPressed()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad == null) continue;

            if (WasPressed(gamepad.buttonEast)) return true;
            if (WasPressed(gamepad.buttonNorth)) return true;
            if (WasPressed(gamepad.startButton)) return true;
            if (WasPressed(gamepad.selectButton)) return true;
            if (WasPressed(gamepad.leftStickButton)) return true;
            if (WasPressed(gamepad.rightStickButton)) return true;
        }

        return false;
    }

    bool IsInputSystemGamepadPrimaryPressed()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad == null) continue;

            if (IsPressed(gamepad.buttonSouth)) return true;
            if (IsPressed(gamepad.buttonWest)) return true;
        }

        return false;
    }

    bool WasPressed(ButtonControl button)
    {
        return button != null && button.wasPressedThisFrame;
    }
#else
    bool IsAnyInputSystemPrimaryPressed()
    {
        return false;
    }
#endif
}
