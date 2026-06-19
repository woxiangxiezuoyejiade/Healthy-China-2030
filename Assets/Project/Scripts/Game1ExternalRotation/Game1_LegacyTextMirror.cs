using TMPro;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class Game1_LegacyTextMirror : MonoBehaviour
{
    const string MirrorObjectName = "Hanyi_Legacy_Text";

    public TextMeshProUGUI sourceText;
    public UnityEngine.UI.Text legacyText;
    public Font hanyiFont;
    public float fontSizeMultiplier = 1f;

    void OnEnable()
    {
        RestoreTmpText();
    }

    void LateUpdate()
    {
        RestoreTmpText();
    }

    public void Configure(Font font)
    {
        hanyiFont = font;
        RestoreTmpText();
    }

    void RestoreTmpText()
    {
        if (sourceText == null) sourceText = GetComponent<TextMeshProUGUI>();
        if (sourceText != null) sourceText.enabled = true;

        if (legacyText != null)
        {
            DestroyObject(legacyText.gameObject);
            legacyText = null;
        }

        Transform legacyChild = transform.Find(MirrorObjectName);
        if (legacyChild != null)
        {
            DestroyObject(legacyChild.gameObject);
        }
    }

    static void DestroyObject(Object target)
    {
        if (target == null) return;

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
