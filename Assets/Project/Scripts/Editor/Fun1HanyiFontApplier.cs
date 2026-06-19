using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class Fun1HanyiFontApplier
{
    const string SourceFontPath = "Assets/Project/Fonts/HanyiHuaMulanW.ttf";
    const string FontAssetPath = "Assets/Project/Fonts/HanyiHuaMulanW SDF.asset";
    const string LegacyMirrorObjectName = "Hanyi_Legacy_Text";

    const string WarmupCharacters =
        "\u8bf7\u5750\u6b63\u5e76\u9762\u5411\u80fd\u91cf\u6838\u5fc3\u4fdd\u6301\u4e0a\u81c2\u8d34\u8fd1\u8eab\u4f53" +
        "\u8098\u90e8\u5f2f\u66f290\u5ea6\u6b63\u5728\u6821\u51c6\u5931\u8d25\u68c0\u67e5\u63a7\u5236\u5668\u540e\u91cd\u8bd5" +
        "\u7f13\u6162\u5411\u5916\u65cb\u8f6c\u524d\u81c2\u5f88\u597d\u7ee7\u7eed\u56de\u5230\u8d77\u70b9\u52a8\u4f5c\u8fd8\u4e0d\u591f\u6807\u51c6\u8c03\u6574\u59ff\u52bf" +
        "\u672a\u627e\u5230\u8bad\u7ec3\u624b\u67c4\u53f3\u624b\u5de6\u624b\u5f15\u7528\u89d2\u5ea6\u8fc7\u5927\u8212\u9002\u8303\u56f4" +
        "\u65f6\u95f4\u672c\u5c40\u603b\u5206\u5b8c\u6210\u8fde\u51fb\u76ee\u6807\u4f18\u79c0\u826f\u597d\u9700\u6539\u8fdb\u65e0\u6548" +
        "\u8bad\u7ec3\u5b8c\u6210\u7ee7\u7eed\u52a0\u6cb9\u5f97\u5206\u6b21\u6570\u6700\u9ad8\u5e73\u5747\u8d28\u91cf\u89c2\u770b\u6559\u5b66\u76f4\u63a5\u5f00\u59cb" +
        "\u6559\u5b66\u6f14\u793a\u8bf7\u89c2\u5bdf\u524d\u81c2\u5411\u5916\u65cb\u8f6c\u518d\u56de\u5230\u8d77\u70b9" +
        "0123456789:%+-/x SpaceABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
        "\uff1a\uff0c\u3002\uff1b\u3001\uff01\uff1f\uff08\uff09/\\\u00b0";

    [MenuItem("Tools/Healthy China/Fun1/Use Hanyi HuaMulan Font")]
    public static void UseHanyiFont()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            EditorUtility.DisplayDialog("Fun1 Font", "Cannot find Assets/Project/Fonts/HanyiHuaMulanW.ttf.", "OK");
            return;
        }

        TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>(true);
        string characters = CollectCharacters(texts);
        TMP_FontAsset tmpFont = RebuildTmpFontAsset(sourceFont, characters);
        if (tmpFont == null)
        {
            EditorUtility.DisplayDialog("Fun1 Font", "Failed to rebuild Hanyi HuaMulan TMP font asset.", "OK");
            return;
        }

        RemoveLegacyTextMirrors();

        Game1_UIController controller = Object.FindObjectOfType<Game1_UIController>(true);
        if (controller != null)
        {
            Undo.RecordObject(controller, "Use Hanyi HuaMulan Font");
            controller.fontOverride = tmpFont;
            controller.preferRuntimeChineseFont = false;
            controller.forceSceneTextFont = true;
            EditorUtility.SetDirty(controller);
        }

        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null) continue;

            Undo.RecordObject(text, "Use Hanyi HuaMulan Font");
            text.enabled = true;
            text.font = tmpFont;
            text.fontSharedMaterial = tmpFont.material;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            text.extraPadding = false;
            text.raycastTarget = false;
            EditorUtility.SetDirty(text);
        }

        CanvasScaler[] scalers = Object.FindObjectsOfType<CanvasScaler>(true);
        foreach (CanvasScaler scaler in scalers)
        {
            Undo.RecordObject(scaler, "Use Hanyi HuaMulan Font");
            scaler.dynamicPixelsPerUnit = 10f;
            EditorUtility.SetDirty(scaler);
        }

        AssetDatabase.SaveAssets();
        if (controller != null)
        {
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
        else
        {
            EditorSceneManager.MarkAllScenesDirty();
        }

        EditorUtility.DisplayDialog("Fun1 Font", "Func1 text has been reset to TMP and Hanyi HuaMulan only.", "OK");
    }

    static string CollectCharacters(TextMeshProUGUI[] texts)
    {
        string characters = WarmupCharacters;
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && !string.IsNullOrEmpty(text.text))
            {
                characters += text.text;
            }
        }

        return characters;
    }

    static TMP_FontAsset RebuildTmpFontAsset(Font sourceFont, string characters)
    {
        AssetDatabase.ImportAsset(SourceFontPath, ImportAssetOptions.ForceUpdate);

        if (AssetDatabase.LoadAssetAtPath<Object>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            4096,
            4096,
            AtlasPopulationMode.Dynamic);

        if (fontAsset == null) return null;

        fontAsset.name = "HanyiHuaMulanW SDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.fallbackFontAssetTable?.Clear();
        fontAsset.TryAddCharacters(characters, out string missingCharacters);

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        PersistFontSubAssets(fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning("Hanyi HuaMulan TMP font is missing some characters: " + missingCharacters);
        }

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    }

    static void PersistFontSubAssets(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null) return;

        Material material = fontAsset.material;
        if (material != null && !EditorUtility.IsPersistent(material))
        {
            material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);
            EditorUtility.SetDirty(material);
        }

        Texture2D atlasTexture = fontAsset.atlasTexture;
        if (atlasTexture != null && !EditorUtility.IsPersistent(atlasTexture))
        {
            atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            EditorUtility.SetDirty(atlasTexture);
        }

        EditorUtility.SetDirty(fontAsset);
    }

    static void RemoveLegacyTextMirrors()
    {
        Game1_LegacyTextMirror[] mirrors = Object.FindObjectsOfType<Game1_LegacyTextMirror>(true);
        foreach (Game1_LegacyTextMirror mirror in mirrors)
        {
            if (mirror == null) continue;

            if (mirror.sourceText != null)
            {
                Undo.RecordObject(mirror.sourceText, "Remove Hanyi Legacy Text Mirror");
                mirror.sourceText.enabled = true;
                EditorUtility.SetDirty(mirror.sourceText);
            }

            if (mirror.legacyText != null)
            {
                Undo.DestroyObjectImmediate(mirror.legacyText.gameObject);
            }

            Undo.DestroyObjectImmediate(mirror);
        }

        TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            Transform legacyChild = text.transform.Find(LegacyMirrorObjectName);
            if (legacyChild != null)
            {
                Undo.DestroyObjectImmediate(legacyChild.gameObject);
            }
        }
    }
}
